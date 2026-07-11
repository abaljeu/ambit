module LazyLoadReconciliationTests

open Gambol.Shared
open Xunit

let private applyOps (graph: Graph) (ops: Op list) : Graph =
    let state = { graph = graph; history = History.empty; revision = Revision.Zero }
    ops
    |> List.fold (fun s op ->
        match Op.apply op s with
        | ApplyResult.Changed next
        | ApplyResult.Unchanged next -> next
        | ApplyResult.Invalid(_, msg) -> failwith msg) state
    |> fun s -> s.graph

let private addWorkspace label graph =
    let id, ops = FileNodeOps.planCreateWorkspace graph label
    id, applyOps graph ops

let private ownedNamedChildren graph parentId =
    graph.nodes.[parentId].children
    |> List.choose (fun child ->
        if child.ref <> Ownership.Owner then None
        else
            let node = graph.nodes.[child.id]
            Filename.tryValue node.name
            |> Option.map (fun name -> name, node))

let private requirePlan graph label paths =
    match LazyLoadReconciliation.planAddedPaths graph label paths with
    | Ok ops -> ops
    | Error err -> failwith err

let private requireChangedPlan graph label changes =
    match LazyLoadReconciliation.planChangedPaths graph label changes with
    | Ok ops -> ops
    | Error err -> failwith err

let private childNamed graph parentId name =
    ownedNamedChildren graph parentId
    |> List.find (fst >> (=) name)
    |> snd

let private createPaths graph paths =
    requirePlan graph "home" paths |> applyOps graph

[<Fact>]
let ``one added file creates a file stub`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = requirePlan graph "home" [ "README.md" ] |> applyOps graph
    let children = ownedNamedChildren graph2 workspaceId
    Assert.Equal(1, children.Length)
    Assert.Equal("README.md", fst children.Head)
    Assert.Equal(Special File, (snd children.Head).kind)
    Assert.Empty((snd children.Head).children)

[<Fact>]
let ``nested added path creates missing directory stubs`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = requirePlan graph "home" [ "src/lib/core.fs" ] |> applyOps graph
    let src = ownedNamedChildren graph2 workspaceId |> List.find (fst >> (=) "src") |> snd
    let lib = ownedNamedChildren graph2 src.id |> List.find (fst >> (=) "lib") |> snd
    let file = ownedNamedChildren graph2 lib.id |> List.find (fst >> (=) "core.fs") |> snd
    Assert.Equal(Special Directory, src.kind)
    Assert.Equal(Special Directory, lib.kind)
    Assert.Equal(Special File, file.kind)

[<Fact>]
let ``repeated reconciliation reuses matching stubs`` () =
    let _, graph = Graph.create () |> addWorkspace "home"
    let graph2 = requirePlan graph "home" [ "src/core.fs" ] |> applyOps graph
    let second = requirePlan graph2 "home" [ "src/core.fs"; "src/core.fs" ]
    Assert.Empty(second)

[<Fact>]
let ``amb suffixes are ordinary file names`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let paths = [ "x.amb"; "notes.amb.txt" ]
    let graph2 = requirePlan graph "home" paths |> applyOps graph
    let children = ownedNamedChildren graph2 workspaceId
    let xAmb = children |> List.find (fst >> (=) "x.amb") |> snd
    let notesAmbTxt = children |> List.find (fst >> (=) "notes.amb.txt") |> snd
    Assert.Equal(Special File, xAmb.kind)
    Assert.Equal(Special File, notesAmbTxt.kind)

[<Fact>]
let ``exact amb marker represents its containing directory`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let paths = [ ".git/config"; ".amb"; "docs/.amb" ]
    let graph2 = requirePlan graph "home" paths |> applyOps graph
    let children = ownedNamedChildren graph2 workspaceId
    Assert.DoesNotContain(children, fun (name, _) -> name = ".git" || name = ".amb")
    let docs = children |> List.find (fst >> (=) "docs") |> snd
    Assert.Equal(Special Directory, docs.kind)
    Assert.DoesNotContain(
        ownedNamedChildren graph2 docs.id,
        fun (name, _) -> name = ".amb")

[<Fact>]
let ``workspace label scopes reconciliation`` () =
    let homeId, graph1 = Graph.create () |> addWorkspace "home"
    let workId, graph2 = graph1 |> addWorkspace "work"
    let graph3 = requirePlan graph2 "work" [ "src/main.fs" ] |> applyOps graph2
    Assert.Empty(ownedNamedChildren graph3 homeId)
    Assert.Single(ownedNamedChildren graph3 workId) |> ignore

