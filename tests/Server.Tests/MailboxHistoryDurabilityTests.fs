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
      runDefinitionId = Graph.rootId
      graphIds = [ Graph.rootId ]
      eventId = EventId.zero }

let private hostFile dir =
    CoreMailbox.host
        (CoreActorPool.create ())
        (FileAgent.persist (FileAgent.create dir))
        admittedCredentials

let private hostDb connStr =
    CoreMailbox.host
        (CoreActorPool.create ())
        (DbAgent.persist (DbAgent.create connStr))
        admittedCredentials

let private functionPlacement (request: ActorStart) =
    let placement : ActorOrderFromStart.FunctionActorPlacement =
        { request = request
          actor =
              TestActor.functionActorAt
                  request.runDefinitionId
                  request.focusId
          actorName = "test"
          actorNodeIdOf = ActorOrderFromStart.focusIdOf }
    placement

let private startFromFunction host request =
    CoreMailboxApi.coreMailboxStartActorFromFunction
        host
        testCaller
        (functionPlacement request)

let private eventIsStop (event: Ev) =
    match event.body with
    | EventBody.ActorStop _ -> true
    | _ -> false

/// functionActorAt stops itself after the start reply. Wait so dispose
/// cannot cut off that ActorStop.
let rec private waitForActorStop host remaining = task {
    let! history = CoreMailboxApi.eventHistory host |> Async.StartAsTask
    if List.exists eventIsStop history.events then
        ()
    elif remaining <= 0 then
        Assert.Fail("ActorStop did not land")
    else
        do! Async.Sleep 20 |> Async.StartAsTask
        do! waitForActorStop host (remaining - 1)
}

/// The change is posted first. functionActorAt's stop is queued during
/// start, so a later change would race that stop.
let private runMixedHello host childText = task {
    let childId, event = addRootChildEvent childText
    let! accepted =
        CoreMailboxApi.postEvents host testCaller [ event ]
        |> Async.StartAsTask
    requireOk "postEvents" accepted |> ignore
    let! started =
        startFromFunction host sampleActorStart
        |> Async.StartAsTask
    requireOk "startActor" started |> ignore
    do! waitForActorStop host 50
    return childId
}

let private mixedIds (history: EventLog) =
    let kept =
        history.events
        |> List.filter (fun event ->
            match event.body with
            | EventBody.ActorStop _ -> true
            | EventBody.Notice _ -> false
            | _ -> true)
    match kept with
    | stop :: start :: change :: [] ->
        match stop.body, start.body, change.body with
        | EventBody.ActorStop _,
          EventBody.Order (Order.StartActor _),
          EventBody.Change _ ->
            stop.id, start.id, change.id
        | _ ->
            Assert.Fail(
                "expected ActorStop, StartActor, Change newest-head")
            EventId.zero, EventId.zero, EventId.zero
    | _ ->
        Assert.Fail("expected three EventLog events")
        EventId.zero, EventId.zero, EventId.zero

let private assertChild state childId text =
    Assert.Equal(text, state.graph.nodes.[childId].text)

let private assertTipEquals host (history: EventLog) = task {
    let stopId, _, _ = mixedIds history
    let! eventId = CoreMailboxApi.getEventId host |> Async.StartAsTask
    Assert.True(EventId.value eventId >= EventId.value stopId)
    Assert.Contains(stopId, history.events |> List.map (fun event -> event.id))
}

let private bodyKind (event: Ev) =
    match event.body with
    | EventBody.ActorStart _ -> "start"
    | EventBody.Change _ -> "change"
    | EventBody.ActorStop _ -> "stop"
    | EventBody.Cancel _ -> "cancel"
    | EventBody.Load _ -> "load"
    | EventBody.Undo _ -> "undo"
    | EventBody.Redo _ -> "redo"
    | EventBody.Notice _ -> "notice"
    | EventBody.Order _ -> "order"

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
    let! history = CoreMailboxApi.eventHistory host |> Async.StartAsTask
    let! since =
        CoreMailboxApi.getEventsSince host EventId.zero
        |> Async.StartAsTask
    assertBodiesMatch history since
    do! assertTipEquals host history
}

let private assertRestartGraph host childId text = task {
    do! assertRestartLog host
    let! state = CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
    assertChild (requireOk "graphAtEvent" state) childId text
}

[<Fact>]
let ``File mixed hello survives dispose create`` () = task {
    let dir = newTempDir ()
    let first = hostFile dir
    let! _ =
        task {
            try
                return! runMixedHello first "file-hello"
            finally
                CoreMailboxApi.dispose first
        }
    let second = CoreMailbox.createFile dir admittedCredentials
    try
        do! assertRestartLog second
    finally
        CoreMailboxApi.dispose second
}

