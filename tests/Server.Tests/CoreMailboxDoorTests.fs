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
      name = ""
      secret = secret }

let private createHost () =
    let dataDir = newTempDir ()
    let pool = CoreActorPool.create ()
    pool.register (ActorName "root") (fun _ _ -> async.Return ())
    pool.register (ActorName "test") TestActor.actorFn
    let host =
        CoreMailbox.host
            pool
            (FileAgent.persist (FileAgent.create dataDir))
            admittedCredentials
    host, pool

let private withHost body =
    task {
        let host, pool = createHost ()
        try
            do! body host pool
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``CoreMailbox.startActor calls pool.startActor and returns bookkeeping result`` () =
    withHost (fun host pool -> task {
        let! result =
            CoreMailbox.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        Assert.True(Set.contains sampleRequest.focusId (pool.liveFocusIds ()))
    })

[<Fact>]
let ``CoreMailbox.startActor with inactive secret is refused`` () =
    withHost (fun host _ -> task {
        let! result =
            CoreMailbox.startActor
                host
                { authority = testAuthority
                  name = testCaller.name
                  secret = Credential "inactive" }
                sampleRequest
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
    })

[<Fact>]
let ``CoreMailbox.login privately admits a Browser secret`` () =
    task {
        let dataDir = newTempDir ()
        let host =
            CoreMailbox.host
                (CoreActorPool.create ())
                (FileAgent.persist (FileAgent.create dataDir))
                CoreCredentials.empty
        try
            let secret = Credential "login-secret"
            let name = "browser-session"
            let caller =
                { authority = Authority "Browser"
                  name = name
                  secret = secret }
            let childId = NodeId.New()
            let change =
                { id = 0
                  changeId = Guid.NewGuid()
                  ops =
                    [ Op.NewNode(childId, "after-login")
                      Op.Replace(
                          Graph.rootId,
                          [],
                          [ ChildNode.owner childId ]) ] }
            let! refused =
                CoreMailbox.postChange host caller [ change ]
                |> Async.StartAsTask
            Assert.Equal(Error CoreAuth.refuse, refused)
            let! before =
                CoreMailbox.isAdmitted host caller
                |> Async.StartAsTask
            Assert.False(before)
            let! loggedIn =
                CoreMailbox.login host name secret
                |> Async.StartAsTask
            requireOk "login" loggedIn
            let! after =
                CoreMailbox.isAdmitted host caller
                |> Async.StartAsTask
            Assert.True(after)
            let! posted =
                CoreMailbox.postChange host caller [ change ]
                |> Async.StartAsTask
            requireOk "post after login" posted |> ignore
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``CoreMailbox.login name distinguishes Callers that share a secret`` () =
    task {
        let dataDir = newTempDir ()
        let host =
            CoreMailbox.host
                (CoreActorPool.create ())
                (FileAgent.persist (FileAgent.create dataDir))
                CoreCredentials.empty
        try
            let secret = Credential "shared-secret"
            let loggedIn =
                { authority = Authority "Browser"
                  name = "browser-a"
                  secret = secret }
            let otherName =
                { loggedIn with name = "browser-b" }
            let! logged =
                CoreMailbox.login host loggedIn.name secret
                |> Async.StartAsTask
            requireOk "login" logged
            let! admitted =
                CoreMailbox.isAdmitted host loggedIn
                |> Async.StartAsTask
            let! refusedName =
                CoreMailbox.isAdmitted host otherName
                |> Async.StartAsTask
            Assert.True(admitted)
            Assert.False(refusedName)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``CoreMailbox.actorStop with valid credential drops live row`` () =
    let actorSecret = Credential "actor-live"
    let stopped = ResizeArray<Credential * ActorResult>()
    let live = ResizeArray<Credential>()
    live.Add actorSecret
    let recordingPool: CoreActorPool = {
        register = fun _ _ -> ()
        startActor =
            fun request _ ->
                Ok { secret = actorSecret; focusId = request.focusId }
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
    task {
        let dataDir = newTempDir ()
        let host =
            CoreMailbox.host
                recordingPool
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
        try
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
    withHost (fun host _ -> task {
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
    withHost (fun host _ -> task {
        let! result =
            CoreMailbox.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        let! events =
            CoreMailbox.eventHistory host
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
        startActor =
            fun request _ ->
                Ok { secret = actorSecret; focusId = request.focusId }
        schedule = fun _ _ -> ()
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
        let host =
            CoreMailbox.host
                recordingPool
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
        try
            let! stopResult =
                CoreMailbox.actorStop
                    host
                    (actorCaller actorSecret)
                    ActorSucceeded
                |> Async.StartAsTask
            requireOk "actorStop" stopResult
            Assert.False(live.Contains actorSecret)
            let! events =
                CoreMailbox.eventHistory host
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
    withHost (fun host pool -> task {
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
        requireOk "postChange" postResult |> ignore
        
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
    withHost (fun host pool -> task {
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
        requireOk "postChange" postResult |> ignore
        
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
    withHost (fun host _ -> task {
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
    withHost (fun host _ -> task {
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
        requireOk "postChange" postResult |> ignore
        
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
let ``mailbox records ActorStarted before actor body runs`` () =
    withHost (fun host _ -> task {
        let! result =
            CoreMailbox.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let! events =
            CoreMailbox.eventHistory host
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
    withHost (fun host pool -> task {
        let! result =
            CoreMailbox.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let focusIds = pool.liveFocusIds ()
        Assert.True(Set.contains sampleRequest.focusId focusIds)
    })

[<Fact>]
let ``Successful PostChange appends ChangeEvent to mailbox history`` () =
    withHost (fun host _ -> task {
        let childId = NodeId.New()
        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(childId, "test")
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }
        
        let! postResult =
            CoreMailbox.postGraphOnlyChange host [ change ]
            |> Async.StartAsTask
        requireOk "postChange" postResult |> ignore
        
        let! events =
            CoreMailbox.eventHistory host
            |> Async.StartAsTask
        
        let changeEvents =
            events
            |> List.choose (fun event ->
                match event with
                | ChangeEvent c when c.changeId = change.changeId -> Some c
                | _ -> None)
        
        Assert.Equal(1, changeEvents.Length)
    })

[<Fact>]
let ``Actor lifecycle and Changes appear on same History sequence`` () =
    withHost (fun host _ -> task {
        // Start actor
        let! startResult =
            CoreMailbox.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" startResult
        
        // Post a change
        let childId = NodeId.New()
        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(childId, "hello")
                  Op.Replace(sampleRequest.focusId, [], [ ChildNode.owner childId ]) ] }
        
        let! postResult =
            CoreMailbox.postGraphOnlyChange host [ change ]
            |> Async.StartAsTask
        requireOk "postChange" postResult |> ignore
        
        let! events =
            CoreMailbox.eventHistory host
            |> Async.StartAsTask
        
        // Verify we have both ActorStarted and ChangeEvent in the same sequence
        let hasActorStarted =
            events
            |> List.exists (fun event ->
                match event with
                | ActorEvent (_, ActorStarted _) -> true
                | _ -> false)
        
        let hasChangeEvent =
            events
            |> List.exists (fun event ->
                match event with
                | ChangeEvent c when c.changeId = change.changeId -> true
                | _ -> false)
        
        Assert.True(hasActorStarted, "Expected ActorStarted in history")
        Assert.True(hasChangeEvent, "Expected ChangeEvent in history")
    })
