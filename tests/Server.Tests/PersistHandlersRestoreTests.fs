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
        let stored = Assert.Single(events)
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
        let stored = Assert.Single(history.events)
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
        let stored = Assert.Single(history.events)
        Assert.Equal(event.submissionId, stored.submissionId)
        Assert.True(EventId.isAccepted stored.id)
        Assert.NotEqual(stored.id, EventLog.nextId history)
    finally
        CoreMailboxApi.dispose second
}

[<Fact>]
let ``ActorStart persists across File restart`` () = task {
    let dir = newTempDir ()
    let actorSecret = Credential "persist-restore-actor"
    let pool: CoreActorPool =
        { register = fun _ _ -> ()
          registerPeer = fun _ _ -> ()
          startActor = fun _ _ -> Ok actorSecret
          startFunction = fun _ -> Error "unused"
          startPeerActor = fun _ _ _ -> Error "unused"
          schedule = fun _ _ -> ()
          isLive = fun secret -> secret = actorSecret
          admit = fun _ -> Ok ()
          drop = fun _ -> ()
          finish = fun _ _ -> Ok ()
          liveFocusIds = fun () -> Set.empty
          getFocusId = fun _ -> Some Graph.rootId
          trySecretForFocus = fun _ -> None
          deliver = fun _ -> Error "not live"
          takeInbox = fun _ -> Error "not live" }
    let first =
        CoreMailbox.host
            pool
            (FileAgent.persist (FileAgent.create dir))
            admittedCredentials
    try
        let request: ActorStart =
            { zoomId = Graph.rootId
              focusId = Graph.rootId
              commandId = Graph.rootId
              graphIds = [ Graph.rootId ]
              eventId = EventId.zero }
        let! started =
            CoreMailboxApi.startActor first testCaller request
            |> Async.StartAsTask
        requireOk "startActor" started
    finally
        CoreMailboxApi.dispose first
    let second = CoreMailbox.createFile dir admittedCredentials
    try
        let! events =
            CoreMailboxApi.getEventsSince second (EventId.zero)
            |> Async.StartAsTask
        Assert.True(
            events
            |> List.exists (fun e ->
                match e.body with
                | EventBody.ActorStart _ -> true
                | _ -> false),
            "ActorStart missing after restore")
    finally
        CoreMailboxApi.dispose second
}

let private sampleActorStart: ActorStart =
    { zoomId = Graph.rootId
      focusId = Graph.rootId
      commandId = Graph.rootId
      graphIds = [ Graph.rootId ]
      eventId = EventId.zero }

let private stubPool secret : CoreActorPool =
    { register = fun _ _ -> ()
      registerPeer = fun _ _ -> ()
      startActor = fun _ _ -> Ok secret
      startFunction = fun _ -> Error "unused"
      startPeerActor = fun _ _ _ -> Error "unused"
      schedule = fun _ _ -> ()
      isLive = fun s -> s = secret
      admit = fun _ -> Ok ()
      drop = fun _ -> ()
      finish = fun _ _ -> Ok ()
      liveFocusIds = fun () -> Set.empty
      getFocusId = fun _ -> Some Graph.rootId
      trySecretForFocus = fun _ -> None
      deliver = fun _ -> Error "not live"
      takeInbox = fun _ -> Error "not live" }

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
let ``ActorStart persist Error does not keep Ev in mailbox log`` () =
    let persist = FileAgent.persist (FileAgent.create (newTempDir ()))
    let filling =
        { persist with
            handlers =
                { persist.handlers with
                    appendEvent = fun _ -> Error "event persist failed" } }
    let pool = stubPool (Credential "persist-append-fail")
    withFilling filling pool (fun host -> task {
        let! started =
            CoreMailboxApi.startActor host testCaller sampleActorStart
            |> Async.StartAsTask
        match started with
        | Ok _ -> Assert.Fail("expected appendEvent Error")
        | Error error ->
            Assert.Contains("event persist failed", error)
        let! history =
            CoreMailboxApi.eventHistory host |> Async.StartAsTask
        Assert.Empty(history.events)
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
