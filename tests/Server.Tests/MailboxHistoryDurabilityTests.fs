module Gambol.Server.Tests.MailboxHistoryDurabilityTests

open System
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error error ->
        Assert.Fail($"{label}: {error}")
        Unchecked.defaultof<_>

let private sampleActorStart: ActorStart =
    { zoomId = Graph.rootId
      focusId = Graph.rootId
      commandId = Graph.rootId
      graphIds = [ Graph.rootId ]
      eventId = EventId.zero }

let private actorCaller secret =
    { authority = Authority "Actor"
      name = ""
      secret = secret }

let private stubPool secret : CoreActorPool =
    { register = fun _ _ -> ()
      startActor = fun _ _ -> Ok secret
      schedule = fun _ _ -> ()
      isLive = fun s -> s = secret
      admit = fun _ -> Ok ()
      drop = fun _ -> ()
      finish = fun _ _ -> Ok ()
      liveFocusIds = fun () -> Set.empty
      getFocusId = fun _ -> Some Graph.rootId }

let private hostFile dir secret =
    CoreMailbox.host
        (stubPool secret)
        (FileAgent.persist (FileAgent.create dir))
        admittedCredentials

let private hostDb connStr secret =
    CoreMailbox.host
        (stubPool secret)
        (DbAgent.persist (DbAgent.create connStr))
        admittedCredentials

let private runMixedHello host secret childText = task {
    let! started =
        CoreMailbox.startActor host testCaller sampleActorStart
        |> Async.StartAsTask
    requireOk "startActor" started
    let childId, event = addRootChildEvent childText
    let! accepted =
        CoreMailbox.postEvents host testCaller [ event ]
        |> Async.StartAsTask
    requireOk "postEvents" accepted |> ignore
    let! stopped =
        CoreMailbox.actorStop
            host
            (actorCaller secret)
            ActorSucceeded
        |> Async.StartAsTask
    requireOk "actorStop" stopped
    return childId
}

let private mixedIds (history: EventLog) =
    match history.events with
    | stop :: change :: start :: [] ->
        match stop.body, change.body, start.body with
        | EventBody.ActorStop _, EventBody.Change _, EventBody.ActorStart _ ->
            stop.id, change.id, start.id
        | _ ->
            Assert.Fail("expected ActorStop, Change, ActorStart newest-head")
            EventId.zero, EventId.zero, EventId.zero
    | _ ->
        Assert.Fail("expected three EventLog events")
        EventId.zero, EventId.zero, EventId.zero

let private assertChild state childId text =
    Assert.Equal(text, state.graph.nodes.[childId].text)

let private assertTipEquals host (history: EventLog) = task {
    let stopId, _, _ = mixedIds history
    let! eventId = CoreMailbox.getEventId host |> Async.StartAsTask
    Assert.Equal(stopId, eventId)
    Assert.Equal(stopId, history.events.Head.id)
}

let private bodyKind (event: Ev) =
    match event.body with
    | EventBody.ActorStart _ -> "start"
    | EventBody.Change _ -> "change"
    | EventBody.ActorStop _ -> "stop"
    | EventBody.Undo _ -> "undo"
    | EventBody.Redo _ -> "redo"

let private assertBodiesMatch (history: EventLog) (since: Ev list) =
    Assert.Equal<string list>(
        List.map bodyKind history.events,
        List.map bodyKind since)

let private lifecycle id body : Ev =
    { id = EventId.fromJson id
      submissionId = Guid.NewGuid()
      authority = testAuthority
      commandName = ""
      body = body }

let private appendOnly persist (events: Ev list) =
    events
    |> List.iter (fun event ->
        persist.appendEvent event |> requireOk "appendEvent" |> ignore)

let private nameOnlyUndo target : Ev =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Wire"
      commandName = "test"
      body = EventBody.Undo(target, []) }

let private assertRestartLog host = task {
    let! history = CoreMailbox.eventHistory host |> Async.StartAsTask
    let! since =
        CoreMailbox.getEventsSince host EventId.zero
        |> Async.StartAsTask
    assertBodiesMatch history since
    do! assertTipEquals host history
}

let private assertRestartGraph host childId text = task {
    do! assertRestartLog host
    let! state = CoreMailbox.getState host |> Async.StartAsTask
    assertChild (requireOk "getState" state) childId text
}

