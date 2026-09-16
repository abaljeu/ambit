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

let private addRootChild text =
    let childId = NodeId.New()
    childId,
    { id = 0
      submissionId = Guid.NewGuid()
      ops =
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

[<Fact>]
let ``getEventsSince returns Ev after postChange`` () = task {
    let dir = newTempDir ()
    let host =
        CoreMailbox.createFile dir admittedCredentials
    try
        let _, change = addRootChild "persist-event"
        let! accepted =
            CoreMailbox.postChange host testCaller [ change ]
            |> Async.StartAsTask
        requireOk "postChange" accepted |> ignore
        let! events =
            CoreMailbox.getEventsSince
                host
                (EventId -1)
            |> Async.StartAsTask
        let stored = Assert.Single(events)
        Assert.Equal(change.submissionId, stored.submissionId)
        match stored.body with
        | EventBody.Change _ -> ()
        | _ -> Assert.Fail("expected Change EventBody")
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``EventLog.restore seeds mailbox across File restart`` () = task {
    let dir = newTempDir ()
    let _, change = addRootChild "restore-seed"
    let first = CoreMailbox.createFile dir admittedCredentials
    try
        let! accepted =
            CoreMailbox.postChange first testCaller [ change ]
            |> Async.StartAsTask
        requireOk "postChange" accepted |> ignore
    finally
        CoreMailbox.dispose first
    let second = CoreMailbox.createFile dir admittedCredentials
    try
        let! history =
            CoreMailbox.eventHistory second
            |> Async.StartAsTask
        let stored = Assert.Single(history.events)
        Assert.Equal(change.submissionId, stored.submissionId)
        Assert.Equal(EventId 2, EventLog.nextId history)
    finally
        CoreMailbox.dispose second
}

[<Fact>]
let ``EventLog.restore seeds mailbox across Db restart`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let _, change = addRootChild "db-restore-seed"
    let first = admittedHostDb (DbAgent.create connStr)
    try
        let! accepted =
            CoreMailbox.postChange first testCaller [ change ]
            |> Async.StartAsTask
        requireOk "postChange" accepted |> ignore
    finally
        CoreMailbox.dispose first
    let second = admittedHostDb (DbAgent.create connStr)
    try
        let! history =
            CoreMailbox.eventHistory second
            |> Async.StartAsTask
        let stored = Assert.Single(history.events)
        Assert.Equal(change.submissionId, stored.submissionId)
        Assert.Equal(EventId 2, EventLog.nextId history)
    finally
        CoreMailbox.dispose second
}

[<Fact>]
let ``ActorStart persists across File restart`` () = task {
    let dir = newTempDir ()
    let actorSecret = Credential "persist-restore-actor"
    let pool: CoreActorPool =
        { register = fun _ _ -> ()
          startActor = fun _ _ -> Ok actorSecret
          schedule = fun _ _ -> ()
          isLive = fun secret -> secret = actorSecret
          admit = fun _ -> Ok ()
          drop = fun _ -> ()
          finish = fun _ _ -> Ok ()
          liveFocusIds = fun () -> Set.empty
          getFocusId = fun _ -> Some Graph.rootId }
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
              revision = EventId 0 }
        let! started =
            CoreMailbox.startActor first testCaller request
            |> Async.StartAsTask
        requireOk "startActor" started
    finally
        CoreMailbox.dispose first
    let second = CoreMailbox.createFile dir admittedCredentials
    try
        let! events =
            CoreMailbox.getEventsSince second (EventId -1)
            |> Async.StartAsTask
        Assert.True(
            events
            |> List.exists (fun e ->
                match e.body with
                | EventBody.ActorStart _ -> true
                | _ -> false),
            "ActorStart missing after restore")
    finally
        CoreMailbox.dispose second
}

let private sampleActorStart: ActorStart =
    { zoomId = Graph.rootId
      focusId = Graph.rootId
      commandId = Graph.rootId
      graphIds = [ Graph.rootId ]
      revision = EventId 0 }

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

let private withFilling filling pool body = task {
    let host = CoreMailbox.host pool filling admittedCredentials
    try
        do! body host
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``getEventsSince Error does not seed empty EventLog as success`` () =
    let persist = FileAgent.persist (FileAgent.create (newTempDir ()))
    let filling =
        { persist with
            handlers =
                { persist.handlers with
                    getEventsSince =
                        fun _ -> Error "events unavailable" } }
    withFilling filling (CoreActorPool.create ()) (fun host -> task {
        try
            let! _ =
                CoreMailbox.getEventsSince host (EventId -1)
                |> Async.StartAsTask
            Assert.Fail("expected persist getEventsSince Error")
        with ex ->
            Assert.Contains("events unavailable", ex.Message)
        let _, change = addRootChild "seed-fail"
        let! posted =
            CoreMailbox.postChange host testCaller [ change ]
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
            CoreMailbox.startActor host testCaller sampleActorStart
            |> Async.StartAsTask
        match started with
        | Ok _ -> Assert.Fail("expected appendEvent Error")
        | Error error ->
            Assert.Contains("event persist failed", error)
        let! history =
            CoreMailbox.eventHistory host |> Async.StartAsTask
        Assert.Empty(history.events)
    })
