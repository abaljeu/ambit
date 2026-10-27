module LazyLoadReconciliationTests

open Gambol.Shared
open Xunit

[<CollectionDefinition("ExclusiveTiming", DisableParallelization = true)>]
type ExclusiveTimingCollection() = class end

/// Graph-only post: guardUnparsed false. NodeParsed sets Current after.
let private applyGraphOnly (graph: Graph) (ops: Op list) : Graph =
    if List.isEmpty ops then
        graph
    else
        let state = { graph = graph; eventId = EventId.zero }
        match
            ChangeAmendment.applyForGraphOnly EventId.zero "Parse" ops state
        with
        | ApplyResult.Changed next, _, _
        | ApplyResult.Unchanged next, _, _ -> next.graph
        | ApplyResult.Invalid(_, msg), _, _ -> failwith msg

let private applyNodeParsed (graph: Graph) (nodeId: NodeId) : Graph =
    let event =
        { id = EventId.zero
          submissionId = System.Guid.NewGuid()
          authority = Authority "Parse"
          commandName = ""
          body = EventBody.Notice(Notice.NodeParsed nodeId) }
    match Ev.apply event { graph = graph; eventId = EventId.zero } with
    | ApplyResult.Changed next
    | ApplyResult.Unchanged next -> next.graph
    | ApplyResult.Invalid(_, msg) -> failwith msg

let private applyOps (graph: Graph) (ops: Op list) : Graph =
    let state = { graph = graph; eventId = EventId.zero }
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
    Graph.children graph parentId
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

let private requireReceivedReport graph label changes =
    match
        LazyLoadReconciliationReport.planReceivedFiles
            graph
            label
            changes
            Map.empty
    with
    | Ok report ->
        Assert.Empty(report.failures)
        report
    | Error err -> failwith err

let private requireReceivedPlan graph label changes =
    (requireReceivedReport graph label changes).ops

let private requireAddedReport graph paths =
    match
        LazyLoadReconciliationReport.planChangedPathsWithArtifacts
            graph
            "home"
            (paths |> List.map LazyLoadReconciliation.Added)
            Map.empty
    with
    | Ok report ->
        Assert.Empty(report.failures)
        report
    | Error err -> failwith err

let private setsUnparsed id = function
    | Op.SetDocumentState(found, _, Unparsed) when found = id -> true
    | _ -> false

let private setsCurrent id = function
    | Op.SetDocumentState(found, _, Current) when found = id -> true
    | _ -> false

let private applyNotice (graph: Graph) (notice: Notice) : Graph =
    let event =
        { id = EventId.zero
          submissionId = System.Guid.Empty
          authority = Authority "test"
          commandName = ""
          body = EventBody.Notice notice }
    match Ev.apply event { graph = graph; eventId = EventId.zero } with
    | ApplyResult.Changed next
    | ApplyResult.Unchanged next -> next.graph
    | ApplyResult.Invalid(_, msg) -> failwith msg

let private applyUnparsedNotice (graph: Graph) nodeId =
    applyNotice graph (Notice.NodeUnparsed nodeId)

/// Same ids the plan chose, except a PullingFile container.
let private containerNotices (graph: Graph) (ids: NodeId list) =
    ids
    |> List.choose (fun id ->
        match Map.tryFind id graph.nodes with
        | Some node when Node.documentStateEnum node <> PullingFile ->
            Some(Notice.NodeUnparsed id)
        | _ -> None)

/// Ops, then container NodeUnparsed. Stub owners stay Unparsed.
let private applyReceived
    (graph: Graph)
    (report: LazyLoadReconciliationReport.Report)
    =
    let afterOps = applyOps graph report.ops
    report.containingDirIds
    |> containerNotices graph
    |> List.fold (fun current notice -> applyNotice current notice) afterOps

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
        match node.kind, Node.documentStateEnum node with
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
    Assert.Equal(Unparsed, (snd children.Head).documentState.AsEnum)
    Assert.Empty(Graph.children graph2 (snd children.Head).id)

