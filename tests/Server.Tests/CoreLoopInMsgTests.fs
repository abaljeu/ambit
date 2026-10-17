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
    let snaps = ResizeArray<NodeId * Graph option>()
    apply (handlers state snaps) (InMsg.ParseFinished id)
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
    let snaps = ResizeArray<NodeId * Graph option>()
    apply (handlers state snaps) (InMsg.MarkUnparsed id)
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(ParseState.Unparsed, node.documentState.parseState)
    Assert.Equal(Unparsed, node.documentState.AsEnum)
    Assert.Equal(PersistState.Unpersisted, node.documentState.persistState)
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
    apply (handlers state snaps) (InMsg.SnapshotDone(id, None))
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
    let missing = NodeId.New()
    let missingResult =
        CoreMailboxLoad.applyInMsg
            (handlers state snaps)
            (InMsg.ParseFinished missing)
    Assert.Equal(Error "node not found", missingResult)
    Assert.Equal(ParseState.Unparsed, state.Value.graph.nodes.[id].documentState.parseState)
    let rejected =
        CoreMailboxLoad.applyInMsg
            (handlers state snaps)
            (InMsg.SnapshotDone(Graph.workspacesId, None))
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
        let snaps = ResizeArray<NodeId * Graph option>()
        let host = hostFor state snaps
        try
            CoreMailboxApi.addInMsg host (InMsg.ParseFinished id)
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

let private loggingHandlers
    (state: State ref)
    (snaps: ResizeArray<NodeId * Graph option>)
    (log: EventLog ref)
    : PersistHandlers =
    let baseHandlers = handlers state snaps
    { baseHandlers with
        getEventLog = fun () -> Ok log.Value
        getEventsSince =
            fun after -> Ok((EventLog.since after log.Value).events)
        appendEvent =
            fun event ->
                log.Value <- EventLog.restore [ event ] log.Value
                state.Value <- { state.Value with eventId = event.id }
                Ok () }

let private loggingHost state snaps log =
    CoreMailbox.host
        (CoreActorPool.create ())
        { filling state snaps with
            handlers = loggingHandlers state snaps log }
        admittedCredentials

let private oneTransition (log: EventLog) =
    match log.events with
    | [ event ] ->
        match event.body with
        | EventBody.Change [ Op.SetDocumentState(id, old, next) ] ->
            id, old, next
        | _ ->
            Assert.Fail("expected one SetDocumentState")
            Unchecked.defaultof<_>
    | _ ->
        Assert.Fail($"expected one Event, got {log.events.Length}")
        Unchecked.defaultof<_>