[<Fact>]
let ``File mixed hello survives dispose create`` () = task {
    let dir = newTempDir ()
    let secret = Credential "mailbox-history-file-hello"
    let first = hostFile dir secret
    let! _ =
        task {
            try
                return! runMixedHello first secret "file-hello"
            finally
                CoreMailbox.dispose first
        }
    let second = CoreMailbox.createFile dir admittedCredentials
    try
        do! assertRestartLog second
    finally
        CoreMailbox.dispose second
}

[<Fact>]
let ``Db mixed hello survives dispose create`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let secret = Credential "mailbox-history-db-hello"
    let first = hostDb connStr secret
    let! childId =
        task {
            try
                return! runMixedHello first secret "db-hello"
            finally
                CoreMailbox.dispose first
        }
    let second = hostDb connStr (Credential "mailbox-history-db-hello-2")
    try
        do! assertRestartGraph second childId "db-hello"
    finally
        CoreMailbox.dispose second
}

[<Fact>]
let ``File recover applies Ops Ev ahead of Graph checkpoint`` () = task {
    let dir = newTempDir ()
    let childId, change = addRootChildEvent "file-recover"
    let agent = FileAgent.create dir
    let persist = FileAgent.persist agent
    appendOnly
        persist.handlers
        [ lifecycle 1 (EventBody.ActorStart sampleActorStart)
          { change with id = EventId.fromJson 2 }
          lifecycle
            3
            (EventBody.ActorStop(Graph.rootId, ActorSucceeded)) ]
    let before = persist.handlers.getState () |> requireOk "before"
    Assert.False(before.graph.nodes.ContainsKey childId)
    persist.dispose ()
    let host = CoreMailbox.createFile dir admittedCredentials
    try
        let! state = CoreMailbox.getState host |> Async.StartAsTask
        let! eventId = CoreMailbox.getEventId host |> Async.StartAsTask
        assertChild (requireOk "getState" state) childId "file-recover"
        Assert.Equal(EventId.fromJson 3, eventId)
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``Db recover applies Ops Ev ahead of projection revision`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let childId, change = addRootChildEvent "db-recover"
    let agent = DbAgent.create connStr
    let persist = DbAgent.persist agent
    appendOnly
        persist.handlers
        [ lifecycle 1 (EventBody.ActorStart sampleActorStart)
          { change with id = EventId.fromJson 2 }
          lifecycle
            3
            (EventBody.ActorStop(Graph.rootId, ActorSucceeded)) ]
    let before = persist.handlers.getState () |> requireOk "before"
    Assert.False(before.graph.nodes.ContainsKey childId)
    persist.dispose ()
    let host = admittedHostDb (DbAgent.create connStr)
    try
        let! state = CoreMailbox.getState host |> Async.StartAsTask
        let! eventId = CoreMailbox.getEventId host |> Async.StartAsTask
        assertChild (requireOk "getState" state) childId "db-recover"
        Assert.Equal(EventId.fromJson 3, eventId)
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``Undo after File restart fills Change and rejects ActorStart`` () = task {
    let dir = newTempDir ()
    let secret = Credential "mailbox-history-undo"
    let first = hostFile dir secret
    try
        let! _ = runMixedHello first secret "undo-hello"
        ()
    finally
        CoreMailbox.dispose first
    let second = CoreMailbox.createFile dir admittedCredentials
    try
        let! history = CoreMailbox.eventHistory second |> Async.StartAsTask
        let stopId, changeId, startId = mixedIds history
        Assert.NotEqual(stopId, changeId)
        let! undone =
            CoreMailbox.postEvent
                second
                testCaller
                (nameOnlyUndo changeId)
            |> Async.StartAsTask
        let stored = requireOk "undo Change" undone
        Assert.NotEmpty(Ev.ops stored |> Option.defaultValue [])
        let! rejected =
            CoreMailbox.postEvent
                second
                testCaller
                (nameOnlyUndo startId)
            |> Async.StartAsTask
        match rejected with
        | Ok _ -> Assert.Fail("expected ActorStart Undo error")
        | Error error ->
            Assert.Contains("no inverse Ops", error)
    finally
        CoreMailbox.dispose second
}