[<Fact>]
let ``nested file parse leaves the new directory Unparsed without NodeParsed`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let report = requireAddedReport graph [ "src/note.txt" ]
    let built = applyOps graph report.ops
    let src = childNamed built workspaceId "src"
    let file = childNamed built src.id "note.txt"
    Assert.DoesNotContain(report.ops, setsCurrent src.id)
    Assert.DoesNotContain(report.ops, setsCurrent workspaceId)
    Assert.Equal(Unparsed, src.documentState.AsEnum)
    Assert.Equal(Unparsed, built.nodes.[workspaceId].documentState.AsEnum)
    Assert.Equal(Unparsed, file.documentState.AsEnum)
    let parsedId = NodeId.New()
    let attach = ChildNode.owner parsedId
    let state =
        { graph = built; eventId = EventId.zero }
    let parseChange =
        { id = EventId.zero
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change
            [ Op.SetDocumentState(file.id, Unparsed, Current)
              Op.NewNode(parsedId, "parsed")
              Op.Replace(file.id, [], [ attach ]) ] }
    match SpecialNodeTestHelpers.applyChange parseChange state with
    | ApplyResult.Changed next ->
        Assert.Equal(Current, next.graph.nodes.[file.id].documentState.AsEnum)
        Assert.Equal(Unparsed, next.graph.nodes.[src.id].documentState.AsEnum)
        Assert.Equal(parsedId, (Graph.children next.graph file.id).Head.id)
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
let ``upload-built directories stay Unparsed without NodeParsed`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let report = requireAddedReport graph [ "src/lib/core.fs" ]
    let built = applyOps graph report.ops
    let src = childNamed built workspaceId "src"
    let lib = childNamed built src.id "lib"
    let file = childNamed built lib.id "core.fs"
    Assert.DoesNotContain(report.ops, setsCurrent workspaceId)
    Assert.DoesNotContain(report.ops, setsCurrent src.id)
    Assert.DoesNotContain(report.ops, setsCurrent lib.id)
    Assert.Equal(Unparsed, src.documentState.AsEnum)
    Assert.Equal(Unparsed, lib.documentState.AsEnum)
    Assert.Equal(Unparsed, file.documentState.AsEnum)
    Assert.Equal(Unparsed, built.nodes.[workspaceId].documentState.AsEnum)

