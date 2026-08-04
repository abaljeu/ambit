module Gambol.Shared.Tests.CommandEntryTests

open FSharp.Reflection
open Xunit
open Gambol.Shared
open Gambol.Shared.CommandEntry

[<Fact>]
let ``inKeyScope respects selection context`` () =
    Assert.True(inKeyScope true SelectionOnly)
    Assert.False(inKeyScope false SelectionOnly)
    Assert.False(inKeyScope true EditingOnly)
    Assert.True(inKeyScope false EditingOnly)
    Assert.True(inKeyScope true SelectionOrEditing)
    Assert.True(inKeyScope false SelectionOrEditing)

[<Fact>]
let ``scopeInSelection includes selection scopes only`` () =
    Assert.True(scopeInSelection SelectionOnly)
    Assert.False(scopeInSelection EditingOnly)
    Assert.True(scopeInSelection SelectionOrEditing)

[<Fact>]
let ``scopeInEditing includes editing scopes only`` () =
    Assert.False(scopeInEditing SelectionOnly)
    Assert.True(scopeInEditing EditingOnly)
    Assert.True(scopeInEditing SelectionOrEditing)

[<Fact>]
let ``allCommands has unique ids for every CommandId case`` () =
    let unionCases =
        FSharpType.GetUnionCases typeof<CommandId>
        |> Array.map (fun c -> unbox<CommandId> (FSharpValue.MakeUnion(c, [||])))
        |> Set.ofArray
    Assert.Equal(unionCases.Count, allCommands.Length)
    let ids = allCommands |> List.map (fun e -> e.id)
    Assert.Equal(allCommands.Length, List.distinct ids |> List.length)
    let tableIds = ids |> Set.ofList
    Assert.True((unionCases = tableIds))

[<Fact>]
let ``commandFor returns every command`` () =
    for id in allCommands |> List.map (fun e -> e.id) do
        match commandFor id with
        | None -> Assert.Fail($"missing metadata for {id}")
        | Some e -> Assert.Equal(id, e.id)

[<Fact>]
let ``displayName matches metadata name`` () =
    for e in allCommands do
        let name = displayName e.id
        Assert.False(System.String.IsNullOrWhiteSpace name)
        Assert.Equal(e.name, name)

[<Fact>]
let ``load owns Ctrl Shift greater-than`` () =
    let entry = commandFor Load |> Option.get
    Assert.Equal("Load", entry.name)
    Assert.Equal<string list>([ "Ctrl+Shift+>" ], entry.keys)
    Assert.DoesNotContain(allCommands, fun command -> command.name = "Import")
    Assert.DoesNotContain(allCommands, fun command -> command.name = "Map workspace")
    Assert.DoesNotContain(allCommands, fun command -> command.name = "Upload")

[<Fact>]
let ``download owns Ctrl Shift less-than`` () =
    let entry = commandFor Download |> Option.get
    Assert.Equal("Download", entry.name)
    Assert.Equal<string list>([ "Ctrl+Shift+<" ], entry.keys)
    Assert.DoesNotContain(allCommands, fun command -> command.name = "Export")
    Assert.DoesNotContain(allCommands, fun command -> command.name = "Git status")

let private contextualGraph () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let childId = NodeId.New()
    let workspaceId = NodeId.New()
    let refHolderId = NodeId.New()
    let owned id = { ref = Ownership.Owner; id = id }
    let file =
        Node.Create(
            fileId,
            name = Filename.create "note.txt",
            children = [ owned childId ],
            kind = Special File,
            documentState = Unparsed)
    let child = Node.Create(childId, owner = fileId)
    let workspace =
        Node.Create(
            workspaceId,
            name = Filename.create "home",
            kind = Special Workspace)
    let holder =
        Node.Create(
            refHolderId,
            children = [ { ref = Ownership.Ref; id = fileId } ])
    let root = graph0.nodes.[Graph.rootId]
    let rootChildren =
        root.children @ [ owned fileId; owned workspaceId; owned refHolderId ]
    let graph =
        graph0.nodes
        |> Map.add Graph.rootId { root with children = rootChildren }
        |> Map.add fileId file
        |> Map.add childId child
        |> Map.add workspaceId workspace
        |> Map.add refHolderId holder
        |> Graph.fromNodes graph0.root
    graph, fileId, workspaceId, refHolderId

