module Gambol.Server.Tests.CoreLoopNoticeTests

open System
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
    (snaps: ResizeArray<NodeId>)
    : PersistHandlers =
    { getState = fun () -> Ok state.Value
      getEventId = fun () -> Ok state.Value.eventId
      getEventsSince = fun _ -> Ok []
      getEventLog = fun () -> Ok EventLog.empty
      appendEvent = fun _ -> Ok ()
      applyEvent = fun _ _ -> Error "unused"
      replaceGraph =
        fun _ graph ->
            state.Value <- { state.Value with graph = graph }
      noteDiffDone = snaps.Add
      noteParsed = ignore
      noteStamped = fun _ _ -> ()
      noteWriteFailed = ignore }

let private filling state snaps : PersistFilling =
    { handlers = handlers state snaps
      onError = fun _ _ _ -> ()
      formatError = id
      isReady = fun () -> true
      dispose = fun () -> ()
      until = None
      bindPersistFinished = ignore }

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
    match CoreMailboxLoad.applyNotice persist msg with
    | Ok () -> ()
    | Error err -> Assert.Fail(err)

[<Fact>]
let ``ParseFinished sets Parsed and leaves PersistState`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Unparsed
    let state = ref initial
    let snaps = ResizeArray<NodeId>()
    apply (handlers state snaps) (Notice.NodeParsed id)
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(ParseState.Parsed, node.documentState.parseState)
    Assert.Equal(Current, node.documentState.AsEnum)
    Assert.False(node.documentState.pullingFile)
    Assert.Equal(PersistState.Unpersisted, node.documentState.persistState)
    Assert.Empty(snaps)

[<Fact>]
let ``ParseFinished leaves a pullingFile File Node Unparsed`` () =
    let id = NodeId.New()
    let node =
        Node.Create(
            id,
            text = "note.txt",
            name = Filename.create "note.txt",
            kind = Special File,
            documentState = PullingFile,
            parseState = ParseState.Unparsed,
            persistState = PersistState.Unpersisted)
    let state =
        ref {
            graph = Graph.addDetachedNode node (Graph.create ())
            eventId = EventId.zero }
    let snaps = ResizeArray<NodeId>()
    apply (handlers state snaps) (Notice.NodeParsed id)
    let after = state.Value.graph.nodes.[id]
    Assert.Equal(ParseState.Unparsed, after.documentState.parseState)
    Assert.True(after.documentState.pullingFile)
    Assert.Equal(PullingFile, after.documentState.AsEnum)
    Assert.Equal(PersistState.Unpersisted, after.documentState.persistState)
    Assert.Empty(snaps)

[<Fact>]
let ``MarkUnparsed sets Unparsed and leaves PersistState`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId>()
    apply (handlers state snaps) (Notice.MarkUnparsed id)
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(ParseState.Unparsed, node.documentState.parseState)
    Assert.Equal(Unparsed, node.documentState.AsEnum)
    Assert.Equal(PersistState.Unpersisted, node.documentState.persistState)
    Assert.Empty(snaps)