[<Fact>]
let ``exact amb add marks directory stub unparsed`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = requirePlan graph "home" [ "docs/.amb" ] |> applyOps graph
    let docs = childNamed graph2 workspaceId "docs"
    Assert.Equal(Special Directory, docs.kind)
    Assert.Equal(Unparsed, docs.documentState.AsEnum)

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
    let parsed = applyGraphOnly graph ops
    let docsId = (childNamed parsed workspaceId "docs").id
    let graph2 = applyNodeParsed parsed docsId
    let docs = graph2.nodes.[docsId]
    Assert.Equal(Current, docs.documentState.AsEnum)
    Assert.Equal(1, (Graph.children graph2 docs.id).Length)
    Assert.Equal("outline body", graph2.nodes.[(Graph.children graph2 docs.id).Head.id].text)

let private requirePersist nodeId persistState graph =
    match Graph.setPersistState nodeId persistState graph with
    | Ok g -> g
    | Error err -> failwith err

[<Fact>]
let ``disk parse of a directory sets Parsed and keeps PersistState`` () =
    for persist in [ PersistState.Persisted; PersistState.Unpersisted ] do
        let workspaceId, graph = Graph.create () |> addWorkspace "home"
        let artifacts =
            Map.ofList [ "docs/.amb", "alpha" + System.Environment.NewLine ]
        let ops1 =
            match
                LazyLoadReconciliation.planAddedPathsWithArtifacts
                    graph
                    "home"
                    [ "docs/.amb" ]
                    artifacts
            with
            | Ok o -> o
            | Error err -> failwith err
        let parsed = applyGraphOnly graph ops1
        let docs = childNamed parsed workspaceId "docs"
        let seeded =
            applyNodeParsed parsed docs.id
            |> requirePersist docs.id persist
            |> requirePersist workspaceId persist
        let edited = Map.ofList [ "docs/.amb", "beta" + System.Environment.NewLine ]
        let ops2 =
            match
                LazyLoadReconciliation.planChangedPathsWithArtifacts
                    seeded
                    "home"
                    [ LazyLoadReconciliation.Modified "docs/.amb" ]
                    edited
            with
            | Ok o -> o
            | Error err -> failwith err
        let graph3 = applyGraphOnly seeded ops2
        Assert.Equal(ParseState.Parsed, graph3.nodes.[docs.id].documentState.parseState)
        Assert.Equal(Current, graph3.nodes.[docs.id].documentState.AsEnum)
        Assert.Equal(persist, graph3.nodes.[docs.id].documentState.persistState)
        Assert.Equal(persist, graph3.nodes.[workspaceId].documentState.persistState)
        Assert.Equal("beta", graph3.nodes.[(Graph.children graph3 docs.id).Head.id].text)

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
    let graph3 =
        applyGraphOnly graph2 ops |> fun parsed -> applyNodeParsed parsed docs.id
    Assert.Equal(Current, graph3.nodes.[docs.id].documentState.AsEnum)
    Assert.Equal(
        "fresh",
        graph3.nodes.[(Graph.children graph3 docs.id).Head.id].text)

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
    let parsed = applyGraphOnly graph ops1
    let docs = childNamed parsed workspaceId "docs"
    let graph2 = applyNodeParsed parsed docs.id
    let childId = (Graph.children graph2 docs.id).Head.id
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
    let graph3 = applyGraphOnly graph2 ops2
    Assert.Equal(childId, (Graph.children graph3 docs.id).Head.id)
    Assert.Equal("ALPHA", graph3.nodes.[childId].text)

[<Fact>]
let ``repeated reconciliation reuses matching stubs`` () =
    let _, graph = Graph.create () |> addWorkspace "home"
    let graph2 = requirePlan graph "home" [ "src/core.fs" ] |> applyOps graph
    let second = requirePlan graph2 "home" [ "src/core.fs"; "src/core.fs" ]
    Assert.Empty(second)

[<Fact>]
let ``reconciliation finds artifacts through normal organizers`` () =
    let workspaceId, created = Graph.create () |> addWorkspace "home"
    let graph0 =
        applyOps created [ Op.SetDocumentState(workspaceId, Unparsed, Current) ]
    let graph1, workspaceOrganizerId = Graph.newNode "organizer" graph0
    let graph2 =
        [ Op.Replace(
              workspaceId,
              [],
              [ ChildNode.owner workspaceOrganizerId ]) ]
        |> applyOps graph1
    let srcId, srcOps =
        FileNodeOps.planCreateOwnedDirectory graph2 workspaceOrganizerId "src"
    let graph3 =
        applyOps graph2 srcOps
        |> fun g -> applyOps g [ Op.SetDocumentState(srcId, Unparsed, Current) ]
    let graph4, srcOrganizerId = Graph.newNode "nested organizer" graph3
    let graph5 =
        [ Op.Replace(
              srcId,
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
let ``exact Directory File represents its containing directory`` () =
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
let ``leading-dot directory File represents containing Directory named .scratch`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 =
        requirePlan graph "home" [ ".scratch/.amb" ] |> applyOps graph
    let scratch =
        ownedNamedChildren graph2 workspaceId
        |> List.find (fst >> (=) ".scratch")
        |> snd
    Assert.Equal(Special Directory, scratch.kind)
    Assert.Equal(Filename.Ok ".scratch", scratch.name)

