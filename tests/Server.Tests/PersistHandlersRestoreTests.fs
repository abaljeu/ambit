module Gambol.Server.Tests.PersistHandlersRestoreTests

open System
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error error ->
        Assert.Fail($"{label}: {error}")
        Unchecked.defaultof<_>

let private addRootChild text = addRootChildEvent text

let private sampleActorStart: ActorStart =
    { zoomId = Graph.rootId
      focusId = Graph.rootId
      runDefinitionId = Graph.rootId
      graphIds = [ Graph.rootId ]
      eventId = EventId.zero }

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

let private bodyIsStartActor (event: Ev) =
    match event.body with
    | EventBody.Order (Order.StartActor _) -> true
    | _ -> false

[<Fact>]
let ``getEventsSince returns Ev after postChange`` () = task {
    let dir = newTempDir ()
    let host =
        CoreMailbox.createFile dir admittedCredentials
    try
        let _, event = addRootChild "persist-event"
        let! accepted =
            CoreMailboxApi.postEvents host testCaller [ event ]
            |> Async.StartAsTask
        requireOk "postChange" accepted |> ignore
        let! events =
            CoreMailboxApi.getEventsSince
                host
                (EventId.zero)
            |> Async.StartAsTask
        let stored = Assert.Single(changesOnly events)
        Assert.Equal(event.submissionId, stored.submissionId)
        match stored.body with
        | EventBody.Change _ -> ()
        | _ -> Assert.Fail("expected Change EventBody")
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``EventLog.restore seeds mailbox across File restart`` () = task {
    let dir = newTempDir ()
    let _, event = addRootChild "restore-seed"
    let first = CoreMailbox.createFile dir admittedCredentials
    try
        let! accepted =
            CoreMailboxApi.postEvents first testCaller [ event ]
            |> Async.StartAsTask
        requireOk "postChange" accepted |> ignore
    finally
        CoreMailboxApi.dispose first
    let second = CoreMailbox.createFile dir admittedCredentials
    try
        let! history =
            CoreMailboxApi.eventHistory second
            |> Async.StartAsTask
        let stored = Assert.Single(changesOnly history.events)
        Assert.Equal(event.submissionId, stored.submissionId)
        Assert.True(EventId.isAccepted stored.id)
        Assert.NotEqual(stored.id, EventLog.nextId history)
    finally
        CoreMailboxApi.dispose second
}

[<Fact>]
let ``EventLog.restore seeds mailbox across Db restart`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let _, event = addRootChild "db-restore-seed"
    let first = admittedHostDb (DbAgent.create connStr)
    try
        let! accepted =
            CoreMailboxApi.postEvents first testCaller [ event ]
            |> Async.StartAsTask
        requireOk "postChange" accepted |> ignore
    finally
        CoreMailboxApi.dispose first
    let second = admittedHostDb (DbAgent.create connStr)
    try
        let! history =
            CoreMailboxApi.eventHistory second
            |> Async.StartAsTask
        let stored = Assert.Single(changesOnly history.events)
        Assert.Equal(event.submissionId, stored.submissionId)
        Assert.True(EventId.isAccepted stored.id)
        Assert.NotEqual(stored.id, EventLog.nextId history)
    finally
        CoreMailboxApi.dispose second
}

[<Fact>]
let ``StartActor persists across File restart`` () = task {
    let dir = newTempDir ()
    let first =
        CoreMailbox.host
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create dir))
            admittedCredentials
    try
        let! started =
            startFromFunction first sampleActorStart
            |> Async.StartAsTask
        requireOk "startActor" started |> ignore
    finally
        CoreMailboxApi.dispose first
    let second = CoreMailbox.createFile dir admittedCredentials
    try
        let! events =
            CoreMailboxApi.getEventsSince second (EventId.zero)
            |> Async.StartAsTask
        Assert.True(
            List.exists bodyIsStartActor events,
            "StartActor missing after restore")
    finally
        CoreMailboxApi.dispose second
}

let private withFilling filling pool body = task {
    let host = CoreMailbox.host pool filling admittedCredentials
    try
        do! body host
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``getEventsSince seed Error propagates and closes writes`` () =
    let persist = FileAgent.persist (FileAgent.create (newTempDir ()))
    let filling =
        { persist with
            handlers =
                { persist.handlers with
                    getEventsSince =
                        fun _ -> Error "events unavailable" } }
    withFilling filling (CoreActorPool.create ()) (fun host -> task {
        let! result =
            MailboxHost.postMailboxAndAsyncReply host (fun channel ->
                Mailbox.Ask(Query.EventsSince(EventId.zero, channel)))
            |> Async.StartAsTask
        match result with
        | Ok _ -> Assert.Fail("expected seed Error from EventsSince")
        | Error error -> Assert.Equal("events unavailable", error)
        let _, event = addRootChild "seed-fail"
        let! posted =
            CoreMailboxApi.postEvents host testCaller [ event ]
            |> Async.StartAsTask
        match posted with
        | Ok _ ->
            Assert.Fail("expected seed persist fail to close writes")
        | Error error ->
            Assert.Contains("events unavailable", error)
    })

[<Fact>]
let ``StartActor persist Error does not keep Ev in mailbox log`` () =
    let persist = FileAgent.persist (FileAgent.create (newTempDir ()))
    let filling =
        { persist with
            handlers =
                { persist.handlers with
                    appendEvent =
                        fun event ->
                            match event.body with
                            | EventBody.Order (Order.StartActor _) ->
                                Error "event persist failed"
                            | _ -> persist.handlers.appendEvent event } }
    withFilling filling (CoreActorPool.create ()) (fun host -> task {
        let! started =
            startFromFunction host sampleActorStart
            |> Async.StartAsTask
        match started with
        | Ok _ -> Assert.Fail("expected appendEvent Error")
        | Error error ->
            Assert.Contains("event persist failed", error)
        let! history =
            CoreMailboxApi.eventHistory host |> Async.StartAsTask
        Assert.False(
            List.exists bodyIsStartActor history.events,
            "StartActor kept after persist Error")
    })

[<Fact>]
let ``post Change uses Event id past Database row when payload does not decode`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use cmd = conn.CreateCommand()
    let occupied = EventLog.empty.nextId
    cmd.CommandText <-
        "INSERT INTO events (event_id, submission_id, payload) "
        + $"VALUES ({EventId.toJson occupied}, '{Guid.NewGuid()}', 'not-json')"
    let! _ = cmd.ExecuteNonQueryAsync()
    let host = admittedHostDb (DbAgent.create connStr)
    try
        let _, event = addRootChild "after-stale-row"
        let! posted =
            CoreMailboxApi.postEvents host testCaller [ event ]
            |> Async.StartAsTask
        match posted with
        | Error error -> Assert.Fail($"postChange: {error}")
        | Ok accepted ->
            let stored = Assert.Single(accepted.events)
            Assert.True(EventId.isAccepted stored.id)
            Assert.NotEqual(occupied, stored.id)
    finally
        CoreMailboxApi.dispose host
}
