module Gambol.Server.Tests.CoreMsgActorCasesTests

open System
open System.Diagnostics
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

/// Seam: CoreMsg union (StartActor, Actor PostChange live-table, ActorStop).

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private sampleRequest: Gambol.Shared.ActorStart =
    { zoomId = Graph.rootId
      focusId = Graph.rootId
      commandId = Graph.rootId
      graphIds = [ Graph.rootId ]
      eventId = EventId.zero }

let private addRootChild text = addRootChildEvent text |> snd

let private actorCaller secret =
    { authority = Authority "Actor"
      name = ""
      secret = secret }

let private recordingPool () =
    let started = TaskCompletionSource<Gambol.Shared.ActorStart>()
    let stopped = ResizeArray<Credential * ActorResult>()
    let live = ResizeArray<Credential>()
    let pool: CoreActorPool = {
        register = fun _ _ -> ()
        startActor =
            fun request _ ->
                started.TrySetResult request |> ignore
                Ok (Credential "recorded")
        schedule = fun _ _ -> ()
        isLive = fun secret -> live.Contains secret
        admit = fun _ -> Ok ()
        drop = fun _ -> ()
        finish =
            fun secret result ->
                live.Remove secret |> ignore
                stopped.Add(secret, result)
                Ok ()
        liveFocusIds = fun () -> Set.empty
        getFocusId = fun _ -> None
        trySecretForFocus = fun _ -> None
        deliver = fun _ -> Error "not live"
        takeInbox = fun _ -> Error "not live"
    }
    started, stopped, live, pool

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
            CoreMailbox.dispose host
    }

let private postStartActor host caller request =
    CoreMailbox.startActor host caller request

let private postActorStop host caller result =
    CoreMailbox.actorStop host caller result

[<Fact>]
let ``StartActor with live credentials calls startActor with ActorStart`` () =
    let started, _, _, pool = recordingPool ()
    withHost pool (fun host -> task {
        let! result =
            postStartActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "StartActor" result
        let! handed = started.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        Assert.Equal(sampleRequest.zoomId, handed.zoomId)
        Assert.Equal(sampleRequest.focusId, handed.focusId)
        Assert.Equal(sampleRequest.commandId, handed.commandId)
        Assert.Equal<NodeId list>(sampleRequest.graphIds, handed.graphIds)
        Assert.Equal(sampleRequest.eventId, handed.eventId)
    })

[<Fact>]
let ``StartActor reply is startActor bookkeeping without waiting for an Actor body`` () =
    let pool = CoreActorPool.create ()
    pool.register (ActorName "root") (fun _ _ -> async.Return ())
    withHost pool (fun host -> task {
        let sw = Stopwatch.StartNew()
        let! result =
            postStartActor host testCaller sampleRequest
            |> Async.StartAsTask
        sw.Stop()
        requireOk "StartActor" result
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds 1.0)
        Assert.True(Set.contains sampleRequest.focusId (pool.liveFocusIds ()))
    })

[<Fact>]
let ``GetState stamps lockPresent from the live table after startActor`` () =
    let pool = CoreActorPool.create ()
    pool.register (ActorName "root") (fun _ _ -> async.Return ())
    withHost pool (fun host -> task {
        let! started =
            postStartActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "StartActor" started
        let! state =
            CoreMailbox.getState host
            |> Async.StartAsTask
        let state = requireOk "GetState" state
        Assert.True(state.graph.nodes.[sampleRequest.focusId].lockPresent)
    })

[<Fact>]
let ``StartActor with inactive secret does not hand off`` () =
    let started, _, _, pool = recordingPool ()
    withHost pool (fun host -> task {
        let! result =
            postStartActor
                host
                { authority = testAuthority
                  name = testCaller.name
                  secret = Credential "inactive" }
                sampleRequest
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
        Assert.False(started.Task.IsCompleted)
    })

[<Fact>]
let ``StartActor with blank Authority does not hand off`` () =
    let started, _, _, pool = recordingPool ()
    withHost pool (fun host -> task {
        let! result =
            postStartActor
                host
                { authority = Authority "  "
                  name = testCaller.name
                  secret = testSecret }
                sampleRequest
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
        Assert.False(started.Task.IsCompleted)
    })