[<Fact>]
let ``reserved gambol dot files are excluded from reconciliation`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let paths = [ "gambol.events"; "nested/GAMBOL.meta"; "gambol" ]
    let graph2 = requirePlan graph "home" paths |> applyOps graph
    let children = ownedNamedChildren graph2 workspaceId
    Assert.DoesNotContain(children, fun (name, _) -> name = "gambol.events")
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
          Op.Replace(file.id, [], [ ChildNode.owner parsedId ]) ]
        |> applyOps graph2
    let graph4 =
        requireChangedPlan graph3 "home" [ LazyLoadReconciliation.Deleted "docs/readme.txt" ]
        |> applyOps graph3
    Assert.Contains(
        Graph.children graph4 Graph.trashId,
        fun child -> child.id = docs.id && child.ref = Ownership.Owner)
    Assert.Equal(parsedId, (Graph.children graph4 file.id).Head.id)

[<Fact>]
let ``deleted file refs become path expressions without promotion`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "note.txt" ] |> markDocumentsCurrent
    let file = childNamed graph2 workspaceId "note.txt"
    let holderId = NodeId.New()
    let graph3 =
        [ Op.NewNode(holderId, "holder")
          Op.Replace(Graph.rootId, [], [ ChildNode.owner holderId ])
          Op.Replace(holderId, [], [ ChildNode.reference file.id ]) ]
        |> applyOps graph2
    let graph4 =
        requireChangedPlan graph3 "home" [ LazyLoadReconciliation.Deleted "note.txt" ]
        |> applyOps graph3
    let replacement = Graph.children graph4 holderId |> List.exactlyOne
    Assert.Equal(Ownership.Owner, replacement.ref)
    Assert.Equal("[[//home/note.txt]]", graph4.nodes.[replacement.id].text)
    Assert.Contains(Graph.children graph4 Graph.trashId, fun child -> child.id = file.id)

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
          Op.Replace(file.id, [], [ ChildNode.owner parsedId ]) ]
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
    Assert.Equal(parsedId, (Graph.children moved movedFile.id).Head.id)

[<Fact>]
let ``Directory File rename coalesces nested renames`` () =
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
let ``exact Directory File modification invalidates containing documents`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "docs/.amb" ] |> markDocumentsCurrent
    let docs = childNamed graph2 workspaceId "docs"
    Assert.Equal(Current, docs.documentState.AsEnum)
    let report =
        requireReceivedReport
            graph2
            "home"
            [ LazyLoadReconciliation.Modified ".amb"
              LazyLoadReconciliation.Modified "docs/.amb" ]
    Assert.Contains(workspaceId, report.unparsedIds)
    Assert.Contains(docs.id, report.unparsedIds)
    Assert.DoesNotContain(report.ops, setsUnparsed workspaceId)
    Assert.DoesNotContain(report.ops, setsUnparsed docs.id)
    let graph3 =
        report.unparsedIds
        |> List.fold applyUnparsedNotice graph2
    Assert.Equal(Unparsed, graph3.nodes.[workspaceId].documentState.AsEnum)
    Assert.Equal(Unparsed, graph3.nodes.[docs.id].documentState.AsEnum)

[<Fact>]
let ``modified file keeps identity and records an unparsed id`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "note.txt" ]
    let file = childNamed graph2 workspaceId "note.txt"
    let current =
        Op.SetDocumentState(file.id, Unparsed, Current)
        |> List.singleton
        |> applyOps graph2
    match
        LazyLoadReconciliationReport.planChangedPathsWithArtifacts
            current
            "home"
            [ LazyLoadReconciliation.Modified "note.txt" ]
            Map.empty
    with
    | Error err -> failwith err
    | Ok report ->
        Assert.Contains(file.id, report.unparsedIds)
        Assert.DoesNotContain(report.ops, setsUnparsed file.id)
        let graph3 = applyOps current report.ops
        let after = childNamed graph3 workspaceId "note.txt"
        Assert.Equal(file.id, after.id)
        Assert.Equal(Current, after.documentState.AsEnum)