[<Fact>]
let ``NodePersisted sets Persisted and calls the diff hook`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId>()
    let stamp = DateTime(2026, 10, 21, 1, 2, 3, DateTimeKind.Utc)
    apply (handlers state snaps) (Notice.NodePersisted(id, stamp))
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(PersistState.Persisted, node.documentState.persistState)
    Assert.Equal(stamp, node.updateTime)
    Assert.Equal(ParseState.Parsed, node.documentState.parseState)
    Assert.Equal(Current, node.documentState.AsEnum)
    Assert.Equal(id, Assert.Single(snaps))

[<Fact>]
let ``NodeUploading applies PullingFile`` () =
    let id, initial =
        fileState PersistState.Persisted ParseState.Parsed
    let beforeCount = initial.graph.nodes.Count
    let state = ref initial
    let snaps = ResizeArray<NodeId>()
    apply (handlers state snaps) (Notice.NodeUploading id)
    let node = state.Value.graph.nodes.[id]
    let row = GraphProjection.nodeRowFromNode node
    Assert.Equal(beforeCount, state.Value.graph.nodes.Count)
    Assert.True(node.documentState.pullingFile)
    Assert.Equal(PullingFile, node.documentState.AsEnum)
    Assert.Equal(PersistState.Unpersisted, node.documentState.persistState)
    Assert.Equal("unparsed", row.documentState)
    Assert.Equal("unpersisted", row.persistState)
    Assert.Empty(snaps)

[<Fact>]
let ``NodeReceived applies Unparsed`` () =
    let id = NodeId.New()
    let node =
        Node.Create(
            id,
            text = "note.txt",
            name = Filename.create "note.txt",
            kind = Special File,
            documentState = PullingFile,
            parseState = ParseState.Unparsed,
            persistState = PersistState.Unpersisted)
    let state =
        ref {
            graph = Graph.addDetachedNode node (Graph.create ())
            eventId = EventId.zero }
    let snaps = ResizeArray<NodeId>()
    apply (handlers state snaps) (Notice.NodeReceived id)
    let after = state.Value.graph.nodes.[id]
    let row = GraphProjection.nodeRowFromNode after
    Assert.Equal(ParseState.Unparsed, after.documentState.parseState)
    Assert.False(after.documentState.pullingFile)
    Assert.Equal(Unparsed, after.documentState.AsEnum)
    Assert.Equal(PersistState.Persisted, after.documentState.persistState)
    Assert.Equal("unparsed", row.documentState)
    Assert.Equal("persisted", row.persistState)
    Assert.Empty(snaps)

[<Fact>]
let ``NodePersisting matches the Unpersisted Set`` () =
    let id, initial =
        fileState PersistState.Persisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId>()
    apply (handlers state snaps) (Notice.NodePersisting id)
    let viaNotice = state.Value.graph.nodes.[id]
    let viaSet =
        Graph.setPersistState id PersistState.Unpersisted initial.graph
        |> requireState "set"
        |> fun graph -> graph.nodes.[id]
    Assert.Equal(viaSet.documentState, viaNotice.documentState)
    Assert.Equal(ParseState.Parsed, viaNotice.documentState.parseState)
    Assert.Equal(PersistState.Unpersisted, viaNotice.documentState.persistState)
    Assert.False(viaNotice.documentState.pullingFile)
    Assert.Empty(snaps)

[<Fact>]
let ``NodeStamped applies stamp ops and leaves persist state`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId>()
    let before = state.Value.graph.nodes.[id].updateTime
    let afterTime = before.AddMinutes(3.0)
    let ops = [ Op.SetUpdateTime(id, before, afterTime) ]
    apply (handlers state snaps) (Notice.NodeStamped(ops, Some "saved"))
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(afterTime, node.updateTime)
    Assert.Equal(PersistState.Unpersisted, node.documentState.persistState)
    Assert.Equal(ParseState.Parsed, node.documentState.parseState)
    Assert.Empty(snaps)