[<Fact>]
let ``Db mixed hello survives dispose create`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let first = hostDb connStr
    let! childId =
        task {
            try
                return! runMixedHello first "db-hello"
            finally
                CoreMailboxApi.dispose first
        }
    let second = hostDb connStr
    try
        do! assertRestartGraph second childId "db-hello"
    finally
        CoreMailboxApi.dispose second
}

[<Fact>]
let ``File startup does not replay the event log`` () = task {
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
    let before = persist.handlers.graphAtEvent () |> requireOk "before"
    Assert.False(before.graph.nodes.ContainsKey childId)
    persist.dispose ()
    let host = CoreMailbox.createFile dir admittedCredentials
    try
        let! state = CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
        let! eventId = CoreMailboxApi.getEventId host |> Async.StartAsTask
        let restarted = requireOk "graphAtEvent" state
        Assert.False(restarted.graph.nodes.ContainsKey childId)
        Assert.Equal(EventId.fromJson 3, eventId)
    finally
        CoreMailboxApi.dispose host
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
    let before = persist.handlers.graphAtEvent () |> requireOk "before"
    Assert.False(before.graph.nodes.ContainsKey childId)
    persist.dispose ()
    let host = admittedHostDb (DbAgent.create connStr)
    try
        let! state = CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
        let! eventId = CoreMailboxApi.getEventId host |> Async.StartAsTask
        assertChild (requireOk "graphAtEvent" state) childId "db-recover"
        Assert.Equal(EventId.fromJson 3, eventId)
    finally
        CoreMailboxApi.dispose host
}

let private redoWithOps target ops : Ev =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Wire"
      commandName = "test"
      body = EventBody.Redo(target, ops) }

let private assertBeforeRestart label result =
    match result with
    | Ok _ -> Assert.Fail($"{label} reached a target from before the restart")
    | Error error -> Assert.Contains("before the restart", error)

[<Fact>]
let ``Undo after restart refuses a target from before the restart`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let first = hostDb connStr
    try
        let! _ = runMixedHello first "undo-refuse"
        ()
    finally
        CoreMailboxApi.dispose first
    let second = hostDb connStr
    try
        let! history = CoreMailboxApi.eventHistory second |> Async.StartAsTask
        let _, _, changeId = mixedIds history
        let change =
            history.events
            |> List.find (fun event -> event.id = changeId)
        let ops = Ev.ops change |> Option.defaultValue []
        Assert.NotEmpty(ops)
        let! undone =
            CoreMailboxApi.postEvent
                second
                testCaller
                (nameOnlyUndo changeId)
            |> Async.StartAsTask
        assertBeforeRestart "name-only Undo" undone
        let! redone =
            CoreMailboxApi.postEvent
                second
                testCaller
                (redoWithOps changeId ops)
            |> Async.StartAsTask
        assertBeforeRestart "Redo with ops" redone
    finally
        CoreMailboxApi.dispose second
}

[<Fact>]
let ``Undo after restart accepts a target after the restart`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let first = hostDb connStr
    try
        let! _ = runMixedHello first "undo-after"
        ()
    finally
        CoreMailboxApi.dispose first
    let second = hostDb connStr
    try
        let _, event = addRootChildEvent "after-restart"
        let! posted =
            CoreMailboxApi.postEvent second testCaller event
            |> Async.StartAsTask
        let stored = requireOk "change after restart" posted
        let! undone =
            CoreMailboxApi.postEvent
                second
                testCaller
                (nameOnlyUndo stored.id)
            |> Async.StartAsTask
        let storedUndo = requireOk "undo after restart" undone
        Assert.NotEmpty(Ev.ops storedUndo |> Option.defaultValue [])
    finally
        CoreMailboxApi.dispose second
}

[<Fact>]
let ``File empty EventLog recover exposes getEventId Graph checkpoint`` () = task {
    let dir = newTempDir ()
    Bookkeeping.writeEventId dir (EventId.fromJson 4) |> requireOk "writeEventId"
    let host = CoreMailbox.createFile dir admittedCredentials
    try
        let! eventId = CoreMailboxApi.getEventId host |> Async.StartAsTask
        Assert.Equal(EventId.fromJson 4, eventId)
    finally
        CoreMailboxApi.dispose host
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
        let history = CoreMailboxApi.eventHistory host |> Async.RunSynchronously
        let eventId = CoreMailboxApi.getEventId host |> Async.RunSynchronously
        Assert.Empty(history.events)
        Assert.Equal(EventId.fromJson 5, eventId)
    finally
        CoreMailboxApi.dispose host

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
        let history = CoreMailboxApi.eventHistory host |> Async.RunSynchronously
        let eventId = CoreMailboxApi.getEventId host |> Async.RunSynchronously
        Assert.Equal(2, history.events.Length)
        Assert.Equal(EventId.fromJson 2, eventId)
    finally
        CoreMailboxApi.dispose host

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
        let history = CoreMailboxApi.eventHistory host |> Async.RunSynchronously
        let eventId = CoreMailboxApi.getEventId host |> Async.RunSynchronously
        Assert.Empty(history.events)
        Assert.Equal(EventId.fromJson 5, eventId)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``File empty Graph checkpoint live append continues past Graph`` () = task {
    let dir = newTempDir ()
    Bookkeeping.writeEventId dir (EventId.fromJson 5) |> requireOk "writeEventId"
    let host = hostFile dir
    try
        let! started =
            startFromFunction host sampleActorStart
            |> Async.StartAsTask
        requireOk "startActor" started |> ignore
        let! history = CoreMailboxApi.eventHistory host |> Async.StartAsTask
        let! eventId = CoreMailboxApi.getEventId host |> Async.StartAsTask
        let startId =
            history.events
            |> List.tryPick (fun event ->
                match event.body with
                | EventBody.Order (Order.StartActor _) -> Some event.id
                | _ -> None)
        match startId with
        | None -> Assert.Fail("expected Order.StartActor")
        | Some id ->
            Assert.Equal(EventId.fromJson 6, id)
            Assert.True(EventId.value eventId >= EventId.value id)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``getEventId is ActorStop after in-process hello`` () = task {
    let dir = newTempDir ()
    let host = hostFile dir
    try
        let! _ = runMixedHello host "live-serial"
        let! history = CoreMailboxApi.eventHistory host |> Async.StartAsTask
        do! assertTipEquals host history
    finally
        CoreMailboxApi.dispose host
}