[<Fact>]
let ``rediscovered Added Current file stays Current with children`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph2 = createPaths graph [ "note.txt" ]
    let file = childNamed graph2 workspaceId "note.txt"
    Assert.Equal(Unparsed, file.documentState.AsEnum)
    let parsedId = NodeId.New()
    let attach = ChildNode.owner parsedId
    let current =
        [ Op.SetDocumentState(file.id, Unparsed, Current)
          Op.NewNode(parsedId, "parsed")
          Op.Replace(file.id, [], [ attach ]) ]
        |> applyOps graph2
    Assert.Equal(Current, current.nodes.[file.id].documentState.AsEnum)
    Assert.Equal(parsedId, (Graph.children current file.id).Head.id)
    let graph3 =
        requirePlan current "home" [ "note.txt" ] |> applyOps current
    let after = childNamed graph3 workspaceId "note.txt"
    Assert.Equal(file.id, after.id)
    Assert.Equal(Current, after.documentState.AsEnum)
    Assert.Equal(parsedId, (Graph.children graph3 after.id).Head.id)

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
    Assert.Contains(Graph.children graph4 Graph.trashId, fun child -> child.id = original.id)

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
    Assert.Contains(Graph.children graph3 Graph.trashId, fun child -> child.id = oldId)

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
let ``deleting exact Directory File alone keeps containing directory`` () =
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
            { id = EventId.zero
              submissionId = System.Guid.NewGuid()
              authority = Authority "Browser"
              commandName = ""
              body = EventBody.Change report.ops }
        let state =
            { graph = graph1
              eventId = EventId.zero }
        match SpecialNodeTestHelpers.applyChange change state with
        | ApplyResult.Invalid(_, msg) ->
            Assert.Fail($"ownership/apply failed: {msg}")
        | ApplyResult.Unchanged _ -> Assert.Fail("expected Changed")
        | ApplyResult.Changed next ->
            let occurrences =
                Graph.children next.graph tasks.id
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

let private seedCurrentDirsWithAmb
    (count: int)
    : Graph * Map<string, string> * LazyLoadReconciliation.ChangedPath list =
    let _, graph0 = Graph.create () |> addWorkspace "home"
    let paths =
        [ for i in 1..count do
              yield $"d{i}/.amb" ]
    let artifacts =
        paths
        |> List.map (fun p -> p, "outline" + System.Environment.NewLine)
        |> Map.ofList
    let ops =
        match
            LazyLoadReconciliationReport.planChangedPathsWithArtifacts
                graph0
                "home"
                (paths |> List.map LazyLoadReconciliation.Added)
                artifacts
        with
        | Ok report -> report.ops
        | Error err -> failwith err
    let graph1 = applyGraphOnly graph0 ops |> markDocumentsCurrent
    let changes = paths |> List.map LazyLoadReconciliation.Added
    graph1, artifacts, changes

[<Fact>]
let ``rediscovered Current Directory Files skip parse on Added`` () =
    let workspaceId, graph0 = Graph.create () |> addWorkspace "home"
    let artifacts =
        Map.ofList [ "docs/.amb", "outline" + System.Environment.NewLine ]
    let ops1 =
        match
            LazyLoadReconciliationReport.planChangedPathsWithArtifacts
                graph0
                "home"
                [ LazyLoadReconciliation.Added "docs/.amb" ]
                artifacts
        with
        | Ok report -> report.ops
        | Error err -> failwith err
    let graph1 = applyGraphOnly graph0 ops1 |> markDocumentsCurrent
    let docs = childNamed graph1 workspaceId "docs"
    Assert.Equal("outline", graph1.nodes.[(Graph.children graph1 docs.id).Head.id].text)
    // Divergent disk text must not apply while stub stays Current.
    let poison =
        Map.ofList [ "docs/.amb", "POISON" + System.Environment.NewLine ]
    match
        LazyLoadReconciliationReport.planChangedPathsWithArtifacts
            graph1
            "home"
            [ LazyLoadReconciliation.Added "docs/.amb" ]
            poison
    with
    | Error err -> Assert.Fail(err)
    | Ok report ->
        Assert.Empty(report.failures)
        Assert.Empty(report.ops)
        Assert.Equal(
            "outline",
            graph1.nodes.[(Graph.children graph1 docs.id).Head.id].text)

[<Fact>]
let ``rediscovered Unparsed Directory File still parses on Added`` () =
    let workspaceId, graph0 = Graph.create () |> addWorkspace "home"
    let graph1 = createPaths graph0 [ "docs/.amb" ]
    let docs = childNamed graph1 workspaceId "docs"
    Assert.Equal(Unparsed, docs.documentState.AsEnum)
    let artifacts =
        Map.ofList [ "docs/.amb", "body" + System.Environment.NewLine ]
    match
        LazyLoadReconciliationReport.planChangedPathsWithArtifacts
            graph1
            "home"
            [ LazyLoadReconciliation.Added "docs/.amb" ]
            artifacts
    with
    | Error err -> Assert.Fail(err)
    | Ok report ->
        Assert.Empty(report.failures)
        Assert.NotEmpty(report.ops)
        let graph2 =
            let parsed = applyGraphOnly graph1 report.ops
            applyNodeParsed parsed docs.id
        let docs2 = graph2.nodes.[docs.id]
        Assert.Equal(Current, docs2.documentState.AsEnum)
        Assert.Equal(1, (Graph.children graph2 docs2.id).Length)
        Assert.Equal("body", graph2.nodes.[(Graph.children graph2 docs2.id).Head.id].text)

[<Fact>]
let ``new Directory File Added still parses outline`` () =
    let workspaceId, graph0 = Graph.create () |> addWorkspace "home"
    let artifacts =
        Map.ofList [ "fresh/.amb", "hello" + System.Environment.NewLine ]
    match
        LazyLoadReconciliationReport.planChangedPathsWithArtifacts
            graph0
            "home"
            [ LazyLoadReconciliation.Added "fresh/.amb" ]
            artifacts
    with
    | Error err -> Assert.Fail(err)
    | Ok report ->
        Assert.Empty(report.failures)
        Assert.NotEmpty(report.ops)
        let parsed = applyGraphOnly graph0 report.ops
        let freshId = (childNamed parsed workspaceId "fresh").id
        let graph1 = applyNodeParsed parsed freshId
        let fresh = graph1.nodes.[freshId]
        Assert.Equal(Current, fresh.documentState.AsEnum)
        Assert.Equal("hello", graph1.nodes.[(Graph.children graph1 fresh.id).Head.id].text)

[<Fact>]
let ``received file changes keep files at any depth and drop directory paths`` () =
    let changes =
        [ LazyLoadReconciliation.Added "top.txt"
          LazyLoadReconciliation.Added "docs/nested.txt"
          LazyLoadReconciliation.Modified "docs/.amb"
          LazyLoadReconciliation.Renamed("old.txt", "new.txt")
          LazyLoadReconciliation.Renamed("dir/.amb", "dir2/.amb") ]
    let files = LazyLoadReconciliation.receivedFileChanges changes
    Assert.Equal<LazyLoadReconciliation.ChangedPath list>(
        [ LazyLoadReconciliation.Added "top.txt"
          LazyLoadReconciliation.Added "docs/nested.txt"
          LazyLoadReconciliation.Renamed("old.txt", "new.txt") ],
        files)

[<Fact>]
let ``received file plan records top and nested unparsed ids and leaves the directory`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph1 = createPaths graph [ "top.txt"; "docs/nested.txt" ]
    let graph2 = markDocumentsCurrent graph1
    let docs = childNamed graph2 workspaceId "docs"
    let top = childNamed graph2 workspaceId "top.txt"
    let nested = childNamed graph2 docs.id "nested.txt"
    let changes =
        LazyLoadReconciliation.receivedFileChanges
            [ LazyLoadReconciliation.Modified "top.txt"
              LazyLoadReconciliation.Modified "docs/nested.txt"
              LazyLoadReconciliation.Modified "docs/.amb" ]
    let report = requireReceivedReport graph2 "home" changes
    Assert.Contains(top.id, report.unparsedIds)
    Assert.Contains(nested.id, report.unparsedIds)
    Assert.DoesNotContain(report.ops, setsUnparsed top.id)
    Assert.DoesNotContain(report.ops, setsUnparsed nested.id)
    let graph3 = applyOps graph2 report.ops
    Assert.Equal(Current, graph3.nodes.[top.id].documentState.AsEnum)
    Assert.Equal(Current, graph3.nodes.[nested.id].documentState.AsEnum)
    Assert.Equal(Current, graph3.nodes.[docs.id].documentState.AsEnum)

