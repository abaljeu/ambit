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

let private markDocumentsCurrent graph =
    graph.nodes
    |> Map.toList
    |> List.choose (fun (nodeId, node) ->
        match node.kind, node.documentState with
        | Special (Workspace | Directory | File), Unparsed ->
            Some(Op.SetDocumentState(nodeId, Unparsed, Current))
        | _ -> None)
    |> applyOps graph

[<Fact>]
let ``one added file creates a file stub`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = requirePlan graph "home" [ "README.md" ] |> applyOps graph
    let children = ownedNamedChildren graph2 workspaceId
    Assert.Equal(1, children.Length)
    Assert.Equal("README.md", fst children.Head)
    Assert.Equal(Special File, (snd children.Head).kind)
    Assert.Equal(Unparsed, (snd children.Head).documentState)
    Assert.Empty((snd children.Head).children)

[<Fact>]
let ``nested file parse after upload tree build is accepted`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = requirePlan graph "home" [ "src/note.txt" ] |> applyOps graph
    let src = childNamed graph2 workspaceId "src"
    let file = childNamed graph2 src.id "note.txt"
    Assert.Equal(Current, src.documentState)
    Assert.Equal(Unparsed, file.documentState)
    let parsedId = NodeId.New()
    let attach = ChildNode.owner parsedId
    let state =
        { graph = graph2; history = History.empty; revision = Revision.Zero }
    let parseChange =
        { id = 0
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.SetDocumentState(file.id, Unparsed, Current)
              Op.NewNode(parsedId, "parsed")
              Op.Replace(file.id, 0, [], [ attach ]) ] }
    match History.applyChange parseChange state with
    | ApplyResult.Changed next ->
        Assert.Equal(Current, next.graph.nodes.[file.id].documentState)
        Assert.Equal(Current, next.graph.nodes.[src.id].documentState)
        Assert.Equal(parsedId, next.graph.nodes.[file.id].children.Head.id)
    | other -> Assert.Fail($"expected Changed, got {other}")

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
let ``nested added path with spaces creates missing stubs`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 =
        requirePlan graph "home" [ "employment/business/Engineering AI.pdf" ]
        |> applyOps graph
    let employment =
        ownedNamedChildren graph2 workspaceId
        |> List.find (fst >> (=) "employment")
        |> snd
    let business =
        ownedNamedChildren graph2 employment.id
        |> List.find (fst >> (=) "business")
        |> snd
    let file =
        ownedNamedChildren graph2 business.id
        |> List.find (fst >> (=) "Engineering AI.pdf")
        |> snd
    Assert.Equal(Special Directory, employment.kind)
    Assert.Equal(Special Directory, business.kind)
    Assert.Equal(Special File, file.kind)

[<Fact>]
let ``upload-built directories become current when members are added`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = requirePlan graph "home" [ "src/lib/core.fs" ] |> applyOps graph
    let src = childNamed graph2 workspaceId "src"
    let lib = childNamed graph2 src.id "lib"
    let file = childNamed graph2 lib.id "core.fs"
    Assert.Equal(Current, src.documentState)
    Assert.Equal(Current, lib.documentState)
    Assert.Equal(Unparsed, file.documentState)
    Assert.Equal(Current, graph2.nodes.[workspaceId].documentState)

[<Fact>]
let ``exact amb add marks directory stub unparsed`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = requirePlan graph "home" [ "docs/.amb" ] |> applyOps graph
    let docs = childNamed graph2 workspaceId "docs"
    Assert.Equal(Special Directory, docs.kind)
    Assert.Equal(Unparsed, docs.documentState)

[<Fact>]
let ``exact amb add with text parses outline immediately`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let artifacts = Map.ofList [ "docs/.amb", "outline body" + System.Environment.NewLine ]
    let ops =
        match
            LazyLoadReconciliation.planAddedPathsWithArtifacts
                graph
                "home"
                [ "docs/.amb" ]
                artifacts
        with
        | Ok o -> o
        | Error err -> failwith err
    let graph2 = applyOps graph ops
    let docs = childNamed graph2 workspaceId "docs"
    Assert.Equal(Current, docs.documentState)
    Assert.Equal(1, docs.children.Length)
    Assert.Equal("outline body", graph2.nodes.[docs.children.Head.id].text)

