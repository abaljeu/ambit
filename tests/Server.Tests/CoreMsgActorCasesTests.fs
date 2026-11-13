module Gambol.Server.Tests.CoreMsgActorCasesTests

open System
open System.Diagnostics
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

/// Seam: function start, Actor PostChange, and Notice.ActorStopped.

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

let private addRootChild text = addRootChildEvent text |> snd

let private actorCaller secret =
    { authority = Authority "Actor"
      name = ""
      secret = secret }

let private recordingPool () =
    let stopped = ResizeArray<Credential * ActorResult>()
    let live = ResizeArray<Credential>()
    let pool =
        { poolForFixedSecret (Credential "recorded") with
            isLive = fun secret -> live.Contains secret
            finish =
                fun secret result ->
                    live.Remove secret |> ignore
                    stopped.Add(secret, result)
                    Ok ()
            getActorNodeId =
                fun secret ->
                    if live.Contains secret then
                        Some sampleRequest.focusId
                    else
                        None
            trySecretForActorNode =
                fun nodeId ->
                    if nodeId = sampleRequest.focusId then
                        live |> Seq.tryHead
                    else
                        None }
    stopped, live, pool

let private createHost dataDir pool =
    CoreMailbox.host
        pool
        (FileAgent.persist (FileAgent.create dataDir))
        admittedCredentials

let private withHost pool body =
    task {
        let dataDir = newTempDir ()
        let host = createHost dataDir pool
        try
            do! body host
        finally
            CoreMailboxApi.dispose host
    }

let private startFunction host caller actor request =
    let placement : ActorOrderFromStart.FunctionActorPlacement =
        { request = request
          actor = actor
          actorName = "test"
          actorNodeIdOf = ActorOrderFromStart.focusIdOf }
    CoreMailboxApi.coreMailboxStartActorFromFunction
        host caller placement

let private stopsOf focusId events =
    events
    |> List.filter (fun event ->
        match event.body with
        | EventBody.ActorStop(id, _) when id = focusId -> true
        | _ -> false)

let private postActorStop host focusId result = async {
    CoreMailboxApi.postNotice
        host
        (Notice.ActorStopped(focusId, result))
    let! history = CoreMailboxApi.eventHistory host
    return history.events
}

[<Fact>]
let ``function start reply is bookkeeping without waiting for an Actor body`` () =
    let pool = CoreActorPool.create ()
    let actor : Gambol.Shared.ActorFunction =
        fun _ _ -> waitCancelled ()
    withHost pool (fun host -> task {
        let sw = Stopwatch.StartNew()
        let! result =
            startFunction host testCaller actor sampleRequest
            |> Async.StartAsTask
        sw.Stop()
        requireOk "function start" result |> ignore
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds 1.0)
        Assert.True(Set.contains sampleRequest.focusId (pool.liveActorNodeIds ()))
    })

[<Fact>]
let ``GetState stamps lockPresent from the live table after a function start`` () =
    let basePool = CoreActorPool.create ()
    // The pool posts ActorStopped and finish drops the row.
    // Leave the row so getState can stamp it.
    let pool = { basePool with finish = fun _ _ -> Ok () }
    let actor =
        TestActor.functionActorAt
            sampleRequest.runDefinitionId
            sampleRequest.focusId
    withHost pool (fun host -> task {
        let! started =
            startFunction host testCaller actor sampleRequest
            |> Async.StartAsTask
        requireOk "function start" started |> ignore
        let! state =
            CoreMailboxApi.graphAtEvent host
            |> Async.StartAsTask
        let state = requireOk "GetState" state
        Assert.True(state.graph.nodes.[sampleRequest.focusId].lockPresent)
    })

[<Fact>]
let ``function start from an inactive Actor does not hand off`` () =
    let pool = CoreActorPool.create ()
    let actor =
        TestActor.functionActorAt
            sampleRequest.runDefinitionId
            sampleRequest.focusId
    withHost pool (fun host -> task {
        let! result =
            startFunction
                host
                { authority = Authority "Actor"
                  name = testCaller.name
                  secret = Credential "inactive" }
                actor
                sampleRequest
            |> Async.StartAsTask
        Assert.Equal(Error "authority dropped", result)
        Assert.False(
            Set.contains sampleRequest.focusId (pool.liveActorNodeIds ()))
    })

