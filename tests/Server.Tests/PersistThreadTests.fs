module Gambol.Server.Tests.PersistThreadTests

open System.Diagnostics
open System.Threading
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
    let pulls = ref 0
    let collect, consumer, _ = PersistCollectors.create ()
    let wrapped () =
        Interlocked.Increment pulls |> ignore
        consumer ()
    PersistThread.start {
        currentState =
            fun () -> { graph = Graph.create (); eventId = EventId.zero }
        consumer = wrapped
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
    |> ignore
    collect, calls, finishes, failures, notices, pulls

let private okGraph post _ =
    Ok { graph = post; message = None }

let private awaitPulls (pulls: int ref) (target: int) =
    let sw = Stopwatch.StartNew()
    while pulls.Value < target && sw.ElapsedMilliseconds < 2000 do
        Thread.Sleep 5
    Assert.True(pulls.Value >= target, $"persist thread pulls {pulls.Value}")

/// Push one job and wait until the thread has finished it.
let private run (collect: PersistSubmit -> unit) (pulls: int ref) (submit: PersistSubmit) =
    awaitPulls pulls 1
    let parked = pulls.Value
    collect submit
    awaitPulls pulls (parked + 1)

[<Fact>]
let ``open node calls persist ops and posts NodeStamped`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, calls, finishes, _failures, notices, pulls =
        startWith okGraph (fun post -> okGraph post [])
    run collect pulls {
        nodeIds = [ id ]
        dataDir = Some "data"
        preGraph = graph
        postGraph = graph
        ops = edit id
        kind = PersistKind.Ops
        notify = true
    }
    Assert.Equal<string>([ "ops" ], calls)
    Assert.Equal(id, Assert.Single(finishes))
    Assert.Equal<Notice list>(
        [ Notice.NodeStamped([], None) ],
        List.ofSeq notices)
    Assert.Equal(
        PersistState.Unpersisted,
        graph.nodes.[id].documentState.persistState)

[<Fact>]
let ``Unparsed node is blocked and does not call persist`` () =
    let id, graph = fileGraph ParseState.Unparsed PersistState.Unpersisted
    let collect, calls, finishes, _failures, notices, pulls =
        startWith okGraph (fun post -> okGraph post [])
    run collect pulls {
        nodeIds = [ id ]
        dataDir = Some "data"
        preGraph = graph
        postGraph = graph
        ops = edit id
        kind = PersistKind.Ops
        notify = true
    }
    Assert.Empty(calls)
    Assert.Empty(finishes)
    Assert.Empty(notices)
    Assert.Equal(
        PersistState.Unpersisted,
        graph.nodes.[id].documentState.persistState)

[<Fact>]
let ``snapshot change calls persist change and adds NodePersisted`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, calls, finishes, _failures, notices, pulls =
        startWith okGraph (fun post -> okGraph post [])
    run collect pulls {
        nodeIds = [ id ]
        dataDir = Some "data"
        preGraph = graph
        postGraph = graph
        ops = []
        kind = PersistKind.Change
        notify = true
    }
    Assert.Equal<string>([ "change" ], calls)
    Assert.Equal(id, Assert.Single(finishes))
    Assert.Empty(notices)

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
    let collect, calls, finishes, _failures, notices, pulls =
        startWith okGraph (fun post -> okGraph post [])
    run collect pulls {
        nodeIds = [ openId; blockedId ]
        dataDir = Some "data"
        preGraph = graph
        postGraph = graph
        ops = []
        kind = PersistKind.Change
        notify = true
    }
    Assert.Empty(calls)
    Assert.Empty(finishes)
    Assert.Empty(notices)

[<Fact>]
let ``failed Change does not add NodePersisted`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let other = NodeId.New()
    let collect, calls, finishes, failures, notices, pulls =
        startWith okGraph (fun _ -> Error "disk full")
    run collect pulls {
        nodeIds = [ id; other ]
        dataDir = Some "data"
        preGraph = graph
        postGraph = graph
        ops = []
        kind = PersistKind.Change
        notify = true
    }
    Assert.Equal<string>([ "change" ], calls)
    Assert.Empty(finishes)
    Assert.Equal<string>([ "disk full" ], failures)
    Assert.Equal<Notice list>(
        [ Notice.PersistFailed(id, "disk full")
          Notice.PersistFailed(other, "disk full") ],
        List.ofSeq notices)
    Assert.Equal(
        PersistState.Unpersisted,
        graph.nodes.[id].documentState.persistState)

[<Fact>]
let ``thrown persist ops post PersistFailed`` () =
    let id, graph = fileGraph ParseState.Parsed PersistState.Unpersisted
    let collect, _calls, finishes, failures, notices, pulls =
        startWith
            (fun _ _ ->
                raise (System.InvalidOperationException("injected persistence failure")))
            (fun post -> okGraph post [])
    run collect pulls {
        nodeIds = [ id ]
        dataDir = Some "data"
        preGraph = graph
        postGraph = graph
        ops = edit id
        kind = PersistKind.Ops
        notify = true
    }
    Assert.Empty(finishes)
    Assert.Empty(failures)
    match Assert.Single(notices) with
    | Notice.PersistFailed (nodeId, message) ->
        Assert.Equal(id, nodeId)
        Assert.Contains("injected persistence failure", message)
    | other -> Assert.Fail($"expected PersistFailed, got {other}")
    Assert.Equal(
        PersistState.Unpersisted,
        graph.nodes.[id].documentState.persistState)
