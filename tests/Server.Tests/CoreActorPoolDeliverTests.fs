module Gambol.Server.Tests.CoreActorPoolDeliverTests

open System
open System.Threading
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private sampleRequest: Gambol.Shared.ActorStart =
    { zoomId = Graph.rootId
      focusId = Graph.rootId
      runDefinitionId = Graph.rootId
      graphIds = [ Graph.rootId ]
      eventId = EventId.zero }

/// Records the session the way RunAgentActor.actorFn does, then stays live.
let private liveSessionActor
    (pool: CoreActorPool)
    (session: string ref)
    : Gambol.Shared.ActorFunction =
    fun run _ ->
        async {
            match pool.sessionIdFor run.secret with
            | Some sessionId -> session := sessionId
            | None -> ()
            do! Async.Sleep 60_000
        }

let private startFunction host actor request =
    let placement : ActorOrderFromStart.FunctionActorPlacement =
        { request = request
          actor = actor
          actorName = "test"
          actorNodeIdOf = ActorOrderFromStart.focusIdOf }
    CoreMailboxApi.coreMailboxStartActorFromFunction
        host
        testCaller
        placement

let private waitSession (session: string ref) =
    let deadline = DateTime.UtcNow.AddSeconds 2.0
    while !session = "" && DateTime.UtcNow < deadline do
        Thread.Sleep 10
    Assert.False(String.IsNullOrEmpty !session)

let private withLiveSession body =
    task {
        let session = ref ""
        let dataDir = newTempDir ()
        let pool = CoreActorPool.create ()
        let host =
            CoreMailbox.host
                pool
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
        try
            let! started =
                startFunction
                    host
                    (liveSessionActor pool session)
                    sampleRequest
                |> Async.StartAsTask
            requireOk "start" started |> ignore
            waitSession session
            do! body pool !session
        finally
            CoreMailboxApi.dispose host
    }

let private dropThenLateFails (pool: CoreActorPool) session =
    match pool.trySecretForActorNode Graph.rootId with
    | None -> Assert.Fail("missing live secret")
    | Some secret -> pool.drop secret
    match pool.deliver (session, "late") with
    | Error "not live" -> ()
    | other -> Assert.Fail($"drop deliver: {other}")

[<Fact>]
let ``mint deliver enqueue drop then deliver fails`` () =
    withLiveSession (fun pool session -> task {
        requireOk "deliver" (pool.deliver (session, "chunk"))
        match pool.takeInbox session with
        | Ok texts ->
            Assert.Equal<string list>([ "chunk" ], texts)
        | Error err -> Assert.Fail($"inbox: {err}")
        dropThenLateFails pool session
    })

[<Fact>]
let ``empty deliver keeps session live for a later chunk`` () =
    withLiveSession (fun pool session -> task {
        requireOk "empty" (pool.deliver (session, ""))
        requireOk "later" (pool.deliver (session, "later"))
        match pool.takeInbox session with
        | Ok texts ->
            Assert.Equal<string list>([ ""; "later" ], texts)
        | Error err -> Assert.Fail($"inbox: {err}")
        dropThenLateFails pool session
    })
