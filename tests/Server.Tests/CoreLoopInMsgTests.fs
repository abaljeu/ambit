module Gambol.Server.Tests.CoreLoopInMsgTests

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

let private writer
    (persist: PersistHandlers)
    (eventLog: EventLog ref)
    : CoreMailboxLoad.ParseWriter =
    { persist = persist
      eventLog = eventLog }

let private apply persist eventLog msg =
    match CoreMailboxLoad.applyInMsg (writer persist eventLog) msg with
    | Ok () -> ()
    | Error err -> Assert.Fail(err)

let private freshLog () = ref EventLog.empty

type private ParseLogs = {
    persist: EventLog ref
    mailbox: EventLog ref
}

let private freshLogs () = {
    persist = ref EventLog.empty
    mailbox = ref EventLog.empty
}

let private loggingHandlers
    (state: State ref)
    (snaps: ResizeArray<NodeId * Graph option>)
    (logs: ParseLogs)
    : PersistHandlers =
    let baseHandlers = handlers state snaps
    { baseHandlers with
        getEventLog = fun () -> Ok logs.persist.Value
        getEventsSince =
            fun after ->
                Ok((EventLog.since after logs.persist.Value).events)
        appendEvent =
            fun event ->
                logs.persist.Value <-
                    EventLog.restore [ event ] logs.persist.Value
                state.Value <- { state.Value with eventId = event.id }
                Ok () }

let private oneTransition (log: EventLog) =
    match log.events with
    | [ event ] ->
        match event.body with
        | EventBody.Change [ Op.SetDocumentState(id, old, next) ] ->
            id, old, next, event
        | _ ->
            Assert.Fail("expected one SetDocumentState")
            Unchecked.defaultof<_>
    | _ ->
        Assert.Fail($"expected one Event, got {log.events.Length}")
        Unchecked.defaultof<_>

let private assertSameTransition
    (logs: ParseLogs)
    (nodeId: NodeId)
    (prior: DocumentStateEnum)
    (next: DocumentStateEnum)
    =
    let persistId, persistOld, persistNext, persistEvent =
        oneTransition logs.persist.Value
    let mailId, mailOld, mailNext, mailEvent =
        oneTransition logs.mailbox.Value
    Assert.Equal(nodeId, persistId)
    Assert.Equal(prior, persistOld)
    Assert.Equal(next, persistNext)
    Assert.Equal(nodeId, mailId)
    Assert.Equal(prior, mailOld)
    Assert.Equal(next, mailNext)
    Assert.Equal(persistEvent.submissionId, mailEvent.submissionId)
    Assert.Equal(persistEvent.id, mailEvent.id)
    Assert.True(EventId.isAccepted persistEvent.id)

[<Fact>]
let ``ParseFinished sets Parsed and leaves PersistState`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Unparsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    let logs = freshLogs ()
    let noted = ref false
    let persist =
        { loggingHandlers state snaps logs with
            noteParsed = fun _ -> noted.Value <- true }
    apply persist logs.mailbox (InMsg.ParseFinished id)
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(ParseState.Parsed, node.documentState.parseState)
    Assert.Equal(Current, node.documentState.AsEnum)
    Assert.False(node.documentState.noServerFile)
    Assert.Equal(PersistState.Unpersisted, node.documentState.persistState)
    Assert.Empty(snaps)
    Assert.True(noted.Value)
    assertSameTransition logs id Unparsed Current

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
    let snaps = ResizeArray<NodeId * Graph option>()
    let logs = freshLogs ()
    apply
        (loggingHandlers state snaps logs)
        logs.mailbox
        (InMsg.ParseFinished id)
    let after = state.Value.graph.nodes.[id]
    Assert.Equal(ParseState.Unparsed, after.documentState.parseState)
    Assert.True(after.documentState.noServerFile)
    Assert.Equal(NoServerFile, after.documentState.AsEnum)
    Assert.Equal(PersistState.Unpersisted, after.documentState.persistState)
    Assert.Empty(snaps)
    Assert.Empty(logs.persist.Value.events)
    Assert.Empty(logs.mailbox.Value.events)

[<Fact>]
let ``MarkUnparsed sets Unparsed and leaves PersistState`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    let logs = freshLogs ()
    apply
        (loggingHandlers state snaps logs)
        logs.mailbox
        (InMsg.MarkUnparsed id)
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(ParseState.Unparsed, node.documentState.parseState)
    Assert.Equal(Unparsed, node.documentState.AsEnum)
    Assert.Equal(PersistState.Unpersisted, node.documentState.persistState)
    Assert.Empty(snaps)
    assertSameTransition logs id Current Unparsed

