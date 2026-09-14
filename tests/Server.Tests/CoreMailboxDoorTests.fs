module Gambol.Server.Tests.CoreMailboxDoorTests

open System
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

/// CoreMailbox door — public API tests using CoreMailbox.startActor / actorStop.

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

let private actorCaller secret =
    { authority = Authority "Actor"
      secret = secret }

let private createHost () =
    let dataDir = newTempDir ()
    let credentials = admittedCredentials ()
    let pool = CoreActorPool.create credentials
    let host =
        CoreMailbox.host
            credentials
            pool
            (FileAgent.persist (FileAgent.create dataDir))
    host, credentials, pool

let private withHost body =
    task {
        let host, credentials, pool = createHost ()
        try
            do! body host credentials pool
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``CoreMailbox.startActor calls pool.startActor and returns bookkeeping result`` () =
    withHost (fun host _ pool -> task {
        let! result =
            CoreMailbox.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        Assert.True(Set.contains sampleRequest.focusId (pool.liveFocusIds ()))
    })

[<Fact>]
let ``CoreMailbox.startActor with inactive secret is refused`` () =
    withHost (fun host _ _ -> task {
        let! result =
            CoreMailbox.startActor
                host
                { authority = testAuthority
                  secret = Credential "inactive" }
                sampleRequest
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
    })

[<Fact>]
let ``CoreMailbox.actorStop with valid credential drops live row`` () =
    let actorSecret = Credential "actor-live"
    let stopped = ResizeArray<Credential * ActorResult>()
    let live = ResizeArray<Credential>()
    live.Add actorSecret
    let recordingPool: CoreActorPool = {
        register = fun _ _ -> ()
        startActor = fun _ -> Ok ()
        isLive = fun secret -> live.Contains secret
        admit = fun _ -> Ok ()
        drop = fun _ -> ()
        finish =
            fun secret result ->
                live.Remove secret |> ignore
                stopped.Add(secret, result)
                Ok ()
        liveFocusIds = fun () -> Set.empty
    }
    task {
        let dataDir = newTempDir ()
        let credentials = admittedCredentials ()
        let host =
            CoreMailbox.host
                credentials
                recordingPool
                (FileAgent.persist (FileAgent.create dataDir))
        try
            do! credentials.add actorSecret |> Async.StartAsTask
            let! result =
                CoreMailbox.actorStop
                    host
                    (actorCaller actorSecret)
                    ActorSucceeded
                |> Async.StartAsTask
            requireOk "actorStop" result
            Assert.Equal(1, stopped.Count)
            Assert.Equal(actorSecret, fst stopped.[0])
            Assert.False(live.Contains actorSecret)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``CoreMailbox door exposes lifecycle facts via getState`` () =
    withHost (fun host _ _ -> task {
        let! result =
            CoreMailbox.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        let! state =
            CoreMailbox.getState host
            |> Async.StartAsTask
        let state = requireOk "getState" state
        Assert.True(state.graph.nodes.[sampleRequest.focusId].lockPresent)
    })