[<Fact>]
let ``Actor PostChange with live row reaches PersistHandlers`` () =
    let _, _, live, pool = recordingPool ()
    let actorSecret = Credential "actor-live"
    live.Add actorSecret
    withHost pool (fun host -> task {
        let event = addRootChild "actor-hello"
        let! result =
            CoreMailbox.postEvents
                host
                (actorCaller actorSecret)
                [ event ]
            |> Async.StartAsTask
        let accepted = requireOk "Actor post" result
        Assert.True(EventId.isAccepted accepted.eventId)
    })

[<Fact>]
let ``Actor PostChange without live row is refused before persist`` () =
    let _, _, _, pool = recordingPool ()
    let actorSecret = Credential "actor-not-live"
    withHost pool (fun host -> task {
        let handle = CoreMailbox.coreChanges host testCaller
        let! before = handle.getEventId () |> Async.StartAsTask
        let! result =
            CoreMailbox.postEvents
                host
                (actorCaller actorSecret)
                [ addRootChild "nope" ]
            |> Async.StartAsTask
        let! after = handle.getEventId () |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
        Assert.Equal(before, after)
    })

[<Fact>]
let ``Browser PostChange does not require a live row`` () =
    let _, _, _, pool = recordingPool ()
    withHost pool (fun host -> task {
        let! result =
            CoreMailbox.postEvents
                host
                testCaller
                [ addRootChild "browser" ]
            |> Async.StartAsTask
        let accepted = requireOk "Browser post" result
        Assert.True(EventId.isAccepted accepted.eventId)
    })

[<Fact>]
let ``ActorStop ActorSucceeded drops live row without waiting`` () =
    let _, stopped, live, pool = recordingPool ()
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
    let _, stopped, live, pool = recordingPool ()
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
let ``ActorStop consults isLive once for a live Actor`` () =
    let isLiveCalls = ResizeArray<Credential>()
    let live = ResizeArray<Credential>()
    let actorSecret = Credential "actor-once"
    live.Add actorSecret
    let pool: CoreActorPool = {
        register = fun _ _ -> ()
        startActor =
            fun _ _ ->
                Ok actorSecret
        schedule = fun _ _ -> ()
        isLive =
            fun secret ->
                isLiveCalls.Add secret
                live.Contains secret
        admit = fun _ -> Ok ()
        drop = fun _ -> ()
        finish =
            fun secret _ ->
                live.Remove secret |> ignore
                Ok ()
        liveFocusIds = fun () -> Set.empty
        getFocusId = fun _ -> None
        trySecretForFocus = fun _ -> None
        deliver = fun _ -> Error "not live"
        takeInbox = fun _ -> Error "not live"
    }
    withHost pool (fun host -> task {
        let! result =
            postActorStop
                host (actorCaller actorSecret) ActorSucceeded
            |> Async.StartAsTask
        requireOk "ActorStop" result
        Assert.Equal(1, isLiveCalls.Count)
        Assert.Equal(actorSecret, isLiveCalls.[0])
    })

[<Fact>]
let ``live Actor stop succeeds and already-finished cannot`` () =
    let pool = CoreActorPool.create ()
    let seen = TaskCompletionSource<Credential>()
    pool.register (ActorName "root") (fun input _ -> async {
        seen.TrySetResult input.secret |> ignore
    })
    withHost pool (fun host -> task {
        let! started =
            postStartActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "StartActor" started
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
        Assert.Equal(Error CoreAuth.refuse, again)
    })

[<Fact>]
let ``ActorStop of an already-finished Actor is refused`` () =
    let _, stopped, live, pool = recordingPool ()
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
        Assert.Equal(Error CoreAuth.refuse, second)
        Assert.Equal(1, stopped.Count)
        Assert.False(live.Contains actorSecret)
    })

[<Fact>]
let ``ActorStop with Test credentials is refused`` () =
    let _, stopped, live, pool = recordingPool ()
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
    let _, stopped, live, pool = recordingPool ()
    let actorSecret = Credential "actor-browser-auth"
    live.Add actorSecret
    withHost pool (fun host -> task {
        let secret = Credential "browser-stop"
        let! logged =
            CoreMailbox.login host "browser" secret
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
    let _, stopped, live, pool = recordingPool ()
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
            CoreMailbox.dispose host
    }