[<Fact>]
let ``File empty EventLog recover exposes getEventId Graph checkpoint`` () = task {
    let dir = newTempDir ()
    Bookkeeping.writeEventId dir (EventId.fromJson 4) |> requireOk "writeEventId"
    let host = CoreMailbox.createFile dir admittedCredentials
    try
        let! eventId = CoreMailbox.getEventId host |> Async.StartAsTask
        Assert.Equal(EventId.fromJson 4, eventId)
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``File Graph greater than Log drops persist EventLog`` () =
    let dir = newTempDir ()
    let persist = FileAgent.persist (FileAgent.create dir)
    appendOnly
        persist.handlers
        [ lifecycle 1 (EventBody.ActorStart sampleActorStart)
          lifecycle 2 (EventBody.ActorStop(Graph.rootId, ActorSucceeded)) ]
    Bookkeeping.writeEventId dir (EventId.fromJson 5) |> requireOk "writeEventId"
    persist.dispose ()
    let host = CoreMailbox.createFile dir admittedCredentials
    try
        let history = CoreMailbox.eventHistory host |> Async.RunSynchronously
        let eventId = CoreMailbox.getEventId host |> Async.RunSynchronously
        Assert.Empty(history.events)
        Assert.Equal(EventId.fromJson 5, eventId)
    finally
        CoreMailbox.dispose host

[<Fact>]
let ``File equal Log and Graph keeps EventLog`` () =
    let dir = newTempDir ()
    let persist = FileAgent.persist (FileAgent.create dir)
    appendOnly
        persist.handlers
        [ lifecycle 1 (EventBody.ActorStart sampleActorStart)
          lifecycle 2 (EventBody.ActorStop(Graph.rootId, ActorSucceeded)) ]
    Bookkeeping.writeEventId dir (EventId.fromJson 2) |> requireOk "writeEventId"
    persist.dispose ()
    let host = CoreMailbox.createFile dir admittedCredentials
    try
        let history = CoreMailbox.eventHistory host |> Async.RunSynchronously
        let eventId = CoreMailbox.getEventId host |> Async.RunSynchronously
        Assert.Equal(2, history.events.Length)
        Assert.Equal(EventId.fromJson 2, eventId)
    finally
        CoreMailbox.dispose host

[<Fact>]
let ``Db Graph greater than Log drops persist EventLog`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let persist = DbAgent.persist (DbAgent.create connStr)
    appendOnly
        persist.handlers
        [ lifecycle 1 (EventBody.ActorStart sampleActorStart)
          lifecycle 2 (EventBody.ActorStop(Graph.rootId, ActorSucceeded)) ]
    persist.dispose ()
    do!
        Database.rebuildFromDocumentFiles
            connStr
            { graph = Graph.create (); eventId = EventId.fromJson 5 }
    let host = admittedHostDb (DbAgent.create connStr)
    try
        let history = CoreMailbox.eventHistory host |> Async.RunSynchronously
        let eventId = CoreMailbox.getEventId host |> Async.RunSynchronously
        Assert.Empty(history.events)
        Assert.Equal(EventId.fromJson 5, eventId)
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``File empty Graph checkpoint live append continues past Graph`` () = task {
    let dir = newTempDir ()
    Bookkeeping.writeEventId dir (EventId.fromJson 5) |> requireOk "writeEventId"
    let host = hostFile dir (Credential "mailbox-history-continue")
    try
        let! started =
            CoreMailbox.startActor host testCaller sampleActorStart
            |> Async.StartAsTask
        requireOk "startActor" started
        let! history = CoreMailbox.eventHistory host |> Async.StartAsTask
        let! eventId = CoreMailbox.getEventId host |> Async.StartAsTask
        Assert.Equal(EventId.fromJson 6, history.events.Head.id)
        Assert.Equal(EventId.fromJson 6, eventId)
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``getEventId is ActorStop after in-process hello`` () = task {
    let dir = newTempDir ()
    let secret = Credential "mailbox-history-live-serial"
    let host = hostFile dir secret
    try
        let! _ = runMixedHello host secret "live-serial"
        let! history = CoreMailbox.eventHistory host |> Async.StartAsTask
        do! assertTipEquals host history
    finally
        CoreMailbox.dispose host
}