[<Fact>]
let ``received file add marks the containing directory Unparsed`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph1 = createPaths graph [ "docs/keep.txt" ]
    let graph2 = markDocumentsCurrent graph1
    let docs = childNamed graph2 workspaceId "docs"
    let report =
        requireReceivedReport
            graph2
            "home"
            [ LazyLoadReconciliation.Added "docs/new.txt" ]
    Assert.Equal<NodeId list>([ docs.id ], report.containingDirIds)
    Assert.Equal<Notice list>(
        [ Notice.NodeUnparsed docs.id ],
        containerNotices graph2 report.containingDirIds)
    let graph3 = applyReceived graph2 report
    Assert.Equal(Unparsed, graph3.nodes.[docs.id].documentState.AsEnum)
    Assert.Equal(Unparsed, (childNamed graph3 docs.id "new.txt").documentState.AsEnum)

[<Fact>]
let ``received file add leaves an Unparsed directory Unparsed`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph1 = createPaths graph [ "docs/keep.txt" ]
    let graph2 = markDocumentsCurrent graph1
    let docs = childNamed graph2 workspaceId "docs"
    let graphUnparsed =
        applyOps graph2 [ Op.SetDocumentState(docs.id, Current, Unparsed) ]
    let report =
        requireReceivedReport
            graphUnparsed
            "home"
            [ LazyLoadReconciliation.Added "docs/new.txt" ]
    Assert.Equal<NodeId list>([ docs.id ], report.containingDirIds)
    Assert.Equal<Notice list>(
        [ Notice.NodeUnparsed docs.id ],
        containerNotices graphUnparsed report.containingDirIds)
    let graph3 = applyReceived graphUnparsed report
    Assert.Equal(Unparsed, graph3.nodes.[docs.id].documentState.AsEnum)