[<Fact>]
let ``kind conflict at an existing path returns error`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let _, fileOps = FileNodeOps.planCreateOwnedFile graph workspaceId "src"
    let graph2 = applyOps graph fileOps
    match LazyLoadReconciliation.planAddedPaths graph2 "home" [ "src/main.fs" ] with
    | Ok _ -> Assert.Fail("expected kind conflict")
    | Error err ->
        Assert.Contains("src", err)
        Assert.Contains("kind conflict", err)

[<Fact>]
let ``deleted file is moved to trash with parsed descendants`` () =
    let _, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "docs/readme.txt" ]
    let docs = childNamed graph2 (childNamed graph2 Graph.workspacesId "home").id "docs"
    let file = childNamed graph2 docs.id "readme.txt"
    let parsedId = NodeId.New()
    let graph3 =
        [ Op.NewNode(parsedId, "parsed")
          Op.Replace(file.id, 0, [], [ { ref = Ownership.Owner; id = parsedId } ]) ]
        |> applyOps graph2
    let graph4 =
        requireChangedPlan graph3 "home" [ LazyLoadReconciliation.Deleted "docs/readme.txt" ]
        |> applyOps graph3
    Assert.Contains(
        graph4.nodes.[Graph.trashId].children,
        fun child -> child.id = docs.id && child.ref = Ownership.Owner)
    Assert.Equal(parsedId, graph4.nodes.[file.id].children.Head.id)

[<Fact>]
let ``deleted file refs become path expressions without promotion`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "note.txt" ]
    let file = childNamed graph2 workspaceId "note.txt"
    let holderId = NodeId.New()
    let graph3 =
        [ Op.NewNode(holderId, "holder")
          Op.Replace(Graph.rootId, 0, [], [ { ref = Ownership.Owner; id = holderId } ])
          Op.Replace(holderId, 0, [], [ { ref = Ownership.Ref; id = file.id } ]) ]
        |> applyOps graph2
    let graph4 =
        requireChangedPlan graph3 "home" [ LazyLoadReconciliation.Deleted "note.txt" ]
        |> applyOps graph3
    let replacement = graph4.nodes.[holderId].children |> List.exactlyOne
    Assert.Equal(Ownership.Owner, replacement.ref)
    Assert.Equal("[[//home/note.txt]]", graph4.nodes.[replacement.id].text)
    Assert.Contains(graph4.nodes.[Graph.trashId].children, fun child -> child.id = file.id)

[<Fact>]
let ``rename and cross-directory move preserve identity and children`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "docs/a.txt"; "archive/.amb" ]
    let docs = childNamed graph2 workspaceId "docs"
    let archive = childNamed graph2 workspaceId "archive"
    let file = childNamed graph2 docs.id "a.txt"
    let parsedId = NodeId.New()
    let graph3 =
        [ Op.NewNode(parsedId, "parsed")
          Op.Replace(file.id, 0, [], [ { ref = Ownership.Owner; id = parsedId } ]) ]
        |> applyOps graph2
    let renamed =
        requireChangedPlan
            graph3
            "home"
            [ LazyLoadReconciliation.Renamed("docs/a.txt", "docs/b.txt") ]
        |> applyOps graph3
    let renamedFile = childNamed renamed docs.id "b.txt"
    Assert.Equal(file.id, renamedFile.id)
    let moved =
        requireChangedPlan
            renamed
            "home"
            [ LazyLoadReconciliation.Renamed("docs/b.txt", "archive/b.txt") ]
        |> applyOps renamed
    let movedFile = childNamed moved archive.id "b.txt"
    Assert.Equal(file.id, movedFile.id)
    Assert.Equal(parsedId, movedFile.children.Head.id)

[<Fact>]
let ``directory marker rename coalesces nested renames`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "docs/.amb"; "docs/nested/a.txt" ]
    let docs = childNamed graph2 workspaceId "docs"
    let nested = childNamed graph2 docs.id "nested"
    let file = childNamed graph2 nested.id "a.txt"
    let changes =
        [ LazyLoadReconciliation.Renamed("docs/.amb", "archive/.amb")
          LazyLoadReconciliation.Renamed(
              "docs/nested/a.txt",
              "archive/nested/a.txt") ]
    let graph3 = requireChangedPlan graph2 "home" changes |> applyOps graph2
    let archive = childNamed graph3 workspaceId "archive"
    let nestedAfter = childNamed graph3 archive.id "nested"
    Assert.Equal(docs.id, archive.id)
    Assert.Equal(nested.id, nestedAfter.id)
    Assert.Equal(file.id, (childNamed graph3 nestedAfter.id "a.txt").id)

