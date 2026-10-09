module Gambol.Server.Tests.CoreLoopInMsgTests

open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private fileState persist parse =
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
    let state =
        { graph = Graph.addDetachedNode node (Graph.create ())
          eventId = EventId.zero }
    id, state

let private handlers
    (state: State ref)
    (snaps: ResizeArray<NodeId * Graph option>)
    : PersistHandlers =
    { getState = fun () -> Ok state.Value
      getEventId = fun () -> Ok state.Value.eventId
      getEventsSince = fun _ -> Ok []
      getEventLog = fun () -> Ok EventLog.empty
      appendEvent = fun _ -> Ok ()
      applyEvent = fun _ _ -> Error "unused"
      replaceGraph =
        fun graph ->
            state.Value <- { state.Value with graph = graph }
      snapshotDone = fun nodeId graph -> snaps.Add(nodeId, graph)
      noteParsed = ignore }

let private filling state snaps : PersistFilling =
    { handlers = handlers state snaps
      onError = fun _ _ _ -> ()
      formatError = id
      isReady = fun () -> true
      flushSnapshot = fun () -> async { return Ok () }
      dispose = fun () -> ()
      until = None
      bindSnapshot = ignore }

let private requireState label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private hostFor state snaps =
    CoreMailbox.host
        (CoreActorPool.create ())
        (filling state snaps)
        admittedCredentials

let private apply persist msg =
    match CoreMailboxLoad.applyInMsg persist msg with
    | Ok () -> ()
    | Error err -> Assert.Fail(err)

[<Fact>]
let ``ParseFinished sets Parsed and leaves PersistState`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Unparsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    apply (handlers state snaps) (InMsg.ParseFinished id)
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(ParseState.Parsed, node.parseState)
    Assert.Equal(Current, node.documentState)
    Assert.Equal(PersistState.Unpersisted, node.persistState)
    Assert.Empty(snaps)

[<Fact>]
let ``MarkUnparsed sets Unparsed and leaves PersistState`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    apply (handlers state snaps) (InMsg.MarkUnparsed id)
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(ParseState.Unparsed, node.parseState)
    Assert.Equal(Unparsed, node.documentState)
    Assert.Equal(PersistState.Unpersisted, node.persistState)
    Assert.Empty(snaps)

[<Fact>]
let ``SnapshotDone sets Persisted and calls the snapshot handler`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    let shot = Some initial.graph
    apply (handlers state snaps) (InMsg.SnapshotDone(id, shot))
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(PersistState.Persisted, node.persistState)
    Assert.Equal(ParseState.Parsed, node.parseState)
    Assert.Equal(Current, node.documentState)
    let snapId, snapGraph = Assert.Single(snaps)
    Assert.Equal(id, snapId)
    Assert.Equal(shot, snapGraph)

[<Fact>]
let ``SnapshotDone without a graph sets Persisted`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    apply (handlers state snaps) (InMsg.SnapshotDone(id, None))
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(PersistState.Persisted, node.persistState)
    Assert.Equal(ParseState.Parsed, node.parseState)
    let snapId, snapGraph = Assert.Single(snaps)
    Assert.Equal(id, snapId)
    Assert.Equal(None, snapGraph)

[<Fact>]
let ``axis write failure is returned`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Unparsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    let missing = NodeId.New()
    let missingResult =
        CoreMailboxLoad.applyInMsg
            (handlers state snaps)
            (InMsg.ParseFinished missing)
    Assert.Equal(Error "node not found", missingResult)
    Assert.Equal(ParseState.Unparsed, state.Value.graph.nodes.[id].parseState)
    let rejected =
        CoreMailboxLoad.applyInMsg
            (handlers state snaps)
            (InMsg.SnapshotDone(Graph.workspacesId, None))
    Assert.Equal(
        Error "workspaces is not a graph document",
        rejected)
    Assert.Equal(
        PersistState.Unpersisted,
        state.Value.graph.nodes.[id].persistState)

[<Fact>]
let ``mailbox queue applies ParseFinished`` () =
    task {
        let id, initial =
            fileState PersistState.Unpersisted ParseState.Unparsed
        let state = ref initial
        let snaps = ResizeArray<NodeId * Graph option>()
        let host = hostFor state snaps
        try
            CoreMailbox.addInMsg host (InMsg.ParseFinished id)
            let! got =
                CoreMailbox.getState host |> Async.StartAsTask
            let got = requireState "state" got
            let node = got.graph.nodes.[id]
            Assert.Equal(ParseState.Parsed, node.parseState)
            Assert.Equal(PersistState.Unpersisted, node.persistState)
            Assert.Empty(snaps)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``mailbox queue applies SnapshotDone off CoreMsg`` () =
    task {
        let id, initial =
            fileState PersistState.Unpersisted ParseState.Parsed
        let state = ref initial
        let snaps = ResizeArray<NodeId * Graph option>()
        let host = hostFor state snaps
        try
            let shot = Some initial.graph
            CoreMailbox.addInMsg host (InMsg.SnapshotDone(id, shot))
            let! got =
                CoreMailbox.getState host |> Async.StartAsTask
            let got = requireState "state" got
            let node = got.graph.nodes.[id]
            Assert.Equal(PersistState.Persisted, node.persistState)
            Assert.Equal(ParseState.Parsed, node.parseState)
            Assert.Equal(1, snaps.Count)
        finally
            CoreMailbox.dispose host
    }