[<Fact>]
let ``context command parses owning unparsed file from focused owner occurrence`` () =
    let graph, fileId, _, _ = contextualGraph ()
    let rootIndex =
        graph.nodes.[Graph.rootId].children
        |> List.findIndex (fun child -> child.id = fileId)
    let target = contextualTarget graph Graph.rootId rootIndex
    Assert.Equal(
        Some(ParseFile fileId),
        target)
    Assert.Equal(
        WorkspaceUploadAction.ParseServerDisk fileId,
        WorkspaceUpload.plan true false false target)
    Assert.True(WorkspaceSyncScope.tryFromFocus graph fileId |> Result.isError)
    Assert.Equal(
        Some(ParseFile fileId),
        contextualTarget graph fileId 0)

[<Fact>]
let ``context command reconciles named workspace and ignores ref occurrence`` () =
    let graph, fileId, workspaceId, refHolderId = contextualGraph ()
    Assert.Equal(
        Some(ReconcileWorkspace workspaceId),
        contextualTarget graph Graph.rootId
            (graph.nodes.[Graph.rootId].children
             |> List.findIndex (fun child -> child.id = workspaceId)))
    Assert.Equal(None, contextualTarget graph refHolderId 0)
    let currentGraph =
        { graph with
            nodes =
                graph.nodes
                |> Map.add fileId
                    { graph.nodes.[fileId] with documentState = Current } }
    Assert.Equal(
        Some(ParseFile fileId),
        contextualTarget currentGraph fileId 0)

[<Fact>]
let ``context command reconciles owned directory under named workspace`` () =
    let applyOps (graph: Graph) (ops: Op list) =
        let state = { graph = graph; history = History.empty; revision = Revision.Zero }
        ops
        |> List.fold (fun s op ->
            match Op.apply op s with
            | ApplyResult.Changed next
            | ApplyResult.Unchanged next -> next
            | ApplyResult.Invalid(_, msg) -> failwith msg) state
        |> fun s -> s.graph
    let workspaceId, wsOps = FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let graph1 = applyOps (Graph.create ()) wsOps
    let dirId, dirOps = FileNodeOps.planCreateOwnedDirectory graph1 workspaceId "docs"
    let graph2 = applyOps graph1 dirOps
    let dirIndex =
        graph2.nodes.[workspaceId].children
        |> List.findIndex (fun child -> child.id = dirId)
    Assert.Equal(
        Some(ReconcileDirectory dirId),
        contextualTarget graph2 workspaceId dirIndex)
    let refHolderId = NodeId.New()
    let holder =
        Node.Create(
            refHolderId,
            children = [ { ref = Ownership.Ref; id = dirId } ])
    let ws = graph2.nodes.[workspaceId]
    let graph3 =
        graph2.nodes
        |> Map.add workspaceId
            { ws with
                children =
                    ws.children @ [ { ref = Ownership.Owner; id = refHolderId } ] }
        |> Map.add refHolderId holder
        |> Graph.fromNodes graph2.root
    let refIndex =
        graph3.nodes.[workspaceId].children
        |> List.findIndex (fun child -> child.id = refHolderId)
    Assert.Equal(None, contextualTarget graph3 workspaceId refIndex)

[<Fact>]
let ``context command reconciles SYSTEM directory`` () =
    let graph = Graph.create ()
    let systemIndex =
        graph.nodes.[Graph.rootId].children
        |> List.findIndex (fun child -> child.id = Graph.systemId)
    Assert.Equal(
        Some(ReconcileDirectory Graph.systemId),
        contextualTarget graph Graph.rootId systemIndex)
    Assert.Equal(
        WorkspaceUploadAction.ReconcileServerDisk,
        WorkspaceUpload.plan false false false (Some(ReconcileDirectory Graph.systemId)))
    Assert.True(
        WorkspaceUpload.isAvailable
            false
            false
            (Some(ReconcileDirectory Graph.systemId)))

[<Fact>]
let ``context command reconciles owned directory under SYSTEM`` () =
    let applyOps (graph: Graph) (ops: Op list) =
        let state = { graph = graph; history = History.empty; revision = Revision.Zero }
        ops
        |> List.fold (fun s op ->
            match Op.apply op s with
            | ApplyResult.Changed next
            | ApplyResult.Unchanged next -> next
            | ApplyResult.Invalid(_, msg) -> failwith msg) state
        |> fun s -> s.graph
    let graph0 = Graph.create ()
    let dirId, dirOps =
        FileNodeOps.planCreateOwnedDirectory graph0 Graph.systemId "cfg"
    let graph1 = applyOps graph0 dirOps
    let dirIndex =
        graph1.nodes.[Graph.systemId].children
        |> List.findIndex (fun child -> child.id = dirId)
    Assert.Equal(
        Some(ReconcileDirectory dirId),
        contextualTarget graph1 Graph.systemId dirIndex)