[<Fact>]
let ``exact amb modify with text reparses instead of leaving unparsed`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "docs/.amb" ]
    let docs = childNamed graph2 workspaceId "docs"
    let artifacts = Map.ofList [ "docs/.amb", "fresh" + System.Environment.NewLine ]
    let ops =
        match
            LazyLoadReconciliation.planChangedPathsWithArtifacts
                graph2
                "home"
                [ LazyLoadReconciliation.Modified "docs/.amb" ]
                artifacts
        with
        | Ok o -> o
        | Error err -> failwith err
    let graph3 = applyOps graph2 ops
    Assert.Equal(Current, graph3.nodes.[docs.id].documentState)
    Assert.Equal("fresh", graph3.nodes.[graph3.nodes.[docs.id].children.Head.id].text)

[<Fact>]
let ``exact amb modify warm keeps node id on text edit`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let initial = Map.ofList [ "docs/.amb", "alpha" + System.Environment.NewLine ]
    let ops1 =
        match
            LazyLoadReconciliation.planAddedPathsWithArtifacts
                graph
                "home"
                [ "docs/.amb" ]
                initial
        with
        | Ok o -> o
        | Error err -> failwith err
    let graph2 = applyOps graph ops1
    let docs = childNamed graph2 workspaceId "docs"
    let childId = docs.children.Head.id
    Assert.Equal("alpha", graph2.nodes.[childId].text)
    let edited = Map.ofList [ "docs/.amb", "ALPHA" + System.Environment.NewLine ]
    let ops2 =
        match
            LazyLoadReconciliation.planChangedPathsWithArtifacts
                graph2
                "home"
                [ LazyLoadReconciliation.Modified "docs/.amb" ]
                edited
        with
        | Ok o -> o
        | Error err -> failwith err
    let graph3 = applyOps graph2 ops2
    Assert.Equal(childId, graph3.nodes.[docs.id].children.Head.id)
    Assert.Equal("ALPHA", graph3.nodes.[childId].text)

[<Fact>]
let ``repeated reconciliation reuses matching stubs`` () =
    let _, graph = Graph.create () |> addWorkspace "home"
    let graph2 = requirePlan graph "home" [ "src/core.fs" ] |> applyOps graph
    let second = requirePlan graph2 "home" [ "src/core.fs"; "src/core.fs" ]
    Assert.Empty(second)

[<Fact>]
let ``reconciliation finds artifacts through normal organizers`` () =
    let workspaceId, graph0 = Graph.create () |> addWorkspace "home"
    let graph1, workspaceOrganizerId = Graph.newNode "organizer" graph0
    let graph2 =
        [ Op.Replace(
              workspaceId,
              0,
              [],
              [ ChildNode.owner workspaceOrganizerId ]) ]
        |> applyOps graph1
    let srcId, srcOps =
        FileNodeOps.planCreateOwnedDirectory graph2 workspaceOrganizerId "src"
    let graph3 = applyOps graph2 srcOps
    let graph4, srcOrganizerId = Graph.newNode "nested organizer" graph3
    let graph5 =
        [ Op.Replace(
              srcId,
              0,
              [],
              [ ChildNode.owner srcOrganizerId ]) ]
        |> applyOps graph4
    let fileId, fileOps =
        FileNodeOps.planCreateOwnedFile graph5 srcOrganizerId "main.fs"
    let graph6 = applyOps graph5 fileOps
    let graph7 = requirePlan graph6 "home" [ "src/main.fs" ] |> applyOps graph6
    match LazyLoadReconciliation.resolveOwnedPath graph7 "home" "src/main.fs" with
    | Ok(Some(resolvedId, File)) -> Assert.Equal(fileId, resolvedId)
    | other -> Assert.Fail($"expected existing file through organizers, got {other}")

