module Gambol.Shared.Tests.WorkspaceUploadStructureTests

open Gambol.Shared
open Xunit

let private applyOps (graph: Graph) (ops: Op list) : Graph =
    let state =
        { graph = graph
          history = History.empty
          revision = Revision.Zero }

    ops
    |> List.fold
        (fun s op ->
            match Op.apply op s with
            | ApplyResult.Changed next
            | ApplyResult.Unchanged next -> next
            | ApplyResult.Invalid(_, msg) -> failwith msg)
        state
    |> fun s -> s.graph

let private addWorkspace label graph =
    let id, ops = FileNodeOps.planCreateWorkspace graph label
    id, applyOps graph ops

let private ownedNamedChildren graph parentId =
    graph.nodes.[parentId].children
    |> List.choose (fun child ->
        if child.ref <> Ownership.Owner then
            None
        else
            let node = graph.nodes.[child.id]

            Filename.tryValue node.name
            |> Option.map (fun name -> name, node))

let private childNamed graph parentId name =
    ownedNamedChildren graph parentId
    |> List.find (fst >> (=) name)
    |> snd

let private item rel isDir : WorkspaceUploadStructure.InventoryItem =
    { relative = rel; isDirectory = isDir }

let private requirePlan graph label items =
    match WorkspaceUploadStructure.planStubOps graph label items with
    | Ok ops -> ops
    | Error err -> failwith err

[<Fact>]
let ``planStubOps creates Directory named .scratch from inventory`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let items = [ item ".scratch" true ]
    let ops = requirePlan graph "home" items
    let created =
        ops
        |> List.choose (function
            | Op.NewSpecialNode(_, Directory, name) -> Some name
            | _ -> None)
    Assert.Contains(".scratch", created)
    let graph2 = applyOps graph ops
    let scratch = childNamed graph2 workspaceId ".scratch"
    Assert.Equal(Special Directory, scratch.kind)
    Assert.Equal(Filename.Ok ".scratch", scratch.name)

[<Fact>]
let ``planStubOps creates Directory named .agents with file child`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let items =
        [ item ".agents" true
          item ".agents/skill.md" false ]
    let graph2 =
        requirePlan graph "home" items |> applyOps graph
    let agents = childNamed graph2 workspaceId ".agents"
    Assert.Equal(Special Directory, agents.kind)
    let skill = childNamed graph2 agents.id "skill.md"
    Assert.Equal(Special File, skill.kind)
    Assert.Equal(Loaded, agents.childrenStatus)

[<Fact>]
let ``plan creates directory then file stubs under workspace`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"

    let items =
        [ item "docs" true
          item "docs/note.txt" false ]

    let graph2 =
        requirePlan graph "home" items |> applyOps graph

    let docs = childNamed graph2 workspaceId "docs"
    let file = childNamed graph2 docs.id "note.txt"
    Assert.Equal(Special Directory, docs.kind)
    Assert.Equal(Special File, file.kind)
    Assert.Empty(file.children)

[<Fact>]
let ``exact amb inventory paths represent containing document roots`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"

    let items =
        [ item ".amb" false
          item "docs/.amb" false
          item "notes.amb" false ]

    let graph2 =
        requirePlan graph "home" items |> applyOps graph

    let children = ownedNamedChildren graph2 workspaceId
    Assert.DoesNotContain(children, fun (name, _) -> name = ".amb")
    let docs = childNamed graph2 workspaceId "docs"
    Assert.Equal(Special Directory, docs.kind)
    Assert.DoesNotContain(
        ownedNamedChildren graph2 docs.id,
        fun (name, _) -> name = ".amb")
    Assert.Equal(Special File, (childNamed graph2 workspaceId "notes.amb").kind)