[<Fact>]
let ``SnapshotDone sets Persisted and calls the snapshot handler`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    let shot = Some initial.graph
    apply (handlers state snaps) (freshLog ()) (InMsg.SnapshotDone(id, shot))
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(PersistState.Persisted, node.documentState.persistState)
    Assert.Equal(ParseState.Parsed, node.documentState.parseState)
    Assert.Equal(Current, node.documentState.AsEnum)
    let snapId, snapGraph = Assert.Single(snaps)
    Assert.Equal(id, snapId)
    Assert.Equal(shot, snapGraph)

[<Fact>]
let ``SnapshotDone without a graph sets Persisted`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    apply (handlers state snaps) (freshLog ()) (InMsg.SnapshotDone(id, None))
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(PersistState.Persisted, node.documentState.persistState)
    Assert.Equal(ParseState.Parsed, node.documentState.parseState)
    let snapId, snapGraph = Assert.Single(snaps)
    Assert.Equal(id, snapId)
    Assert.Equal(None, snapGraph)

[<Fact>]
let ``axis write failure is returned`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Unparsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    let logs = freshLogs ()
    let missing = NodeId.New()
    let missingResult =
        CoreMailboxLoad.applyInMsg
            (writer (loggingHandlers state snaps logs) logs.mailbox)
            (InMsg.ParseFinished missing)
    Assert.Equal(Error "node not found", missingResult)
    Assert.Equal(
        ParseState.Unparsed,
        state.Value.graph.nodes.[id].documentState.parseState)
    Assert.Empty(logs.persist.Value.events)
    Assert.Empty(logs.mailbox.Value.events)
    let rejected =
        CoreMailboxLoad.applyInMsg
            (writer (handlers state snaps) (freshLog ()))
            (InMsg.SnapshotDone(Graph.workspacesId, None))
    Assert.Equal(
        Error "workspaces is not a graph document",
        rejected)
    Assert.Equal(
        PersistState.Unpersisted,
        state.Value.graph.nodes.[id].documentState.persistState)

let private loggingHost state snaps logs =
    CoreMailbox.host
        (CoreActorPool.create ())
        { filling state snaps with
            handlers = loggingHandlers state snaps logs }
        admittedCredentials

[<Fact>]
let ``mailbox queue applies ParseFinished`` () =
    task {
        let id, initial =
            fileState PersistState.Unpersisted ParseState.Unparsed
        let state = ref initial
        let snaps = ResizeArray<NodeId * Graph option>()
        let logs = freshLogs ()
        let host = loggingHost state snaps logs
        try
            CoreMailboxApi.addInMsg host (InMsg.ParseFinished id)
            let! got =
                CoreMailboxApi.getState host |> Async.StartAsTask
            let got = requireState "state" got
            let node = got.graph.nodes.[id]
            Assert.Equal(ParseState.Parsed, node.documentState.parseState)
            Assert.Equal(PersistState.Unpersisted, node.documentState.persistState)
            Assert.Empty(snaps)
            let! history =
                CoreMailboxApi.eventHistory host |> Async.StartAsTask
            let! since =
                CoreMailboxApi.getEventsSince host EventId.zero
                |> Async.StartAsTask
            let historyId, historyOld, historyNext, historyEvent =
                oneTransition history
            Assert.Equal(id, historyId)
            Assert.Equal(Unparsed, historyOld)
            Assert.Equal(Current, historyNext)
            match since with
            | [ stored ] ->
                Assert.Equal(historyEvent.submissionId, stored.submissionId)
                Assert.Equal(historyEvent.id, stored.id)
            | _ ->
                Assert.Fail($"expected one persisted Event, got {since.Length}")
        finally
            CoreMailboxApi.dispose host
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
            CoreMailboxApi.addInMsg host (InMsg.SnapshotDone(id, shot))
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

[<Fact>]
let ``SetDocumentState sets NoServerFile and logs both outputs`` () =
    let id, initial =
        fileState PersistState.Persisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    let logs = freshLogs ()
    apply
        (loggingHandlers state snaps logs)
        logs.mailbox
        (InMsg.SetDocumentState(id, NoServerFile))
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(NoServerFile, node.documentState.AsEnum)
    Assert.True(node.documentState.noServerFile)
    Assert.Equal(ParseState.Unparsed, node.documentState.parseState)
    Assert.Equal(PersistState.Persisted, node.documentState.persistState)
    assertSameTransition logs id Current NoServerFile

[<Fact>]
let ``append failure leaves the graph and both logs unchanged`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Unparsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    let logs = freshLogs ()
    let noted = ref false
    let persist = {
        loggingHandlers state snaps logs with
            appendEvent = fun _ -> Error "append failed"
            noteParsed = fun _ -> noted.Value <- true
    }
    let result =
        CoreMailboxLoad.applyInMsg
            (writer persist logs.mailbox)
            (InMsg.ParseFinished id)
    Assert.Equal(Error "append failed", result)
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(ParseState.Unparsed, node.documentState.parseState)
    Assert.Equal(Unparsed, node.documentState.AsEnum)
    Assert.Empty(logs.persist.Value.events)
    Assert.Empty(logs.mailbox.Value.events)
    Assert.False(noted.Value)