[<Fact>]
let ``structural reconciliation rejects an already unparsed document`` () =
    let _, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "note.txt" ]
    match
        LazyLoadReconciliation.planChangedPaths
            graph2
            "home"
            [ LazyLoadReconciliation.Deleted "note.txt" ]
    with
    | Ok _ -> Assert.Fail("expected unparsed document rejection")
    | Error error ->
        Assert.Equal(
            "operation cannot modify an unparsed document; parse it first",
            error)

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
let ``reserved gambol dot files are excluded from reconciliation`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let paths = [ "gambol.log"; "nested/GAMBOL.meta"; "gambol" ]
    let graph2 = requirePlan graph "home" paths |> applyOps graph
    let children = ownedNamedChildren graph2 workspaceId
    Assert.DoesNotContain(children, fun (name, _) -> name = "gambol.log")
    Assert.DoesNotContain(children, fun (name, _) -> name = "nested")
    Assert.Contains(children, fun (name, _) -> name = "gambol")

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
    let graph2 = createPaths graph [ "docs/readme.txt" ] |> markDocumentsCurrent
    let docs = childNamed graph2 (childNamed graph2 Graph.workspacesId "home").id "docs"
    let file = childNamed graph2 docs.id "readme.txt"
    let parsedId = NodeId.New()
    let graph3 =
        [ Op.NewNode(parsedId, "parsed")
          Op.Replace(file.id, 0, [], [ ChildNode.owner parsedId ]) ]
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
    let graph2 = createPaths graph [ "note.txt" ] |> markDocumentsCurrent
    let file = childNamed graph2 workspaceId "note.txt"
    let holderId = NodeId.New()
    let graph3 =
        [ Op.NewNode(holderId, "holder")
          Op.Replace(Graph.rootId, 0, [], [ ChildNode.owner holderId ])
          Op.Replace(holderId, 0, [], [ ChildNode.reference file.id ]) ]
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
    let graph2 =
        createPaths graph [ "docs/a.txt"; "archive/.amb" ]
        |> markDocumentsCurrent
    let docs = childNamed graph2 workspaceId "docs"
    let archive = childNamed graph2 workspaceId "archive"
    let file = childNamed graph2 docs.id "a.txt"
    let parsedId = NodeId.New()
    let graph3 =
        [ Op.NewNode(parsedId, "parsed")
          Op.Replace(file.id, 0, [], [ ChildNode.owner parsedId ]) ]
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
    let graph2 =
        createPaths graph [ "docs/.amb"; "docs/nested/a.txt" ]
        |> markDocumentsCurrent
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
let ``directory rename survives deletion of its last old child`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 =
        createPaths graph [ "docs/.amb"; "docs/old.txt" ]
        |> markDocumentsCurrent
    let docsId = (childNamed graph2 workspaceId "docs").id
    let changes =
        [ LazyLoadReconciliation.Deleted "docs/old.txt"
          LazyLoadReconciliation.Renamed("docs/.amb", "archive/.amb") ]
    let graph3 = requireChangedPlan graph2 "home" changes |> applyOps graph2
    Assert.Equal(docsId, (childNamed graph3 workspaceId "archive").id)

[<Fact>]
let ``exact marker modification invalidates containing documents`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "docs/.amb" ] |> markDocumentsCurrent
    let docs = childNamed graph2 workspaceId "docs"
    Assert.Equal(Current, docs.documentState)
    let graph3 =
        requireChangedPlan
            graph2
            "home"
            [ LazyLoadReconciliation.Modified ".amb"
              LazyLoadReconciliation.Modified "docs/.amb" ]
        |> applyOps graph2
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
let ``rediscovered Added Current file stays Current with children`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "note.txt" ]
    let file = childNamed graph2 workspaceId "note.txt"
    Assert.Equal(Unparsed, file.documentState)
    let parsedId = NodeId.New()
    let attach = ChildNode.owner parsedId
    let current =
        [ Op.SetDocumentState(file.id, Unparsed, Current)
          Op.NewNode(parsedId, "parsed")
          Op.Replace(file.id, 0, [], [ attach ]) ]
        |> applyOps graph2
    Assert.Equal(Current, current.nodes.[file.id].documentState)
    Assert.Equal(parsedId, current.nodes.[file.id].children.Head.id)
    let graph3 =
        requirePlan current "home" [ "note.txt" ] |> applyOps current
    let after = childNamed graph3 workspaceId "note.txt"
    Assert.Equal(file.id, after.id)
    Assert.Equal(Current, after.documentState)
    Assert.Equal(parsedId, after.children.Head.id)

