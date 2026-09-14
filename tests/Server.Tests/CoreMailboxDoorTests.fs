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
        startActor = fun _ _ _ _ -> Ok ()
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
let ``CoreMailbox door exposes Graph lockPresent via getState`` () =
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

[<Fact>]
let ``CoreMailbox.startActor appends ActorStarted to lifecycle events`` () =
    withHost (fun host _ _ -> task {
        let! result =
            CoreMailbox.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        let! events =
            CoreMailbox.lifecycleEvents host
            |> Async.StartAsTask
        let actorStartedEvents =
            events
            |> List.choose (fun event ->
                match event with
                | ActorEvent (_, ActorStarted (focusId, _)) when focusId = sampleRequest.focusId ->
                    Some focusId
                | _ -> None)
        Assert.Equal(1, actorStartedEvents.Length)
    })

[<Fact>]
let ``CoreMailbox.actorStop appends ActorFinished and drops live row`` () =
    let actorSecret = Credential "actor-live"
    let live = ResizeArray<Credential>()
    live.Add actorSecret
    let recordingPool: CoreActorPool = {
        register = fun _ _ -> ()
        startActor = fun _ _ _ _ -> Ok ()
        isLive = fun secret -> live.Contains secret
        admit = fun _ -> Ok ()
        drop = fun _ -> ()
        finish =
            fun secret result ->
                live.Remove secret |> ignore
                Ok ()
        liveFocusIds = fun () -> Set.empty
        getFocusId = fun _ -> Some sampleRequest.focusId
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
            let! stopResult =
                CoreMailbox.actorStop
                    host
                    (actorCaller actorSecret)
                    ActorSucceeded
                |> Async.StartAsTask
            requireOk "actorStop" stopResult
            Assert.False(live.Contains actorSecret)
            let! events =
                CoreMailbox.lifecycleEvents host
                |> Async.StartAsTask
            let actorFinishedEvents =
                events
                |> List.choose (fun event ->
                    match event with
                    | ActorEvent (_, ActorFinished focusId) when focusId = sampleRequest.focusId ->
                        Some focusId
                    | _ -> None)
            Assert.Equal(1, actorFinishedEvents.Length)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``CoreActorPool.startActor uses client graphIds to build subgraph`` () =
    withHost (fun host credentials pool -> task {
        let childId = NodeId.New()
        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(childId, "child")
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnlyChange host [ change ]
            |> Async.StartAsTask
        requireOk "postChange" postResult
        
        let request =
            { zoomId = Graph.rootId
              focusId = Graph.rootId
              commandId = Graph.rootId
              graphIds = [ Graph.rootId; childId ]
              revision = Revision 0 }
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        
        Assert.True(Set.contains request.focusId (pool.liveFocusIds ()))
    })

[<Fact>]
let ``CoreActorPool.startActor selects actor from command node text`` () =
    withHost (fun host credentials pool -> task {
        let commandId = NodeId.New()
        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(commandId, "test")
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnlyChange host [ change ]
            |> Async.StartAsTask
        requireOk "postChange" postResult
        
        let request =
            { zoomId = Graph.rootId
              focusId = Graph.rootId
              commandId = commandId
              graphIds = [ Graph.rootId; commandId ]
              revision = Revision 0 }
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        
        Assert.True(Set.contains request.focusId (pool.liveFocusIds ()))
    })

[<Fact>]
let ``CoreActorPool.startActor fails when graphIds is empty`` () =
    withHost (fun host _ _ -> task {
        let request =
            { zoomId = Graph.rootId
              focusId = Graph.rootId
              commandId = Graph.rootId
              graphIds = []
              revision = Revision 0 }
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        
        match result with
        | Error msg -> Assert.Contains("graphIds required", msg)
        | Ok _ -> Assert.Fail("expected error for empty graphIds")
    })

[<Fact>]
let ``CoreActorPool.startActor fails when commandId not in graphIds`` () =
    withHost (fun host _ _ -> task {
        let commandId = NodeId.New()
        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(commandId, "test")
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnlyChange host [ change ]
            |> Async.StartAsTask
        requireOk "postChange" postResult
        
        let request =
            { zoomId = Graph.rootId
              focusId = Graph.rootId
              commandId = commandId
              graphIds = [ Graph.rootId ]  // commandId not included
              revision = Revision 0 }
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        
        match result with
        | Error msg -> Assert.Contains("command node not found in provided graphIds", msg)
        | Ok _ -> Assert.Fail("expected error when command not in graphIds")
    })

[<Fact>]
let ``CoreActorPool.startActor appends ActorStarted before actor body runs`` () =
    withHost (fun host _ _ -> task {
        let! result =
            CoreMailbox.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let! events =
            CoreMailbox.lifecycleEvents host
            |> Async.StartAsTask
        
        let actorStartedEvents =
            events
            |> List.choose (fun event ->
                match event with
                | ActorEvent (_, ActorStarted (focusId, _)) when focusId = sampleRequest.focusId ->
                    Some focusId
                | _ -> None)
        
        Assert.Equal(1, actorStartedEvents.Length)
    })

[<Fact>]
let ``CoreActorPool.startActor creates live row synchronously`` () =
    withHost (fun host _ pool -> task {
        let! result =
            CoreMailbox.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let focusIds = pool.liveFocusIds ()
        Assert.True(Set.contains sampleRequest.focusId focusIds)
    })
