module Gambol.Server.Tests.CoreMailboxDoorTests

open System
open System.Reflection
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

/// CoreMailbox door — public API tests using CoreMailboxApi.startActor / actorStop.

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private eventPast host =
    async {
        let! history = CoreMailboxApi.eventHistory host
        return history.events
    }

let private sampleRequest: Gambol.Shared.ActorStart =
    { zoomId = Graph.rootId
      focusId = Graph.rootId
      commandId = Graph.rootId
      graphIds = [ Graph.rootId ]
      eventId = EventId.zero }

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
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``CoreMailboxApi.startActor calls pool.startActor and returns bookkeeping result`` () =
    withHost (fun host pool -> task {
        let! result =
            CoreMailboxApi.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        Assert.True(Set.contains sampleRequest.focusId (pool.liveFocusIds ()))
    })

[<Fact>]
let ``CoreMailboxApi.startActor with inactive secret is refused`` () =
    withHost (fun host _ -> task {
        let! result =
            CoreMailboxApi.startActor
                host
                { authority = testAuthority
                  name = testCaller.name
                  secret = Credential "inactive" }
                sampleRequest
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
    })

[<Fact>]
let ``CoreMailboxApi.login privately admits a Browser secret`` () =
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
            let event = { id = EventId.zero
                          submissionId = Guid.NewGuid()
                          authority = Authority "Browser"
                          commandName = ""
                          body = EventBody.Change
                                          [ Op.NewNode(childId, "after-login")
                                            Op.Replace(
                                                Graph.rootId,
                                                [],
                                                [ ChildNode.owner childId ]) ] }
            let! refused =
                CoreMailboxApi.postEvents host caller [ event ]
                |> Async.StartAsTask
            Assert.Equal(Error CoreAuth.refuse, refused)
            let! before =
                CoreMailboxApi.isAdmitted host caller
                |> Async.StartAsTask
            Assert.False(before)
            let! loggedIn =
                CoreMailboxApi.login host name secret
                |> Async.StartAsTask
            requireOk "login" loggedIn
            let! after =
                CoreMailboxApi.isAdmitted host caller
                |> Async.StartAsTask
            Assert.True(after)
            let! posted =
                CoreMailboxApi.postEvents host caller [ event ]
                |> Async.StartAsTask
            requireOk "post after login" posted |> ignore
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``CoreMailboxApi.login name distinguishes Callers that share a secret`` () =
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
                CoreMailboxApi.login host loggedIn.name secret
                |> Async.StartAsTask
            requireOk "login" logged
            let! admitted =
                CoreMailboxApi.isAdmitted host loggedIn
                |> Async.StartAsTask
            let! refusedName =
                CoreMailboxApi.isAdmitted host otherName
                |> Async.StartAsTask
            Assert.True(admitted)
            Assert.False(refusedName)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``CoreMailboxApi.actorStop with valid credential drops live row`` () =
    let actorSecret = Credential "actor-live"
    let stopped = ResizeArray<Credential * ActorResult>()
    let live = ResizeArray<Credential>()
    live.Add actorSecret
    let recordingPool: CoreActorPool = {
        register = fun _ _ -> ()
        registerPeer = fun _ _ -> ()
        startActor =
            fun _ _ ->
                Ok actorSecret
        startFunction = fun _ -> Error "unused"
        startPeerActor = fun _ _ _ -> Error "unused"
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
        getFocusId = fun _ -> Some Graph.rootId
        trySecretForFocus = fun _ -> None
        deliver = fun _ -> Error "not live"
        takeInbox = fun _ -> Error "not live"
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
                CoreMailboxApi.actorStop
                    host
                    (actorCaller actorSecret)
                    ActorSucceeded
                |> Async.StartAsTask
            requireOk "actorStop" result
            Assert.Equal(1, stopped.Count)
            Assert.Equal(actorSecret, fst stopped.[0])
            Assert.False(live.Contains actorSecret)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``CoreMailbox door exposes Graph lockPresent via getState`` () =
    withHost (fun host _ -> task {
        let! result =
            CoreMailboxApi.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        let! state =
            CoreMailboxApi.getState host
            |> Async.StartAsTask
        let state = requireOk "getState" state
        Assert.True(state.graph.nodes.[sampleRequest.focusId].lockPresent)
    })

[<Fact>]
let ``CoreMailboxApi.startActor appends ActorStart to lifecycle events`` () =
    withHost (fun host _ -> task {
        let! result =
            CoreMailboxApi.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        let! events =
            eventPast host
            |> Async.StartAsTask
        let actorStartedEvents =
            events
            |> List.choose (fun event ->
                match event.body with
                | Gambol.Shared.EventBody.ActorStart request
                    when request.focusId = sampleRequest.focusId ->
                    Some request.focusId
                | _ -> None)
        Assert.Equal(1, actorStartedEvents.Length)
    })

[<Fact>]
let ``CoreMailboxApi.actorStop appends ActorStop and drops live row`` () =
    let actorSecret = Credential "actor-live"
    let live = ResizeArray<Credential>()
    live.Add actorSecret
    let recordingPool: CoreActorPool = {
        register = fun _ _ -> ()
        registerPeer = fun _ _ -> ()
        startActor =
            fun _ _ ->
                Ok actorSecret
        startFunction = fun _ -> Error "unused"
        startPeerActor = fun _ _ _ -> Error "unused"
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
        trySecretForFocus = fun _ -> None
        deliver = fun _ -> Error "not live"
        takeInbox = fun _ -> Error "not live"
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
                CoreMailboxApi.actorStop
                    host
                    (actorCaller actorSecret)
                    ActorSucceeded
                |> Async.StartAsTask
            requireOk "actorStop" stopResult
            Assert.False(live.Contains actorSecret)
            let! events =
                eventPast host
                |> Async.StartAsTask
            let actorFinishedEvents =
                events
                |> List.choose (fun event ->
                    match event.body with
                    | Gambol.Shared.EventBody.ActorStop(focusId, _)
                        when focusId = sampleRequest.focusId ->
                        Some focusId
                    | _ -> None)
            Assert.Equal(1, actorFinishedEvents.Length)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``CoreActorPool.startActor uses client graphIds to build subgraph`` () =
    withHost (fun host pool -> task {
        let childId = NodeId.New()
        let event = { id = EventId.zero
                      submissionId = Guid.NewGuid()
                      authority = Authority "Browser"
                      commandName = ""
                      body = EventBody.Change
                                      [ Op.NewNode(childId, "child")
                                        Op.Replace(Graph.rootId, [],
                                            [ ChildNode.owner childId ]) ] }
        let! postResult =
            CoreMailboxApi.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" postResult |> ignore
        
        let request: Gambol.Shared.ActorStart =
            { zoomId = Graph.rootId
              focusId = Graph.rootId
              commandId = Graph.rootId
              graphIds = [ Graph.rootId; childId ]
              eventId = EventId.zero }
        
        let! result =
            CoreMailboxApi.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        let! history =
            CoreMailboxApi.eventHistory host
            |> Async.StartAsTask
        Assert.Contains(
            history.events,
            fun event ->
                match event.body with
                | Gambol.Shared.EventBody.ActorStart started ->
                    started.commandId = request.commandId
                | _ -> false)
    })

[<Fact>]
let ``CoreActorPool.startActor selects actor from command node text`` () =
    withHost (fun host _ -> task {
        let commandId = NodeId.New()
        let event = { id = EventId.zero
                      submissionId = Guid.NewGuid()
                      authority = Authority "Browser"
                      commandName = ""
                      body = EventBody.Change
                                      [ Op.NewNode(commandId, "test")
                                        Op.Replace(Graph.rootId, [],
                                            [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailboxApi.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" postResult |> ignore
        
        let request: Gambol.Shared.ActorStart =
            { zoomId = Graph.rootId
              focusId = Graph.rootId
              commandId = commandId
              graphIds = [ Graph.rootId; commandId ]
              eventId = EventId.zero }
        
        let! result =
            CoreMailboxApi.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        let! history =
            CoreMailboxApi.eventHistory host
            |> Async.StartAsTask
        Assert.Contains(
            history.events,
            fun event ->
                match event.body with
                | Gambol.Shared.EventBody.ActorStart started ->
                    started.commandId = request.commandId
                | _ -> false)
    })

[<Fact>]
let ``CoreActorPool.startActor fails when graphIds is empty`` () =
    withHost (fun host _ -> task {
        let request: Gambol.Shared.ActorStart =
            { zoomId = Graph.rootId
              focusId = Graph.rootId
              commandId = Graph.rootId
              graphIds = []
              eventId = EventId.zero }
        
        let! result =
            CoreMailboxApi.startActor host testCaller request
            |> Async.StartAsTask
        
        match result with
        | Error msg -> Assert.Contains("graphIds required", msg)
        | Ok _ -> Assert.Fail("expected error for empty graphIds")
    })

[<Fact>]
let ``CoreActorPool.startActor fails when commandId not in graphIds`` () =
    withHost (fun host _ -> task {
        let commandId = NodeId.New()
        let event = { id = EventId.zero
                      submissionId = Guid.NewGuid()
                      authority = Authority "Browser"
                      commandName = ""
                      body = EventBody.Change
                                      [ Op.NewNode(commandId, "test")
                                        Op.Replace(Graph.rootId, [],
                                            [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailboxApi.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" postResult |> ignore
        
        let request: Gambol.Shared.ActorStart =
            { zoomId = Graph.rootId
              focusId = Graph.rootId
              commandId = commandId
              graphIds = [ Graph.rootId ]  // commandId not included
              eventId = EventId.zero }
        
        let! result =
            CoreMailboxApi.startActor host testCaller request
            |> Async.StartAsTask
        
        match result with
        | Error msg -> Assert.Contains("command node not found in provided graphIds", msg)
        | Ok _ -> Assert.Fail("expected error when command not in graphIds")
    })

[<Fact>]
let ``mailbox records ActorStarted before actor body runs`` () =
    withHost (fun host _ -> task {
        let! result =
            CoreMailboxApi.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let! events =
            eventPast host
            |> Async.StartAsTask
        
        let actorStartedEvents =
            events
            |> List.choose (fun event ->
                match event.body with
                | Gambol.Shared.EventBody.ActorStart request
                    when request.focusId = sampleRequest.focusId ->
                    Some request.focusId
                | _ -> None)
        
        Assert.Equal(1, actorStartedEvents.Length)
    })

[<Fact>]
let ``CoreActorPool.startActor creates live row synchronously`` () =
    withHost (fun host pool -> task {
        let! result =
            CoreMailboxApi.startActor host testCaller sampleRequest
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let focusIds = pool.liveFocusIds ()
        Assert.True(Set.contains sampleRequest.focusId focusIds)
    })

[<Fact>]
let ``Graph-only post without admitted Caller is refused`` () =
    withHost (fun host _ -> task {
        let event = { id = EventId.zero
                      submissionId = Guid.NewGuid()
                      authority = Authority "Browser"
                      commandName = ""
                      body = EventBody.Change [ Op.NewNode(NodeId.New(), "nope") ] }
        let! result =
            CoreMailboxApi.postGraphOnly
                host
                { testCaller with secret = Credential "inactive" }
                event
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
    })

[<Fact>]
let ``Graph-only post with admitted Caller reaches persist`` () =
    withHost (fun host _ -> task {
        let childId = NodeId.New()
        let event = { id = EventId.zero
                      submissionId = Guid.NewGuid()
                      authority = Authority "Browser"
                      commandName = ""
                      body = EventBody.Change
                                      [ Op.NewNode(childId, "graph-only")
                                        Op.Replace(Graph.rootId, [],
                                            [ ChildNode.owner childId ]) ] }
        let! result =
            CoreMailboxApi.postGraphOnly host testCaller event
            |> Async.StartAsTask
        let accepted = requireOk "graph-only admitted" result
        Assert.True(EventId.isAccepted accepted.eventId)
    })

[<Fact>]
let ``CoreMailboxApi.logout revokes the Caller at the mailbox`` () =
    withHost (fun host _ -> task {
        let! before =
            CoreMailboxApi.isAdmitted host testCaller |> Async.StartAsTask
        Assert.True(before)
        let! loggedOut =
            CoreMailboxApi.logout host testCaller |> Async.StartAsTask
        requireOk "logout" loggedOut
        let! after =
            CoreMailboxApi.isAdmitted host testCaller |> Async.StartAsTask
        Assert.False(after)
        let! refused =
            CoreMailboxApi.postEvents
                host
                testCaller
                [ { id = EventId.zero
                    submissionId = Guid.NewGuid()
                    authority = Authority "Browser"
                    commandName = ""
                    body = EventBody.Change [ Op.NewNode(NodeId.New(), "after-logout") ] } ]
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, refused)
    })

[<Fact>]
let ``MailboxHost has no public mailbox processor field`` () =
    let leaked =
        typeof<MailboxHost>.GetMembers(
            BindingFlags.Public ||| BindingFlags.Instance)
        |> Array.exists (fun m ->
            m.Name.IndexOf("mailbox", StringComparison.OrdinalIgnoreCase)
            >= 0)
    Assert.False(leaked)

[<Fact>]
let ``CoreMsg is not a public type`` () =
    let found =
        typeof<MailboxHost>.Assembly.GetExportedTypes()
        |> Array.exists (fun t -> t.Name = "CoreMsg")
    Assert.False(found)

let private postedEvent () : Ev =
    let childId = NodeId.New()
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Gambol.Shared.Authority "Browser"
      commandName = "Set text"
      body =
        Gambol.Shared.EventBody.Change
            [ Op.NewNode(childId, "posted")
              Op.Replace(
                  Graph.rootId,
                  [],
                  [ ChildNode.owner childId ]) ] }

[<Fact>]
let ``CoreMailboxApi.postEvent appends an Ev that eventsSince returns`` () =
    withHost (fun host _ -> task {
        let event = postedEvent ()
        let! posted =
            CoreMailboxApi.postEvent host testCaller event
            |> Async.StartAsTask
        let stored = requireOk "postEvent" posted
        let! tail =
            CoreMailboxApi.eventsSince
                host
                EventId.zero
            |> Async.StartAsTask
        Assert.Equal(1, tail.events.Length)
        Assert.Contains(stored, tail.events)
        Assert.Equal(event.submissionId, stored.submissionId)
        let! afterStored =
            CoreMailboxApi.eventsSince host stored.id
            |> Async.StartAsTask
        Assert.Empty(afterStored.events)
    })

[<Fact>]
let ``CoreMailboxApi.postEvent without admitted Caller is refused`` () =
    withHost (fun host _ -> task {
        let! result =
            CoreMailboxApi.postEvent
                host
                { testCaller with secret = Credential "inactive" }
                (postedEvent ())
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
    })

[<Fact>]
let ``ActorStart persist Error leaves Focus out of liveFocusIds`` () =
    let persist = FileAgent.persist (FileAgent.create (newTempDir ()))
    let filling =
        { persist with
            handlers =
                { persist.handlers with
                    appendEvent =
                        fun event ->
                            match event.body with
                            | EventBody.ActorStart _ ->
                                Error "actor start persist failed"
                            | _ -> persist.handlers.appendEvent event } }
    let pool = CoreActorPool.create ()
    pool.register (ActorName "root") (fun _ _ -> async.Return ())
    pool.register (ActorName "test") TestActor.actorFn
    let host =
        CoreMailbox.host pool filling admittedCredentials
    task {
        try
            let! started =
                CoreMailboxApi.startActor host testCaller sampleRequest
                |> Async.StartAsTask
            match started with
            | Ok _ -> Assert.Fail("expected ActorStart persist Error")
            | Error error ->
                Assert.Contains("actor start persist failed", error)
            Assert.False(
                Set.contains sampleRequest.focusId (pool.liveFocusIds ()))
        finally
            CoreMailboxApi.dispose host
    }