[<Fact>]
let ``received file removal marks the containing directory Unparsed`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph1 = createPaths graph [ "docs/keep.txt"; "docs/gone.txt" ]
    let graph2 = markDocumentsCurrent graph1
    let docs = childNamed graph2 workspaceId "docs"
    let report =
        requireReceivedReport
            graph2
            "home"
            [ LazyLoadReconciliation.Deleted "docs/gone.txt" ]
    Assert.Equal<NodeId list>([ docs.id ], report.containingDirIds)
    Assert.Equal<Notice list>(
        [ Notice.NodeUnparsed docs.id ],
        containerNotices graph2 report.containingDirIds)
    let graph3 = applyReceived graph2 report
    Assert.Equal(Unparsed, graph3.nodes.[docs.id].documentState.AsEnum)
    Assert.Equal(Current, (childNamed graph3 docs.id "keep.txt").documentState.AsEnum)

[<Fact>]
let ``received file rename marks both directories Unparsed`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph1 =
        createPaths graph [ "docs/keep.txt"; "docs/move.txt"; "other/keep.txt" ]
    let graph2 = markDocumentsCurrent graph1
    let docs = childNamed graph2 workspaceId "docs"
    let other = childNamed graph2 workspaceId "other"
    let report =
        requireReceivedReport
            graph2
            "home"
            [ LazyLoadReconciliation.Renamed("docs/move.txt", "other/move.txt") ]
    Assert.Equal<NodeId list>(
        [ docs.id; other.id ],
        report.containingDirIds)
    Assert.Equal<Notice list>(
        [ Notice.NodeUnparsed docs.id; Notice.NodeUnparsed other.id ],
        containerNotices graph2 report.containingDirIds)
    let graph3 = applyReceived graph2 report
    Assert.Equal(Unparsed, graph3.nodes.[docs.id].documentState.AsEnum)
    Assert.Equal(Unparsed, graph3.nodes.[other.id].documentState.AsEnum)

[<Fact>]
let ``received file at the workspace top marks the workspace Unparsed`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph1 = markDocumentsCurrent graph
    let report =
        requireReceivedReport
            graph1
            "home"
            [ LazyLoadReconciliation.Added "top.txt" ]
    Assert.Equal<NodeId list>([ workspaceId ], report.containingDirIds)
    Assert.Equal<Notice list>(
        [ Notice.NodeUnparsed workspaceId ],
        containerNotices graph1 report.containingDirIds)
    let graph2 = applyReceived graph1 report
    Assert.Equal(Unparsed, graph2.nodes.[workspaceId].documentState.AsEnum)

[<Fact>]
let ``received file at the workspace top leaves an Unparsed workspace Unparsed`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph1 = markDocumentsCurrent graph
    let graphUnparsed =
        applyOps graph1 [ Op.SetDocumentState(workspaceId, Current, Unparsed) ]
    let report =
        requireReceivedReport
            graphUnparsed
            "home"
            [ LazyLoadReconciliation.Added "top.txt" ]
    Assert.Equal<NodeId list>([ workspaceId ], report.containingDirIds)
    Assert.Equal<Notice list>(
        [ Notice.NodeUnparsed workspaceId ],
        containerNotices graphUnparsed report.containingDirIds)
    let graph2 = applyReceived graphUnparsed report
    Assert.Equal(Unparsed, graph2.nodes.[workspaceId].documentState.AsEnum)

