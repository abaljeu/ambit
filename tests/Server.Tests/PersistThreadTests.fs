module Gambol.Server.Tests.PersistThreadTests

open Xunit
open Gambol.Server
open Gambol.Shared

let private fileGraph (parse: ParseState) (persist: PersistState) =
    let id = NodeId.New()
    let node =
        Node.Create(
            id,
            text = "note.txt",
            name = Filename.create "note.txt",
            kind = Special File,
            documentState = ParseState.toDocumentState parse,
            parseState = parse,
            persistState = persist)
    id, Graph.addDetachedNode node (Graph.create ())

let private edit (id: NodeId) =
    [ Op.SetText(id, "note.txt", "next") ]

let private startWith persistOps persistChange =
    let calls = ResizeArray<string>()
    let finishes = ResizeArray<NodeId * Graph option>()
    let failures = ResizeArray<string>()
    let collect, consumer = PersistCollectors.create ()
    PersistThread.start {
        consumer = consumer
        persistOps =
            fun _ _ post ops ->
                calls.Add("ops")
                persistOps post ops
        persistChange =
            fun _ _ post ->
                calls.Add("change")
                persistChange post
        finish = fun id graph -> finishes.Add(id, graph)
        changeFailed = fun _ err -> failures.Add(err)
    }
    collect, calls, finishes, failures

let private okGraph post _ =
    Ok { graph = post; message = None }

[<Fact>]
let ``open node calls persist ops and adds SnapshotDone`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, calls, finishes, _failures =
        startWith okGraph (fun post -> okGraph post [])
    let outcome =
        collect {
            nodeIds = [ id ]
            dataDir = Some "data"
            preGraph = graph
            postGraph = graph
            ops = edit id
            kind = PersistKind.Ops
            notify = true
            wait = true
        }
    match outcome with
    | PersistOutcome.Wrote stamped ->
        Assert.Equal(graph, stamped.graph)
    | other -> Assert.Fail($"expected Wrote, got {other}")
    Assert.Equal<string>([ "ops" ], calls)
    let finishedId, finishedGraph = Assert.Single(finishes)
    Assert.Equal(id, finishedId)
    Assert.Equal(Some graph, finishedGraph)
    Assert.Equal(PersistState.Unpersisted, graph.nodes.[id].persistState)

[<Fact>]
let ``Unparsed node is blocked and does not call persist`` () =
    let id, graph = fileGraph ParseState.Unparsed PersistState.Unpersisted
    let collect, calls, finishes, _failures =
        startWith okGraph (fun post -> okGraph post [])
    let outcome =
        collect {
            nodeIds = [ id ]
            dataDir = Some "data"
            preGraph = graph
            postGraph = graph
            ops = edit id
            kind = PersistKind.Ops
            notify = true
            wait = true
        }
    match outcome with
    | PersistOutcome.Blocked -> ()
    | other -> Assert.Fail($"expected Blocked, got {other}")
    Assert.Empty(calls)
    Assert.Empty(finishes)
    Assert.Equal(PersistState.Unpersisted, graph.nodes.[id].persistState)

[<Fact>]
let ``Current document with stale Unparsed axis still persists`` () =
    let id = NodeId.New()
    let node =
        Node.Create(
            id,
            text = "note.txt",
            name = Filename.create "note.txt",
            kind = Special File,
            documentState = Current,
            parseState = ParseState.Unparsed,
            persistState = PersistState.Unpersisted)
    let graph = Graph.addDetachedNode node (Graph.create ())
    let collect, calls, finishes, _failures =
        startWith okGraph (fun post -> okGraph post [])
    let outcome =
        collect {
            nodeIds = [ id ]
            dataDir = Some "data"
            preGraph = graph
            postGraph = graph
            ops = edit id
            kind = PersistKind.Ops
            notify = true
            wait = true
        }
    match outcome with
    | PersistOutcome.Wrote _ -> ()
    | other -> Assert.Fail($"expected Wrote, got {other}")
    Assert.Equal<string>([ "ops" ], List.ofSeq calls)
    Assert.NotEmpty(finishes)

[<Fact>]
let ``snapshot change calls persist change and adds SnapshotDone`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, calls, finishes, _failures =
        startWith okGraph (fun post -> okGraph post [])
    let outcome =
        collect {
            nodeIds = [ id ]
            dataDir = Some "data"
            preGraph = graph
            postGraph = graph
            ops = []
            kind = PersistKind.Change
            notify = true
            wait = true
        }
    match outcome with
    | PersistOutcome.Wrote _ -> ()
    | other -> Assert.Fail($"expected Wrote, got {other}")
    Assert.Equal<string>([ "change" ], calls)
    let finishedId, _ = Assert.Single(finishes)
    Assert.Equal(id, finishedId)

[<Fact>]
let ``Change is blocked when any submitted node is Unparsed`` () =
    let openId, graph =
        fileGraph ParseState.Parsed PersistState.Unpersisted
    let blockedId = NodeId.New()
    let blockedNode =
        Node.Create(
            blockedId,
            text = "raw.txt",
            name = Filename.create "raw.txt",
            kind = Special File,
            documentState = Unparsed,
            parseState = ParseState.Unparsed,
            persistState = PersistState.Unpersisted)
    let graph = Graph.addDetachedNode blockedNode graph
    let collect, calls, finishes, _failures =
        startWith okGraph (fun post -> okGraph post [])
    let outcome =
        collect {
            nodeIds = [ openId; blockedId ]
            dataDir = Some "data"
            preGraph = graph
            postGraph = graph
            ops = []
            kind = PersistKind.Change
            notify = true
            wait = true
        }
    match outcome with
    | PersistOutcome.Blocked -> ()
    | other -> Assert.Fail($"expected Blocked, got {other}")
    Assert.Empty(calls)
    Assert.Empty(finishes)

[<Fact>]
let ``failed Change does not add SnapshotDone`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, calls, finishes, failures =
        startWith okGraph (fun _ -> Error "disk full")
    let outcome =
        collect {
            nodeIds = [ id ]
            dataDir = Some "data"
            preGraph = graph
            postGraph = graph
            ops = []
            kind = PersistKind.Change
            notify = true
            wait = true
        }
    match outcome with
    | PersistOutcome.Failed err -> Assert.Equal("disk full", err)
    | other -> Assert.Fail($"expected Failed, got {other}")
    Assert.Equal<string>([ "change" ], calls)
    Assert.Empty(finishes)
    Assert.Equal<string>([ "disk full" ], failures)
    Assert.Equal(PersistState.Unpersisted, graph.nodes.[id].persistState)

[<Fact>]
let ``thrown persist ops return Failed`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, _calls, finishes, failures =
        startWith
            (fun _ _ ->
                raise (System.InvalidOperationException("injected persistence failure")))
            (fun post -> okGraph post [])
    let outcome =
        collect {
            nodeIds = [ id ]
            dataDir = Some "data"
            preGraph = graph
            postGraph = graph
            ops = edit id
            kind = PersistKind.Ops
            notify = true
            wait = true
        }
    match outcome with
    | PersistOutcome.Failed err ->
        Assert.Contains("injected persistence failure", err)
    | other -> Assert.Fail($"expected Failed, got {other}")
    Assert.Empty(finishes)
    Assert.Empty(failures)
    Assert.Equal(PersistState.Unpersisted, graph.nodes.[id].persistState)