[<Fact>]
let ``exact marker modification invalidates containing documents`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "docs/.amb" ]
    let docs = childNamed graph2 workspaceId "docs"
    let current =
        [ Op.SetDocumentState(docs.id, Unparsed, Current) ]
        |> applyOps graph2
    let graph3 =
        requireChangedPlan
            current
            "home"
            [ LazyLoadReconciliation.Modified ".amb"
              LazyLoadReconciliation.Modified "docs/.amb" ]
        |> applyOps current
    Assert.Equal(Unparsed, graph3.nodes.[workspaceId].documentState)
    Assert.Equal(Unparsed, graph3.nodes.[docs.id].documentState)

[<Fact>]
let ``modified file becomes unparsed without losing identity or placement`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "note.txt" ]
    let file = childNamed graph2 workspaceId "note.txt"
    let current =
        Op.SetDocumentState(file.id, Unparsed, Current)
        |> List.singleton
        |> applyOps graph2
    let graph3 =
        requireChangedPlan current "home" [ LazyLoadReconciliation.Modified "note.txt" ]
        |> applyOps current
    let after = childNamed graph3 workspaceId "note.txt"
    Assert.Equal(file.id, after.id)
    Assert.Equal(Unparsed, after.documentState)

[<Fact>]
let ``x amb remains an ordinary file for rename and delete`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "x.amb" ]
    let original = childNamed graph2 workspaceId "x.amb"
    let graph3 =
        requireChangedPlan
            graph2
            "home"
            [ LazyLoadReconciliation.Renamed("x.amb", "y.amb") ]
        |> applyOps graph2
    Assert.Equal(original.id, (childNamed graph3 workspaceId "y.amb").id)
    let graph4 =
        requireChangedPlan graph3 "home" [ LazyLoadReconciliation.Deleted "y.amb" ]
        |> applyOps graph3
    Assert.Contains(graph4.nodes.[Graph.trashId].children, fun child -> child.id = original.id)

[<Fact>]
let ``delete and add without rename use a new identity`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "old.txt" ]
    let oldId = (childNamed graph2 workspaceId "old.txt").id
    let changes =
        [ LazyLoadReconciliation.Deleted "old.txt"
          LazyLoadReconciliation.Added "new.txt" ]
    let graph3 = requireChangedPlan graph2 "home" changes |> applyOps graph2
    let newId = (childNamed graph3 workspaceId "new.txt").id
    Assert.NotEqual(oldId, newId)
    Assert.Contains(graph3.nodes.[Graph.trashId].children, fun child -> child.id = oldId)

[<Fact>]
let ``repeated full reconciliation is idempotent`` () =
    let _, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "old.txt" ]
    let changes =
        [ LazyLoadReconciliation.Renamed("old.txt", "new.txt")
          LazyLoadReconciliation.Modified "new.txt" ]
    let graph3 = requireChangedPlan graph2 "home" changes |> applyOps graph2
    Assert.Empty(requireChangedPlan graph3 "home" changes)

[<Fact>]
let ``deleting exact marker alone keeps containing directory`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "docs/.amb"; "docs/keep.txt" ]
    let docsId = (childNamed graph2 workspaceId "docs").id
    let ops =
        requireChangedPlan
            graph2
            "home"
            [ LazyLoadReconciliation.Deleted "docs/.amb" ]
    Assert.Empty(ops)
    Assert.Equal(docsId, (childNamed graph2 workspaceId "docs").id)

[<Fact>]
let ``delete file plus add directory at same path is a kind conflict`` () =
    let _, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "item" ]
    let changes =
        [ LazyLoadReconciliation.Deleted "item"
          LazyLoadReconciliation.Added "item/.amb" ]
    match LazyLoadReconciliation.planChangedPaths graph2 "home" changes with
    | Ok _ -> Assert.Fail("expected kind conflict")
    | Error err -> Assert.Contains("kind conflict", err)