[<Fact>]
let ``received file modification leaves the containing directory Current`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph1 = createPaths graph [ "docs/note.txt" ]
    let graph2 = markDocumentsCurrent graph1
    let docs = childNamed graph2 workspaceId "docs"
    let note = childNamed graph2 docs.id "note.txt"
    let report =
        requireReceivedReport
            graph2
            "home"
            [ LazyLoadReconciliation.Modified "docs/note.txt" ]
    Assert.Empty(report.containingDirIds)
    let graph3 = applyOps graph2 report.ops
    Assert.Equal(Current, graph3.nodes.[docs.id].documentState.AsEnum)
    Assert.Contains(note.id, report.unparsedIds)
    Assert.DoesNotContain(report.ops, setsUnparsed note.id)

[<Fact>]
let ``received file add marks the nearest existing ancestor directory`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph1 = createPaths graph [ "w/x/keep.txt" ]
    let graph2 = markDocumentsCurrent graph1
    let w = childNamed graph2 workspaceId "w"
    let x = childNamed graph2 w.id "x"
    let report =
        requireReceivedReport
            graph2
            "home"
            [ LazyLoadReconciliation.Added "w/x/y/z" ]
    Assert.Equal<NodeId list>([ x.id ], report.containingDirIds)
    Assert.Equal<Notice list>(
        [ Notice.NodeUnparsed x.id ],
        containerNotices graph2 report.containingDirIds)
    let graph3 = applyReceived graph2 report
    Assert.Equal(Unparsed, graph3.nodes.[x.id].documentState.AsEnum)
    Assert.Equal(Current, graph3.nodes.[w.id].documentState.AsEnum)
    Assert.Equal(Current, graph3.nodes.[workspaceId].documentState.AsEnum)

[<Fact>]
let ``received file add leaves an Unparsed nearest ancestor Unparsed`` () =
    let workspaceId, graph = Graph.create () |> addWorkspace "home"
    let graph1 = createPaths graph [ "w/x/keep.txt" ]
    let graph2 = markDocumentsCurrent graph1
    let w = childNamed graph2 workspaceId "w"
    let x = childNamed graph2 w.id "x"
    let graphUnparsed =
        applyOps graph2 [ Op.SetDocumentState(x.id, Current, Unparsed) ]
    let report =
        requireReceivedReport
            graphUnparsed
            "home"
            [ LazyLoadReconciliation.Added "w/x/y/z" ]
    Assert.Equal<NodeId list>([ x.id ], report.containingDirIds)
    Assert.Equal<Notice list>(
        [ Notice.NodeUnparsed x.id ],
        containerNotices graphUnparsed report.containingDirIds)
    let graph3 = applyReceived graphUnparsed report
    Assert.Equal(Unparsed, graph3.nodes.[x.id].documentState.AsEnum)
    Assert.Equal(Current, graph3.nodes.[w.id].documentState.AsEnum)

[<Collection("ExclusiveTiming")>]
type SecondRediscoveryTiming() =
    [<Fact>]
    member _.``second Added rediscovery of Current dirs stays under 100ms`` () =
        let graph1, artifacts, changes = seedCurrentDirsWithAmb 80
        match
            LazyLoadReconciliationReport.planChangedPathsWithArtifacts
                graph1
                "home"
                changes
                artifacts
        with
        | Error err -> Assert.Fail(err)
        | Ok warmup -> Assert.Empty(warmup.ops)
        let sw = System.Diagnostics.Stopwatch.StartNew()
        match
            LazyLoadReconciliationReport.planChangedPathsWithArtifacts
                graph1
                "home"
                changes
                artifacts
        with
        | Error err -> Assert.Fail(err)
        | Ok report ->
            sw.Stop()
            Assert.Empty(report.ops)
            Assert.True(
                sw.ElapsedMilliseconds < 100L,
                $"second rediscovery took {sw.ElapsedMilliseconds}ms")