let private nameOnlyUndo target : Ev =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Wire"
      commandName = "test"
      body = EventBody.Undo(target, []) }

[<Fact>]
let ``ParseFinished event reaches history and undo`` () =
    task {
        let host =
            CoreMailbox.createFile (newTempDir ()) admittedCredentials
        try
            let id = NodeId.New()
            let! created =
                CoreMailboxApi.postGraphOnly
                    host
                    testCaller
                    (wireEvent
                        (Guid.NewGuid())
                        [ Op.NewSpecialNode(id, File, "note.txt") ])
                |> Async.StartAsTask
            let created = requireState "create" created
            Assert.True(EventId.isAccepted created.eventId)
            CoreMailboxApi.addInMsg host (InMsg.ParseFinished id)
            let! parsed =
                CoreMailboxApi.getState host |> Async.StartAsTask
            let parsed = requireState "parsed" parsed
            Assert.Equal(
                ParseState.Parsed,
                parsed.graph.nodes.[id].documentState.parseState)
            let! history =
                CoreMailboxApi.eventHistory host |> Async.StartAsTask
            let! since =
                CoreMailboxApi.getEventsSince host EventId.zero
                |> Async.StartAsTask
            let logged =
                history.events
                |> List.tryFind (fun event ->
                    match event.body with
                    | EventBody.Change [ Op.SetDocumentState(nodeId, old, next) ] ->
                        nodeId = id && old = Unparsed && next = Current
                    | _ -> false)
            match logged with
            | None -> Assert.Fail("missing SetDocumentState in mailbox history")
            | Some event ->
                let inPersist =
                    since
                    |> List.exists (fun stored ->
                        stored.submissionId = event.submissionId
                        && stored.id = event.id)
                Assert.True(inPersist, "persist log missing the history event")
                let! undone =
                    CoreMailboxApi.postEvent
                        host
                        testCaller
                        (nameOnlyUndo event.id)
                    |> Async.StartAsTask
                let undone = requireState "undo" undone
                match Ev.ops undone with
                | Some [ Op.SetDocumentState(nodeId, old, next) ] ->
                    Assert.Equal(id, nodeId)
                    Assert.Equal(Current, old)
                    Assert.Equal(Unparsed, next)
                | _ -> Assert.Fail("undo did not store the inverse")
                let! after =
                    CoreMailboxApi.getState host |> Async.StartAsTask
                let after = requireState "after undo" after
                Assert.Equal(
                    Unparsed,
                    after.graph.nodes.[id].documentState.AsEnum)
                Assert.Equal(
                    ParseState.Unparsed,
                    after.graph.nodes.[id].documentState.parseState)
        finally
            CoreMailboxApi.dispose host
    }
