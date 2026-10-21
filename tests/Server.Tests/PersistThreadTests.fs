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
    let finishes = ResizeArray<NodeId>()
    let failures = ResizeArray<string>()
    let notices = ResizeArray<Notice>()
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
        finish = finishes.Add
        postNotice = notices.Add
        changeFailed = fun _ err -> failures.Add(err)
    }
    collect, calls, finishes, failures, notices

let private okGraph post _ =
    Ok { graph = post; message = None }

[<Fact>]
let ``open node calls persist ops and adds SnapshotDone`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, calls, finishes, _failures, _notices =
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
    Assert.Equal(id, Assert.Single(finishes))
    Assert.Equal(PersistState.Unpersisted, graph.nodes.[id].documentState.persistState)

[<Fact>]
let ``Unparsed node is blocked and does not call persist`` () =
    let id, graph = fileGraph ParseState.Unparsed PersistState.Unpersisted
    let collect, calls, finishes, _failures, notices =
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
    Assert.Empty(notices)
    Assert.Equal(PersistState.Unpersisted, graph.nodes.[id].documentState.persistState)

[<Fact>]
let ``snapshot change calls persist change and adds SnapshotDone`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, calls, finishes, _failures, _notices =
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
    Assert.Equal(id, Assert.Single(finishes))

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
    let collect, calls, finishes, _failures, notices =
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
    Assert.Empty(notices)

[<Fact>]
let ``failed Change does not add SnapshotDone`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let other = NodeId.New()
    let collect, calls, finishes, failures, notices =
        startWith okGraph (fun _ -> Error "disk full")
    let outcome =
        collect {
            nodeIds = [ id; other ]
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
    Assert.Equal<Notice list>(
        [ Notice.PersistFailed(id, "disk full")
          Notice.PersistFailed(other, "disk full") ],
        List.ofSeq notices)
    Assert.Equal(PersistState.Unpersisted, graph.nodes.[id].documentState.persistState)

[<Fact>]
let ``thrown persist ops return Failed`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, _calls, finishes, failures, notices =
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
    match Assert.Single(notices) with
    | Notice.PersistFailed (nodeId, message) ->
        Assert.Equal(id, nodeId)
        Assert.Contains("injected persistence failure", message)
    | other -> Assert.Fail($"expected PersistFailed, got {other}")
    Assert.Equal(PersistState.Unpersisted, graph.nodes.[id].documentState.persistState)
