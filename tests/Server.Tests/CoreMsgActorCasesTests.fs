module Gambol.Server.Tests.CoreMsgActorCasesTests

open System
open System.Diagnostics
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

/// Seam: CoreMsg union (StartActor, Actor PostChange live-table, ActorStop).

let private waitCancelled () =
    async {
        let! token = Async.CancellationToken
        do! Async.AwaitWaitHandle token.WaitHandle |> Async.Ignore
        return ActorSucceeded
    }

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

let private postActorStop host caller result =
    CoreMailboxApi.actorStop host caller result

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
    // functionActorAt calls actorStop. Leave the row so getState can stamp it.
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

[<Fact>]
let ``ActorStop ActorSucceeded drops live row without waiting`` () =
    let stopped, live, pool = recordingPool ()
    let actorSecret = Credential "actor-stop"
    live.Add actorSecret
    withHost pool (fun host -> task {
        let lingering = Task.Delay 5000
        let sw = Stopwatch.StartNew()
        let! result =
            postActorStop
                host (actorCaller actorSecret) ActorSucceeded
            |> Async.StartAsTask
        sw.Stop()
        requireOk "ActorStop" result
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
        let! result =
            postActorStop
                host (actorCaller actorSecret) (ActorFailed "")
            |> Async.StartAsTask
        requireOk "ActorStop fail" result
        Assert.Equal(1, stopped.Count)
        Assert.Equal(actorSecret, fst stopped.[0])
        Assert.Equal(ActorFailed "", snd stopped.[0])
        Assert.False(live.Contains actorSecret)
    })

[<Fact>]
let ``ActorStop checks the live receipt once`` () =
    let isLiveCalls = ResizeArray<Credential>()
    let live = ResizeArray<Credential>()
    let actorSecret = Credential "actor-once"
    live.Add actorSecret
    let pool =
        { poolForFixedSecret actorSecret with
            isLive =
                fun secret ->
                    isLiveCalls.Add secret
                    live.Contains secret
            finish =
                fun secret _ ->
                    live.Remove secret |> ignore
                    Ok ()
            getActorNodeId =
                fun secret ->
                    if live.Contains secret then
                        Some sampleRequest.focusId
                    else
                        None }
    withHost pool (fun host -> task {
        let! result =
            postActorStop
                host (actorCaller actorSecret) ActorSucceeded
            |> Async.StartAsTask
        requireOk "ActorStop" result
        Assert.Equal(1, isLiveCalls.Count)
        Assert.Equal(actorSecret, isLiveCalls.[0])
        Assert.False(live.Contains actorSecret)
    })

[<Fact>]
let ``function start yields its secret and a later stop is dropped`` () =
    let pool = CoreActorPool.create ()
    let seen = TaskCompletionSource<Credential>()
    let actor : Gambol.Shared.ActorFunction =
        fun run _ -> async {
            seen.TrySetResult run.secret |> ignore
            return ActorSucceeded
        }
    withHost pool (fun host -> task {
        let! started =
            startFunction host testCaller actor sampleRequest
            |> Async.StartAsTask
        requireOk "function start" started |> ignore
        let! secret = seen.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        let caller = actorCaller secret
        let! stopped =
            postActorStop host caller ActorSucceeded
            |> Async.StartAsTask
        requireOk "ActorStop" stopped
        Assert.False(pool.isLive secret)
        let! again =
            postActorStop host caller ActorSucceeded
            |> Async.StartAsTask
        Assert.Equal(Error "authority dropped", again)
    })

[<Fact>]
let ``ActorStop after the pool row is finished is dropped`` () =
    let stopped, live, pool = recordingPool ()
    let actorSecret = Credential "actor-finished"
    live.Add actorSecret
    withHost pool (fun host -> task {
        let caller = actorCaller actorSecret
        let! first =
            postActorStop host caller ActorSucceeded
            |> Async.StartAsTask
        requireOk "first stop" first
        let! second =
            postActorStop host caller ActorSucceeded
            |> Async.StartAsTask
        Assert.Equal(Error "authority dropped", second)
        Assert.Equal(1, stopped.Count)
        Assert.False(live.Contains actorSecret)
    })

[<Fact>]
let ``ActorStop with Test credentials is refused`` () =
    let stopped, live, pool = recordingPool ()
    let actorSecret = Credential "actor-test-auth"
    live.Add actorSecret
    withHost pool (fun host -> task {
        let! result =
            postActorStop host testCaller ActorSucceeded
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
        Assert.Equal(0, stopped.Count)
        Assert.True(live.Contains actorSecret)
    })

[<Fact>]
let ``ActorStop with Browser credentials is refused`` () =
    let stopped, live, pool = recordingPool ()
    let actorSecret = Credential "actor-browser-auth"
    live.Add actorSecret
    withHost pool (fun host -> task {
        let secret = Credential "browser-stop"
        let! logged =
            CoreMailboxApi.login host "browser" secret
            |> Async.StartAsTask
        requireOk "login" logged
        let browserCaller =
            { authority = Authority "Browser"
              name = "browser"
              secret = secret }
        let! result =
            postActorStop host browserCaller ActorSucceeded
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
        Assert.Equal(0, stopped.Count)
        Assert.True(live.Contains actorSecret)
    })

[<Fact>]
let ``ActorStop with Parse credentials is refused`` () =
    let stopped, live, pool = recordingPool ()
    let actorSecret = Credential "actor-parse-auth"
    live.Add actorSecret
    let parseCaller =
        { authority = Authority "Parse"
          name = "process"
          secret = Credential "parse-secret" }
    let creds = CoreCredentials.add parseCaller admittedCredentials
    task {
        let dataDir = newTempDir ()
        let host =
            CoreMailbox.host
                pool
                (FileAgent.persist (FileAgent.create dataDir))
                creds
        try
            let! result =
                postActorStop host parseCaller ActorSucceeded
                |> Async.StartAsTask
            Assert.Equal(Error CoreAuth.refuse, result)
            Assert.Equal(0, stopped.Count)
            Assert.True(live.Contains actorSecret)
        finally
            CoreMailboxApi.dispose host
    }

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