[<Fact>]
let ``function start with blank Authority does not hand off`` () =
    let pool = CoreActorPool.create ()
    let actor =
        TestActor.functionActorAt
            sampleRequest.runDefinitionId
            sampleRequest.focusId
    withHost pool (fun host -> task {
        let! result =
            startFunction
                host
                { authority = Authority "  "
                  name = testCaller.name
                  secret = testSecret }
                actor
                sampleRequest
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
        Assert.False(
            Set.contains sampleRequest.focusId (pool.liveActorNodeIds ()))
    })

[<Fact>]
let ``Actor PostChange with live row reaches CoreMailboxAccess`` () =
    let _, live, pool = recordingPool ()
    let actorSecret = Credential "actor-live"
    live.Add actorSecret
    withHost pool (fun host -> task {
        let event = addRootChild "actor-hello"
        let! result =
            CoreMailboxApi.postEvents
                host
                (actorCaller actorSecret)
                [ event ]
            |> Async.StartAsTask
        let accepted = requireOk "Actor post" result
        Assert.True(EventId.isAccepted accepted.eventId)
    })

[<Fact>]
let ``Actor PostChange without a live row is dropped`` () =
    let _, _, pool = recordingPool ()
    let actorSecret = Credential "actor-not-live"
    withHost pool (fun host -> task {
        let! result =
            CoreMailboxApi.postEvents
                host
                (actorCaller actorSecret)
                [ addRootChild "nope" ]
            |> Async.StartAsTask
        Assert.Equal(Error "authority dropped", result)
    })

[<Fact>]
let ``Browser PostChange does not require a live row`` () =
    let _, _, pool = recordingPool ()
    withHost pool (fun host -> task {
        let! result =
            CoreMailboxApi.postEvents
                host
                testCaller
                [ addRootChild "browser" ]
            |> Async.StartAsTask
        let accepted = requireOk "Browser post" result
        Assert.True(EventId.isAccepted accepted.eventId)
    })

let private assertOneStop focusId result events =
    let stops = stopsOf focusId events
    Assert.Equal(1, stops.Length)
    Assert.Equal(Authority "Actor", stops.[0].authority)
    match stops.[0].body with
    | EventBody.ActorStop(_, found) -> Assert.Equal(result, found)
    | other -> Assert.Fail($"expected ActorStop, got {other}")

[<Fact>]
let ``ActorStop ActorSucceeded drops live row without waiting`` () =
    let stopped, live, pool = recordingPool ()
    let actorSecret = Credential "actor-stop"
    live.Add actorSecret
    withHost pool (fun host -> task {
        let lingering = Task.Delay 5000
        let sw = Stopwatch.StartNew()
        let! events =
            postActorStop
                host sampleRequest.focusId ActorSucceeded
            |> Async.StartAsTask
        sw.Stop()
        assertOneStop sampleRequest.focusId ActorSucceeded events
        Assert.Equal(1, stopped.Count)
        Assert.Equal(actorSecret, fst stopped.[0])
        Assert.Equal(ActorSucceeded, snd stopped.[0])
        Assert.False(live.Contains actorSecret)
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds 1.0)
        Assert.False(lingering.IsCompleted)
    })

[<Fact>]
let ``ActorStop ActorFailed drops live row and records terminal`` () =
    let stopped, live, pool = recordingPool ()
    let actorSecret = Credential "actor-fail"
    live.Add actorSecret
    withHost pool (fun host -> task {
        let! events =
            postActorStop
                host sampleRequest.focusId (ActorFailed "")
            |> Async.StartAsTask
        assertOneStop
            sampleRequest.focusId (ActorFailed "") events
        Assert.Equal(1, stopped.Count)
        Assert.Equal(actorSecret, fst stopped.[0])
        Assert.Equal(ActorFailed "", snd stopped.[0])
        Assert.False(live.Contains actorSecret)
    })

