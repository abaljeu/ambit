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

let private sampleRequest: StartActorRequest =
    { zoomId = Graph.rootId
      focusId = Graph.rootId
      commandId = Graph.rootId
      graphIds = [ Graph.rootId ]
      revision = Revision 0 }

let private addRootChild text =
    let childId = NodeId.New()
    { id = 0
      changeId = Guid.NewGuid()
      ops =
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

let private actorCaller secret =
    { authority = Authority "Actor"
      secret = secret }

let private recordingPool () =
    let started = TaskCompletionSource<StartActorRequest>()
    let stopped = ResizeArray<Credential * ActorResult>()
    let live = ResizeArray<Credential>()
    let pool: CoreActorPool = {
        register = fun _ _ -> ()
        startActor =
            fun request _ ->
                started.TrySetResult request |> ignore
                Ok { secret = Credential "recorded"; focusId = request.focusId }
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
    }
    started, stopped, live, pool

let private createHost dataDir pool =
    CoreMailbox.host
        pool
        (FileAgent.persist (FileAgent.create dataDir))
        admittedSecrets

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
    host.mailbox.PostAndAsyncReply(fun reply ->
        StartActor(caller, request, reply))

let private postActorStop host caller result =
    host.mailbox.PostAndAsyncReply(fun reply ->
        ActorStop(caller, result, reply))

[<Fact>]
let ``StartActor with live credentials calls startActor with StartActorRequest`` () =
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
        Assert.Equal(sampleRequest.revision, handed.revision)
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
        let change = addRootChild "actor-hello"
        let! result =
            CoreMailbox.postChange
                host (actorCaller actorSecret) [ change ]
            |> Async.StartAsTask
        let accepted = requireOk "Actor post" result
        Assert.Equal(Revision 1, accepted.revision)
    })

[<Fact>]
let ``Actor PostChange without live row is refused before persist`` () =
    let _, _, _, pool = recordingPool ()
    let actorSecret = Credential "actor-not-live"
    withHost pool (fun host -> task {
        let handle = CoreMailbox.coreChanges host testCaller
        let! before = handle.getRevision () |> Async.StartAsTask
        let! result =
            CoreMailbox.postChange
                host
                (actorCaller actorSecret)
                [ addRootChild "nope" ]
            |> Async.StartAsTask
        let! after = handle.getRevision () |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
        Assert.Equal(before, after)
    })

[<Fact>]
let ``Browser PostChange does not require a live row`` () =
    let _, _, _, pool = recordingPool ()
    withHost pool (fun host -> task {
        let! result =
            CoreMailbox.postChange
                host
                { authority = Authority "Browser"
                  secret = testSecret }
                [ addRootChild "browser" ]
            |> Async.StartAsTask
        let accepted = requireOk "Browser post" result
        Assert.Equal(Revision 1, accepted.revision)
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
                host (actorCaller actorSecret) ActorFailed
            |> Async.StartAsTask
        requireOk "ActorStop fail" result
        Assert.Equal(1, stopped.Count)
        Assert.Equal(actorSecret, fst stopped.[0])
        Assert.Equal(ActorFailed, snd stopped.[0])
        Assert.False(live.Contains actorSecret)
    })