[<Fact>]
let ``x amb remains an ordinary file for rename and delete`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "x.amb" ] |> markDocumentsCurrent
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
    let graph2 = createPaths graph [ "old.txt" ] |> markDocumentsCurrent
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
    let graph2 = createPaths graph [ "old.txt" ] |> markDocumentsCurrent
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

[<Fact>]
let ``report planner keeps sibling ops when one path fails`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "bad.txt" ]
    match
        LazyLoadReconciliationReport.planChangedPathsWithArtifacts
            graph2
            "home"
            [ LazyLoadReconciliation.Deleted "bad.txt"
              LazyLoadReconciliation.Added "good.txt" ]
            Map.empty
    with
    | Error err -> Assert.Fail(err)
    | Ok report ->
        Assert.NotEmpty(report.failures)
        Assert.Contains(
            report.failures,
            fun f ->
                f.path = "bad.txt"
                && f.message.Contains("unparsed document"))
        Assert.NotEmpty(report.ops)
        let graph3 = applyOps graph2 report.ops
        let names =
            ownedNamedChildren graph3 workspaceId
            |> List.map fst
            |> List.sort
        Assert.Equal<string list>([ "bad.txt"; "good.txt" ], names)

[<Fact>]
let ``directory amb ref to existing owned child keeps owner occurrence`` () =
    let workspaceId, graph0 = Graph.create () |> addWorkspace "home"
    let graph1 = createPaths graph0 [ "tasks/active/.amb" ]
    let tasks = childNamed graph1 workspaceId "tasks"
    let active = childNamed graph1 tasks.id "active"
    let sid = AmbDocument.formatStableId active.id
    let artifacts =
        Map.ofList
            [ "tasks/.amb", $"-> //home/tasks/active/^{sid}\n" ]
    match
        LazyLoadReconciliationReport.planChangedPathsWithArtifacts
            graph1
            "home"
            [ LazyLoadReconciliation.Added "tasks/.amb"
              LazyLoadReconciliation.Added "tasks/inbox.txt" ]
            artifacts
    with
    | Error err -> Assert.Fail(err)
    | Ok report ->
        Assert.Empty(report.failures)
        Assert.NotEmpty(report.ops)
        let change =
            { id = 0
              changeId = System.Guid.NewGuid()
              ops = report.ops }
        let state =
            { graph = graph1
              history = History.empty
              revision = Revision.Zero }
        match History.applyChange change state with
        | ApplyResult.Invalid(_, msg) ->
            Assert.Fail($"ownership/apply failed: {msg}")
        | ApplyResult.Unchanged _ -> Assert.Fail("expected Changed")
        | ApplyResult.Changed next ->
            let occurrences =
                next.graph.nodes.[tasks.id].children
                |> List.filter (fun c -> c.id = active.id)
            Assert.Equal(1, occurrences.Length)
            Assert.Equal(Ownership.Owner, occurrences.Head.ref)

[<Fact>]
let ``planAddedPaths creates File under SYSTEM`` () =
    let graph0 = Graph.create ()
    let graph1 =
        requirePlan graph0 "SYSTEM" [ "user.css" ] |> applyOps graph0
    let file = childNamed graph1 Graph.systemId "user.css"
    Assert.Equal(Special File, file.kind)
    Assert.True(Graph.isSpecialSystemDirectoryMember graph1 file.id)