[<Fact>]
let ``ActorStopped Notice looks up the live secret once`` () =
    let lookups = ResizeArray<NodeId>()
    let live = ResizeArray<Credential>()
    let actorSecret = Credential "actor-once"
    live.Add actorSecret
    let pool =
        { poolForFixedSecret actorSecret with
            isLive = fun secret -> live.Contains secret
            finish =
                fun secret _ ->
                    live.Remove secret |> ignore
                    Ok ()
            trySecretForActorNode =
                fun nodeId ->
                    lookups.Add nodeId
                    if nodeId = sampleRequest.focusId
                        && live.Contains actorSecret then
                        Some actorSecret
                    else
                        None }
    withHost pool (fun host -> task {
        let! events =
            postActorStop
                host sampleRequest.focusId ActorSucceeded
            |> Async.StartAsTask
        assertOneStop sampleRequest.focusId ActorSucceeded events
        Assert.Equal(1, lookups.Count)
        Assert.Equal(sampleRequest.focusId, lookups.[0])
        Assert.False(live.Contains actorSecret)
    })

[<Fact>]
let ``function start yields its secret and a later stop is dropped`` () =
    let pool = CoreActorPool.create ()
    let seen = TaskCompletionSource<Credential>()
    let actor : Gambol.Shared.ActorFunction =
        fun run _ -> async {
            seen.TrySetResult run.secret |> ignore
            return! waitCancelled ()
        }
    withHost pool (fun host -> task {
        let! started =
            startFunction host testCaller actor sampleRequest
            |> Async.StartAsTask
        requireOk "function start" started |> ignore
        let! secret = seen.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        let! stopped =
            postActorStop
                host sampleRequest.focusId ActorSucceeded
            |> Async.StartAsTask
        assertOneStop sampleRequest.focusId ActorSucceeded stopped
        Assert.False(pool.isLive secret)
        let! again =
            postActorStop
                host sampleRequest.focusId ActorSucceeded
            |> Async.StartAsTask
        Assert.Equal(1, (stopsOf sampleRequest.focusId again).Length)
    })

[<Fact>]
let ``ActorStop after the pool row is finished is dropped`` () =
    let stopped, live, pool = recordingPool ()
    let actorSecret = Credential "actor-finished"
    live.Add actorSecret
    withHost pool (fun host -> task {
        let! first =
            postActorStop
                host sampleRequest.focusId ActorSucceeded
            |> Async.StartAsTask
        assertOneStop sampleRequest.focusId ActorSucceeded first
        let! second =
            postActorStop
                host sampleRequest.focusId ActorSucceeded
            |> Async.StartAsTask
        Assert.Equal(1, (stopsOf sampleRequest.focusId second).Length)
        Assert.Equal(1, stopped.Count)
        Assert.False(live.Contains actorSecret)
    })

[<Fact>]
let ``ActorOrder places the actor on the thread pool`` () = task {
    let pool = CoreActorPool.create ()
    let host = acceptingHost pool
    let focusId = NodeId.New()
    let ran = TaskCompletionSource<Credential>()
    let actor : Gambol.Shared.ActorFunction =
        fun run _ ->
            async {
                ran.TrySetResult run.secret |> ignore
                return! waitCancelled ()
            }
    let order =
        { actor = actor
          actorNodeId = focusId
          actorName = "test" }
    try
        let! placed =
            MailboxHost.postActorOrder host testCaller order
            |> Async.StartAsTask
        match placed with
        | Error err -> Assert.Fail(err)
        | Ok _ ->
            let! secret = ran.Task
            Assert.NotEqual<Credential>(testSecret, secret)
            Assert.True(pool.isLive secret)
            Assert.Contains(focusId, pool.liveActorNodeIds ())
            let! history =
                CoreMailboxApi.eventHistory host |> Async.StartAsTask
            let stored =
                history.events
                |> List.filter (fun event ->
                    match event.body with
                    | EventBody.Order (Order.StartActor found) ->
                        found.actorNodeId = focusId
                    | _ -> false)
            match stored with
            | [ event ] ->
                Assert.Equal(testAuthority, event.authority)
            | other ->
                Assert.Fail($"expected one ActorOrder, got {other.Length}")
    finally
        CoreMailboxApi.dispose host
}