[<Fact>]
let ``new file stubs start NoServerFile; directory parent stays Current`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"

    let items =
        [ item "docs" true
          item "docs/note.txt" false ]

    let graph2 =
        requirePlan graph "home" items |> applyOps graph

    let docs = childNamed graph2 workspaceId "docs"
    let file = childNamed graph2 docs.id "note.txt"
    Assert.Equal(Current, docs.documentState)
    Assert.Equal(NoServerFile, file.documentState)

[<Fact>]
let ``reuses existing owned path without duplicating`` () =
    let workspaceId, graph0 = Graph.create () |> addWorkspace "home"
    let _, dirOps =
        FileNodeOps.planCreateOwnedDirectory graph0 workspaceId "docs"

    let graph1 = applyOps graph0 dirOps
    let docsBefore = childNamed graph1 workspaceId "docs"

    let items =
        [ item "docs" true
          item "docs/note.txt" false ]

    let ops = requirePlan graph1 "home" items
    let newDirs =
        ops
        |> List.choose (function
            | Op.NewSpecialNode(_, Directory, _) -> Some()
            | _ -> None)

    Assert.Empty(newDirs)
    let graph2 = applyOps graph1 ops
    let docsAfter = childNamed graph2 workspaceId "docs"
    Assert.Equal(docsBefore.id, docsAfter.id)
    let file = childNamed graph2 docsAfter.id "note.txt"
    Assert.Equal(Special File, file.kind)
    Assert.Equal(NoServerFile, file.documentState)

[<Fact>]
let ``TopLevel cap keeps only immediate children under scope`` () =
    let items =
        [ item "docs" true
          item "docs/a.txt" false
          item "docs/sub" true
          item "docs/sub/deep.txt" false
          item "root.txt" false ]

    let capped =
        WorkspaceUploadStructure.capPaths
            ""
            WorkspaceUploadStructure.StructureCap.TopLevelOnly
            items

    let rels =
        capped |> List.map (fun i -> i.relative) |> Set.ofList

    Assert.True((set [ "docs"; "root.txt" ]) = rels)

[<Fact>]
let ``Full cap keeps nested inventory paths`` () =
    let items =
        [ item "docs" true
          item "docs/a.txt" false ]

    let capped =
        WorkspaceUploadStructure.capPaths
            ""
            WorkspaceUploadStructure.StructureCap.FullPaths
            items

    Assert.Equal(2, capped.Length)

[<Fact>]
let ``plan is 1:1 with capped inventory paths`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"

    let inventory =
        [ item "docs" true
          item "docs/a.txt" false
          item "docs/sub" true
          item "root.txt" false ]

    let capped =
        WorkspaceUploadStructure.capPaths
            ""
            WorkspaceUploadStructure.StructureCap.TopLevelOnly
            inventory

    let ops = requirePlan graph "home" capped
    let graph2 = applyOps graph ops
    let names =
        ownedNamedChildren graph2 workspaceId
        |> List.map fst
        |> Set.ofList

    Assert.True((set [ "docs"; "root.txt" ]) = names)
    let docs = childNamed graph2 workspaceId "docs"
    Assert.Empty(ownedNamedChildren graph2 docs.id)

[<Fact>]
let ``no content children on new file stubs`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"

    let graph2 =
        requirePlan graph "home" [ item "only.txt" false ]
        |> applyOps graph

    let file = childNamed graph2 workspaceId "only.txt"
    Assert.Empty(file.children)

[<Fact>]
let ``reused Unparsed directory with owned child becomes Current`` () =
    let workspaceId, graph0 = Graph.create () |> addWorkspace "home"
    let _, dirOps = FileNodeOps.planCreateOwnedDirectory graph0 workspaceId "docs"
    let graph1 = applyOps graph0 dirOps
    let docsId = (childNamed graph1 workspaceId "docs").id

    let graph1' =
        applyOps
            graph1
            [ Op.SetDocumentState(docsId, Current, Unparsed) ]

    let _, fileOps = FileNodeOps.planCreateOwnedFile graph1' docsId "note.txt"
    let graph2 = applyOps graph1' fileOps

    let graph3 =
        requirePlan graph2 "home" [ item "docs" true ] |> applyOps graph2

    let docs = childNamed graph3 workspaceId "docs"
    Assert.Equal(Current, docs.documentState)

[<Fact>]
let ``plan promotes ancestor directories when nested file stub added`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"

    let items =
        [ item "audio" true
          item "audio/clip.wav" false
          item "cs" true
          item "cs/Block.cs" false ]

    let graph2 =
        requirePlan graph "home" items |> applyOps graph

    let audio = childNamed graph2 workspaceId "audio"
    let cs = childNamed graph2 workspaceId "cs"
    Assert.Equal(Current, audio.documentState)
    Assert.Equal(Current, cs.documentState)

[<Fact>]
let ``tryResolveFileNode finds owned file by relative path`` () =
    let _, graph0 = Graph.create () |> addWorkspace "home"

    let graph1 =
        requirePlan
            graph0
            "home"
            [ item "docs" true; item "docs/note.txt" false ]
        |> applyOps graph0

    match
        WorkspaceUploadStructure.tryResolveFileNode
            graph1
            "home"
            "docs/note.txt"
    with
    | None -> Assert.Fail("expected file node")
    | Some id ->
        Assert.Equal(Special File, graph1.nodes.[id].kind)

    Assert.True(
        WorkspaceUploadStructure.tryResolveFileNode
            graph1
            "home"
            "docs"
        |> Option.isNone)