[<Fact>]
let ``ParseFinished and MarkUnparsed log SetDocumentState`` () =
    task {
        let id, initial =
            fileState PersistState.Unpersisted ParseState.Unparsed
        let state = ref initial
        let snaps = ResizeArray<NodeId * Graph option>()
        let log = ref EventLog.empty
        let host = loggingHost state snaps log
        try
            CoreMailboxApi.addInMsg host (InMsg.ParseFinished id)
            let! got =
                CoreMailboxApi.getState host |> Async.StartAsTask
            let got = requireState "parsed" got
            let! events =
                CoreMailboxApi.getEventsSince host EventId.zero
                |> Async.StartAsTask
            let loggedId, old, next = oneTransition log.Value
            Assert.Equal(id, loggedId)
            Assert.Equal(Unparsed, old)
            Assert.Equal(Current, next)
            Assert.Equal(1, events.Length)
            let parsed = got.graph.nodes.[id]
            Assert.Equal(ParseState.Parsed, parsed.documentState.parseState)
            Assert.Equal(Current, parsed.documentState.AsEnum)
            Assert.Equal(
                PersistState.Unpersisted,
                parsed.documentState.persistState)
            CoreMailboxApi.addInMsg host (InMsg.ParseFinished id)
            let! _ =
                CoreMailboxApi.getState host |> Async.StartAsTask
            Assert.Equal(1, log.Value.events.Length)
            CoreMailboxApi.addInMsg host (InMsg.MarkUnparsed id)
            let! marked =
                CoreMailboxApi.getState host |> Async.StartAsTask
            let marked = requireState "unparsed" marked
            Assert.Equal(2, log.Value.events.Length)
            match log.Value.events with
            | mark :: _ ->
                match mark.body with
                | EventBody.Change [
                    Op.SetDocumentState(markId, markOld, markNext)
                  ] ->
                    Assert.Equal(id, markId)
                    Assert.Equal(Current, markOld)
                    Assert.Equal(Unparsed, markNext)
                | _ -> Assert.Fail("expected SetDocumentState")
            | [] -> Assert.Fail("missing Event")
            let node = marked.graph.nodes.[id]
            Assert.Equal(ParseState.Unparsed, node.documentState.parseState)
            Assert.Equal(Unparsed, node.documentState.AsEnum)
            Assert.Equal(
                PersistState.Unpersisted,
                node.documentState.persistState)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``SetDocumentState InMsg sets NoServerFile and logs the Event`` () =
    let id, initial =
        fileState PersistState.Persisted ParseState.Parsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    let log = ref EventLog.empty
    apply
        (loggingHandlers state snaps log)
        (InMsg.SetDocumentState(id, NoServerFile))
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(NoServerFile, node.documentState.AsEnum)
    Assert.True(node.documentState.noServerFile)
    Assert.Equal(ParseState.Unparsed, node.documentState.parseState)
    Assert.Equal(PersistState.Persisted, node.documentState.persistState)
    let loggedId, old, next = oneTransition log.Value
    Assert.Equal(id, loggedId)
    Assert.Equal(Current, old)
    Assert.Equal(NoServerFile, next)

[<Fact>]
let ``writer skips a second Event when the post stored the transition`` () =
    let id, initial =
        fileState PersistState.Unpersisted ParseState.Unparsed
    let state = ref initial
    let snaps = ResizeArray<NodeId * Graph option>()
    let stored =
        { id = EventId.fromJson 1
          submissionId = Guid.NewGuid()
          authority = Authority "Core"
          commandName = ""
          body =
            EventBody.Change [
                Op.SetDocumentState(id, Unparsed, Current)
            ] }
    let log = ref (EventLog.restore [ stored ] EventLog.empty)
    apply
        (loggingHandlers state snaps log)
        (InMsg.ParseFinished id)
    let node = state.Value.graph.nodes.[id]
    Assert.Equal(ParseState.Parsed, node.documentState.parseState)
    Assert.Equal(Current, node.documentState.AsEnum)
    Assert.Equal(1, log.Value.events.Length)
    Assert.Equal(stored.submissionId, log.Value.events.Head.submissionId)

[<Fact>]
let ``public post of SetDocumentState is accepted`` () =
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
            let! posted =
                CoreMailboxApi.postEvents
                    host
                    testCaller
                    [ wireEvent
                        (Guid.NewGuid())
                        [ Op.SetDocumentState(id, Unparsed, NoServerFile) ] ]
                |> Async.StartAsTask
            let posted = requireState "postEvents" posted
            Assert.True(EventId.isAccepted posted.eventId)
            let! graphOnly =
                CoreMailboxApi.postGraphOnly
                    host
                    testCaller
                    (wireEvent
                        (Guid.NewGuid())
                        [ Op.SetDocumentState(id, NoServerFile, Current) ])
                |> Async.StartAsTask
            let graphOnly = requireState "postGraphOnly" graphOnly
            Assert.True(EventId.isAccepted graphOnly.eventId)
            let! got =
                CoreMailboxApi.getState host |> Async.StartAsTask
            let got = requireState "state" got
            let node = got.graph.nodes.[id]
            Assert.Equal(ParseState.Parsed, node.documentState.parseState)
            Assert.Equal(Current, node.documentState.AsEnum)
            Assert.False(node.documentState.noServerFile)
            let! history =
                CoreMailboxApi.eventHistory host |> Async.StartAsTask
            Assert.Equal(3, history.events.Length)
        finally
            CoreMailboxApi.dispose host
    }