[<Fact>]
let ``Posted NodeStamped is on the log`` () =
    task {
        let id, initial =
            fileState PersistState.Unpersisted ParseState.Parsed
        let state = ref initial
        let snaps = ResizeArray<NodeId>()
        let before = state.Value.graph.nodes.[id].updateTime
        let afterTime = before.AddMinutes(4.0)
        let notice =
            Notice.NodeStamped(
                [ Op.SetUpdateTime(id, before, afterTime) ],
                None)
        let host = hostFor state snaps
        try
            CoreMailboxApi.postNotice host notice
            let! got =
                CoreMailboxApi.getState host |> Async.StartAsTask
            let got = requireState "state" got
            Assert.Equal(afterTime, got.graph.nodes.[id].updateTime)
            Assert.Equal(
                PersistState.Unpersisted,
                got.graph.nodes.[id].documentState.persistState)
            let! log =
                CoreMailboxApi.eventsSince host EventId.zero
                |> Async.StartAsTask
            let stored = Assert.Single(log.events)
            Assert.Equal(EventBody.Notice notice, stored.body)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``PersistFailed does not change persist state`` () =
    let id, initial =
        fileState PersistState.Persisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId>()
    apply
        (handlers state snaps)
        (Notice.PersistFailed(id, "disk full"))
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(PersistState.Persisted, node.documentState.persistState)
    Assert.Equal(ParseState.Parsed, node.documentState.parseState)
    Assert.Empty(snaps)

[<Fact>]
let ``NodeParsing changes nothing`` () =
    task {
        let id, initial =
            fileState PersistState.Persisted ParseState.Parsed
        let state = ref initial
        let snaps = ResizeArray<NodeId>()
        let pushes = ResizeArray<NodeId>()
        let host =
            CoreMailbox.hostWithParsePush
                (CoreActorPool.create ())
                (filling state snaps)
                admittedCredentials
                (fun nodeId -> pushes.Add nodeId)
        try
            CoreMailboxApi.postNotice host (Notice.NodeParsing id)
            let! got =
                CoreMailboxApi.getState host |> Async.StartAsTask
            let got = requireState "state" got
            let node = got.graph.nodes.[id]
            Assert.Equal(ParseState.Parsed, node.documentState.parseState)
            Assert.Equal(Current, node.documentState.AsEnum)
            Assert.False(node.documentState.pullingFile)
            Assert.Equal(PersistState.Persisted, node.documentState.persistState)
            Assert.Empty(pushes)
            Assert.Empty(snaps)
            let! log =
                CoreMailboxApi.eventsSince host EventId.zero
                |> Async.StartAsTask
            let stored = Assert.Single(log.events)
            Assert.Equal(EventBody.Notice(Notice.NodeParsing id), stored.body)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``axis write failure is returned`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Unparsed
    let state = ref initial
    let snaps = ResizeArray<NodeId>()
    let missing = NodeId.New()
    let missingResult =
        CoreMailboxLoad.applyNotice
            (handlers state snaps)
            (Notice.NodeParsed missing)
    Assert.Equal(Error "node not found", missingResult)
    Assert.Equal(ParseState.Unparsed, state.Value.graph.nodes.[id].documentState.parseState)
    let rejected =
        CoreMailboxLoad.applyNotice
            (handlers state snaps)
            (Notice.NodePersisted(Graph.workspacesId, DateTime.UtcNow))
    Assert.Equal(
        Error "workspaces is not a graph document",
        rejected)
    Assert.Equal(
        PersistState.Unpersisted,
        state.Value.graph.nodes.[id].documentState.persistState)

[<Fact>]
let ``Posted Notice is on poll`` () =
    task {
        let id, initial =
            fileState PersistState.Persisted ParseState.Parsed
        let state = ref initial
        let snaps = ResizeArray<NodeId>()
        let host = hostFor state snaps
        try
            CoreMailboxApi.postNotice host (Notice.NodeParsing id)
            let! log =
                CoreMailboxApi.eventsSince host EventId.zero
                |> Async.StartAsTask
            let stored = Assert.Single(log.events)
            Assert.Equal(EventBody.Notice(Notice.NodeParsing id), stored.body)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``mailbox queue applies ParseFinished`` () =
    task {
        let id, initial =
            fileState PersistState.Unpersisted ParseState.Unparsed
        let state = ref initial
        let snaps = ResizeArray<NodeId>()
        let host = hostFor state snaps
        try
            CoreMailboxApi.postNotice host (Notice.NodeParsed id)
            let! got =
                CoreMailboxApi.getState host |> Async.StartAsTask
            let got = requireState "state" got
            let node = got.graph.nodes.[id]
            Assert.Equal(ParseState.Parsed, node.documentState.parseState)
            Assert.Equal(PersistState.Unpersisted, node.documentState.persistState)
            Assert.Empty(snaps)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``mailbox queue applies NodePersisted off CoreMsg`` () =
    task {
        let id, initial =
            fileState PersistState.Unpersisted ParseState.Parsed
        let state = ref initial
        let snaps = ResizeArray<NodeId>()
        let host = hostFor state snaps
        try
            CoreMailboxApi.postNotice host (Notice.NodePersisted(id, DateTime.UtcNow))
            let! got =
                CoreMailboxApi.getState host |> Async.StartAsTask
            let got = requireState "state" got
            let node = got.graph.nodes.[id]
            Assert.Equal(PersistState.Persisted, node.documentState.persistState)
            Assert.Equal(ParseState.Parsed, node.documentState.parseState)
            Assert.Equal(1, snaps.Count)
        finally
            CoreMailboxApi.dispose host
    }