[<Fact>]
let ``planServerFilePresentOps transitions only matching absent files`` () =
    let _, graph0 = Graph.create () |> addWorkspace "home"
    let graph1 =
        requirePlan
            graph0
            "home"
            [ item "present.txt" false
              item "oversized.txt" false ]
        |> applyOps graph0
    let ops =
        WorkspaceUploadStructure.planServerFilePresentOps
            graph1
            "home"
            [ "present.txt"; "missing.txt" ]
    let graph2 = applyOps graph1 ops
    let presentId =
        WorkspaceUploadStructure.tryResolveFileNode graph2 "home" "present.txt"
        |> Option.get
    let oversizedId =
        WorkspaceUploadStructure.tryResolveFileNode graph2 "home" "oversized.txt"
        |> Option.get
    Assert.Equal(Unparsed, graph2.nodes.[presentId].documentState)
    Assert.Equal(NoServerFile, graph2.nodes.[oversizedId].documentState)

[<Fact>]
let ``planAlignFileStampOps SetUpdateTime when node lags download mtime`` () =
    let _, graph0 = Graph.create () |> addWorkspace "home"
    let graph1 =
        requirePlan
            graph0
            "home"
            [ item "note.txt" false ]
        |> applyOps graph0
    let fileId =
        WorkspaceUploadStructure.tryResolveFileNode graph1 "home" "note.txt"
        |> Option.get
    let oldStamp =
        System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
    let newStamp =
        System.DateTime(2026, 1, 2, 0, 0, 0, System.DateTimeKind.Utc)
    let stamped =
        { graph1.nodes.[fileId] with updateTime = oldStamp }
    let graph2 =
        Graph.fromNodes
            graph1.root
            (Map.add fileId stamped graph1.nodes)
    match
        WorkspaceUploadStructure.planAlignFileStampOps
            graph2
            "home"
            [ "note.txt", newStamp ]
    with
    | [ Op.SetUpdateTime(id, oldT, newT) ] ->
        Assert.Equal(fileId, id)
        Assert.Equal(oldStamp, oldT)
        Assert.Equal(NodeUpdateTime.toDbPrecision newStamp, newT)
    | other -> Assert.Fail($"expected one SetUpdateTime, got {other}")
    Assert.True(
        WorkspaceUploadStructure.planAlignFileStampOps
            graph2
            "home"
            [ "note.txt", oldStamp ]
        |> List.isEmpty)

[<Fact>]
let ``shouldReparseAfterMtimeSkip only Unparsed`` () =
    Assert.True(WorkspaceUploadStructure.shouldReparseAfterMtimeSkip Unparsed)
    Assert.False(WorkspaceUploadStructure.shouldReparseAfterMtimeSkip Current)
    Assert.False(
        WorkspaceUploadStructure.shouldReparseAfterMtimeSkip NoServerFile)

[<Fact>]
let ``planStubOps creates File under SYSTEM label`` () =
    let graph0 = Graph.create ()
    let graph2 =
        requirePlan graph0 "SYSTEM" [ item "user.css" false ]
        |> applyOps graph0
    let file = childNamed graph2 Graph.systemId "user.css"
    Assert.Equal(Special File, file.kind)
    Assert.True(Graph.isSpecialSystemDirectoryMember graph2 file.id)
    Assert.Equal(
        Some file.id,
        WorkspaceUploadStructure.tryResolveFileNode graph2 "SYSTEM" "user.css")

/// Issue 21 Load path: client sees Unloaded workspace (bootstrap), inventory
/// lists paths already resident on the server. Stub planning against empty
/// Unloaded children invents NewSpecialNodes; POST /changes then 400s with
/// "name conflict" — stuck Uploading, package never fetched.
[<Fact>]
let ``Load Unloaded stub plan must not name-conflict on resident server`` () =
    let workspaceId, server0 = Graph.create () |> addWorkspace "home"
    let server =
        requirePlan server0 "home" [ item "note.txt" false ]
        |> applyOps server0
    let serverFileId = (childNamed server workspaceId "note.txt").id
    let client =
        ResidentProjection.bootstrapGraph
            BootstrapScope.RootClosure
            None
            server
    Assert.Equal(Unloaded, client.nodes.[workspaceId].childrenStatus)
    Assert.False(Map.containsKey serverFileId client.nodes)
    let ops = requirePlan client "home" [ item "note.txt" false ]
    match ops with
    | [] -> ()
    | _ ->
        let invented =
            ops
            |> List.choose (function
                | Op.NewSpecialNode(id, File, name) -> Some(id, name)
                | _ -> None)
        Assert.True(
            invented |> List.exists (fun (id, name) ->
                name = "note.txt" && id <> serverFileId),
            "expected a new File stub id for note.txt")
        let change =
            { id = 0
              changeId = System.Guid.NewGuid()
              ops = ops }
        let state =
            { graph = server
              history = History.empty
              revision = Revision 0 }
        match History.applyChange change state with
        | ApplyResult.Invalid(_, msg) ->
            Assert.True(
                false,
                "Load structure POST must not 400; got: " + msg)
        | ApplyResult.Unchanged _
        | ApplyResult.Changed _ -> ()
