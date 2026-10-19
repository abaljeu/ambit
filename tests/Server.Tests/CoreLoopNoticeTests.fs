module Gambol.Server.Tests.CoreLoopNoticeTests

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
        fun graph ->
            state.Value <- { state.Value with graph = graph }
      noteDiffDone = snaps.Add
      noteParsed = ignore }

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
    Assert.False(node.documentState.noServerFile)
    Assert.Equal(PersistState.Unpersisted, node.documentState.persistState)
    Assert.Empty(snaps)

[<Fact>]
let ``ParseFinished leaves a noServerFile File Node Unparsed`` () =
    let id = NodeId.New()
    let node =
        Node.Create(
            id,
            text = "note.txt",
            name = Filename.create "note.txt",
            kind = Special File,
            documentState = NoServerFile,
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
    Assert.True(after.documentState.noServerFile)
    Assert.Equal(NoServerFile, after.documentState.AsEnum)
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
let ``SnapshotDone sets Persisted and calls the diff hook`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId>()
    apply (handlers state snaps) (Notice.SnapshotDone id)
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(PersistState.Persisted, node.documentState.persistState)
    Assert.Equal(ParseState.Parsed, node.documentState.parseState)
    Assert.Equal(Current, node.documentState.AsEnum)
    Assert.Equal(id, Assert.Single(snaps))

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
            (Notice.SnapshotDone Graph.workspacesId)
    Assert.Equal(
        Error "workspaces is not a graph document",
        rejected)
    Assert.Equal(
        PersistState.Unpersisted,
        state.Value.graph.nodes.[id].documentState.persistState)

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
let ``mailbox queue applies SnapshotDone off CoreMsg`` () =
    task {
        let id, initial =
            fileState PersistState.Unpersisted ParseState.Parsed
        let state = ref initial
        let snaps = ResizeArray<NodeId>()
        let host = hostFor state snaps
        try
            CoreMailboxApi.postNotice host (Notice.SnapshotDone id)
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
