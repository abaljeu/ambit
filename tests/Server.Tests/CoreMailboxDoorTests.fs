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
let ``CoreMailboxApi.startActor from an inactive Actor is refused`` () =
    withHost (fun host _ -> task {
        let! result =
            CoreMailboxApi.startActor
                host
                { authority = Authority "Actor"
                  name = testCaller.name
                  secret = Credential "inactive" }
                sampleRequest
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
    })

[<Fact>]
let ``Actor start rejects a secret the pool does not admit`` () =
    let actorSecret = Credential "dead-at-start"
    let basePool = CoreActorPool.create ()
    let pool =
        { basePool with
            startActor = fun _ _ -> Ok actorSecret
            admit = fun _ -> Error CoreAuth.refuse }
    task {
        let dataDir = newTempDir ()
        let host =
            CoreMailbox.host
                pool
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
        try
            let! result =
                CoreMailboxApi.startActor host testCaller sampleRequest
                |> Async.StartAsTask
            Assert.Equal(Error CoreAuth.refuse, result)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``Actor start admits the secret and a later post skips the pool live check`` () =
    let actorSecret = Credential "live-at-start"
    let admitCount = ref 0
    let basePool = CoreActorPool.create ()
    let pool =
        { basePool with
            startActor = fun _ _ -> Ok actorSecret
            isLive = fun _ -> false
            admit =
                fun secret ->
                    Assert.Equal(actorSecret, secret)
                    admitCount.Value <- admitCount.Value + 1
                    Ok () }
    task {
        let dataDir = newTempDir ()
        let host =
            CoreMailbox.host
                pool
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
        try
            let! started =
                CoreMailboxApi.startActor host testCaller sampleRequest
                |> Async.StartAsTask
            requireOk "startActor" started
            let caller = actorCaller actorSecret
            let childId = NodeId.New()
            let event =
                { id = EventId.zero
                  submissionId = Guid.NewGuid()
                  authority = Authority "Wire"
                  commandName = "Actor post"
                  body = EventBody.Change [ Op.NewNode(childId, "Actor post") ] }
            let! posted =
                CoreMailboxApi.postEvent host caller event
                |> Async.StartAsTask
            let stored = requireOk "Actor post" posted
            Assert.Equal(1, admitCount.Value)
            Assert.Equal(Authority "Actor", stored.authority)
        finally
            CoreMailboxApi.dispose host
    }

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
let ``Graph-only post reads authority without repeated admission`` () =
    withHost (fun host _ -> task {
        let event = { id = EventId.zero
                      submissionId = Guid.NewGuid()
                      authority = Authority "Wire"
                      commandName = ""
                      body = EventBody.Change [ Op.NewNode(NodeId.New(), "nope") ] }
        let caller =
            { testCaller with
                authority = Authority "Direct"
                secret = Credential "inactive" }
        let! result =
            CoreMailboxApi.postGraphOnly
                host
                caller
                event
            |> Async.StartAsTask
        requireOk "graph-only direct" result |> ignore
        let! history =
            CoreMailboxApi.eventHistory host |> Async.StartAsTask
        Assert.Equal(Authority "Direct", history.events.Head.authority)
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
let ``CoreMailboxApi.logout rejects a later order once authority is dropped`` () =
    withHost (fun host _ -> task {
        let! before =
            CoreMailboxApi.isAdmitted host testCaller |> Async.StartAsTask
        Assert.True(before)
        do! CoreMailboxApi.logout host testCaller |> Async.StartAsTask
        let! after =
            CoreMailboxApi.isAdmitted host testCaller |> Async.StartAsTask
        Assert.False(after)
        let! posted =
            CoreMailboxApi.postEvents
                host
                testCaller
                [ { id = EventId.zero
                    submissionId = Guid.NewGuid()
                    authority = Authority "Browser"
                    commandName = ""
                    body = EventBody.Change [ Op.NewNode(NodeId.New(), "after-logout") ] } ]
            |> Async.StartAsTask
        Assert.Equal(Error "authority dropped", posted)
        Session.login (MailboxHost.session host) testCaller
        |> requireOk "relogin"
        let! again =
            CoreMailboxApi.postEvents
                host
                testCaller
                [ { id = EventId.zero
                    submissionId = Guid.NewGuid()
                    authority = Authority "Browser"
                    commandName = ""
                    body = EventBody.Change [ Op.NewNode(NodeId.New(), "after-login") ] } ]
            |> Async.StartAsTask
        requireOk "post after login" again |> ignore
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
            waitForNotices
                (fun () ->
                    CoreMailboxApi.getEventsSince host EventId.zero
                    |> Async.StartAsTask)
                (fun events ->
                    let saw pred =
                        events
                        |> List.exists (fun event ->
                            match event.body with
                            | EventBody.Notice notice -> pred notice
                            | _ -> false)
                    saw (function
                        | Notice.NodePersisted _ -> true
                        | _ -> false))
        let ordered = List.rev tail
        Assert.Equal(3, ordered.Length)
        Assert.Contains(stored, tail)
        Assert.Equal(event.submissionId, stored.submissionId)
        match ordered |> List.map (fun event -> event.body) with
        | [ EventBody.Change _
            EventBody.Notice (Notice.NodePersisting persistingId)
            EventBody.Notice (Notice.NodePersisted (persistedId, _, _)) ] ->
            Assert.Equal(persistingId, persistedId)
        | other ->
            Assert.Fail(
                $"expected Change, NodePersisting, NodePersisted, got {other}")
        let! afterStored =
            CoreMailboxApi.eventsSince host stored.id
            |> Async.StartAsTask
        match
            afterStored.events
            |> List.rev
            |> List.map (fun event -> event.body)
        with
        | [ EventBody.Notice (Notice.NodePersisting persistingId)
            EventBody.Notice (Notice.NodePersisted (persistedId, _, _)) ] ->
            Assert.Equal(persistingId, persistedId)
        | other ->
            Assert.Fail(
                $"expected NodePersisting then NodePersisted, got {other}")
    })

[<Fact>]
let ``CoreMailboxApi.postEvent stores Caller authority without admission`` () =
    withHost (fun host _ -> task {
        let caller =
            { testCaller with
                authority = Authority "Direct"
                secret = Credential "inactive" }
        let! result =
            CoreMailboxApi.postEvent
                host
                caller
                (postedEvent ())
            |> Async.StartAsTask
        let stored = requireOk "direct post" result
        Assert.Equal(Authority "Direct", stored.authority)
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

let private fileWithText () =
    let fileId = NodeId.New()
    let node =
        Node.Create(
            fileId,
            text = "hello",
            name = Filename.create "note.txt",
            kind = Special File)
    let state =
        { graph = Graph.addDetachedNode node (Graph.create ())
          eventId = EventId.zero }
    fileId, state

let private editText (nodeId: NodeId) oldText newText : Ev =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = "Edit"
      body = EventBody.Change [ Op.SetText(nodeId, oldText, newText) ] }

let private editFileText (fileId: NodeId) : Ev =
    editText fileId "hello" "edited"

let private hostWithHandlers (handlers: PersistHandlers) =
    CoreMailbox.host
        (CoreActorPool.create ())
        { handlers = handlers
          onError = fun _ _ _ -> ()
          formatError = id
          isReady = fun () -> true
          dispose = ignore
          until = None
          bindPersistFinished = ignore }
        admittedCredentials

let private acceptPosted (event: Ev) (_graphOnly: bool) =
    Ok(CoreChanges.accepted event.id true [ event ] false)

let private hostRecordingPush
    (state: State)
    (pushed: ResizeArray<NodeId>)
    =
    let handlers =
        { cannedHandlers (fun () -> Ok state) (fun _ -> []) acceptPosted with
            pushPersist = pushed.Add }
    hostWithHandlers handlers

let private hostRecordingGraph (state: State ref) =
    let handlers =
        { cannedHandlers
            (fun () -> Ok state.Value)
            (fun _ -> [])
            acceptPosted with
            replaceGraph =
                fun _notice graph ->
                    state.Value <- { state.Value with graph = graph } }
    hostWithHandlers handlers

[<Fact>]
let ``An Edit pushes its owning special node`` () =
    task {
        let fileId, state = fileWithText ()
        let pushed = ResizeArray<NodeId>()
        let host = hostRecordingPush state pushed
        try
            let! posted =
                CoreMailboxApi.postEvents
                    host
                    testCaller
                    [ editFileText fileId ]
                |> Async.StartAsTask
            requireOk "edit" posted |> ignore
            Assert.Equal<NodeId list>([ fileId ], List.ofSeq pushed)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``An Edit marks its owning special node at the Edit's id`` () =
    task {
        let fileId, initial = fileWithText ()
        let state = ref initial
        let host = hostRecordingGraph state
        try
            let! first =
                CoreMailboxApi.postEvents
                    host
                    testCaller
                    [ editFileText fileId ]
                |> Async.StartAsTask
            requireOk "first edit" first |> ignore
            let! second =
                CoreMailboxApi.postEvents
                    host
                    testCaller
                    [ editFileText fileId ]
                |> Async.StartAsTask
            let accepted = requireOk "second edit" second
            let! live =
                CoreMailboxApi.getState host |> Async.StartAsTask
            let live = requireOk "state" live
            let node = live.graph.nodes.[fileId]
            Assert.Equal(
                PersistState.Unpersisted,
                node.documentState.persistState)
            Assert.Equal(
                accepted.eventId,
                node.documentState.changedVersion)
        finally
            CoreMailboxApi.dispose host
    }

let private hostRecordingNotices
    (state: State ref)
    (notices: ResizeArray<Notice>)
    =
    let handlers =
        { cannedHandlers
            (fun () -> Ok state.Value)
            (fun _ -> [])
            acceptPosted with
            replaceGraph =
                fun notice graph ->
                    notices.Add notice
                    state.Value <-
                        { state.Value with graph = graph } }
    hostWithHandlers handlers

[<Fact>]
let ``an edit logs NodeUnpersisted for its owning special`` () =
    task {
        let fileId, initial = fileWithText ()
        let state = ref initial
        let host = hostRecordingGraph state
        try
            let! posted =
                CoreMailboxApi.postEvents
                    host
                    testCaller
                    [ editFileText fileId ]
                |> Async.StartAsTask
            requireOk "edit" posted |> ignore
            let! since =
                CoreMailboxApi.getEventsSince host EventId.zero
                |> Async.StartAsTask
            let ordered = List.rev since
            let changeAt =
                ordered
                |> List.findIndex (fun event ->
                    match event.body with
                    | EventBody.Change _ -> true
                    | _ -> false)
            let noticeAt =
                ordered
                |> List.findIndex (fun event ->
                    match event.body with
                    | EventBody.Notice (Notice.NodeUnpersisted id) ->
                        id = fileId
                    | _ -> false)
            Assert.True(noticeAt > changeAt)
            let loggedPersisting =
                ordered
                |> List.exists (fun event ->
                    match event.body with
                    | EventBody.Notice (Notice.NodePersisting _) -> true
                    | _ -> false)
            Assert.False(loggedPersisting)
        finally
            CoreMailboxApi.dispose host
    }

let private applyOrdinaryEdit (state: State ref) (event: Ev) (_graphOnly: bool) =
    match Ev.ops event with
    | None -> Error "Event has no Ops"
    | Some ops ->
        let step graph op =
            match op with
            | Op.SetText(nodeId, oldText, newText) ->
                GraphMutate.setText None nodeId oldText newText graph
            | _ -> Error "not an ordinary edit"
        let applied =
            ops
            |> List.fold
                (fun acc op ->
                    acc |> Result.bind (fun graph -> step graph op))
                (Ok state.Value.graph)
        match applied with
        | Error err -> Error err
        | Ok graph ->
            state.Value <- { state.Value with graph = graph }
            Ok(CoreChanges.accepted event.id true [ event ] false)

let private runOrdinaryEdit (initial: State) (event: Ev) =
    let state = ref initial
    let handlers =
        { cannedHandlers
            (fun () -> Ok state.Value)
            (fun _ -> [])
            (applyOrdinaryEdit state) with
            replaceGraph =
                fun _notice graph ->
                    state.Value <- { state.Value with graph = graph } }
    let context: CoreEventDispatch.Context = {
        persist = handlers
        eventLog = ref EventLog.empty
        restartTip = EventId.zero }
    match CoreEventDispatch.postEvent context testCaller event false with
    | Error err -> failwith err
    | Ok _ -> List.rev context.eventLog.Value.events, state.Value

let private changeBeforeUnpersisted (ordered: Ev list) nodeId =
    let changeAt =
        ordered
        |> List.findIndex (fun stored ->
            match stored.body with
            | EventBody.Change _ -> true
            | _ -> false)
    let noticeAt =
        ordered
        |> List.findIndex (fun stored ->
            match stored.body with
            | EventBody.Notice (Notice.NodeUnpersisted id) -> id = nodeId
            | _ -> false)
    Assert.True(noticeAt > changeAt)

let private hasUnpersisted (ordered: Ev list) nodeId =
    ordered
    |> List.exists (fun stored ->
        match stored.body with
        | EventBody.Notice (Notice.NodeUnpersisted id) -> id = nodeId
        | _ -> false)

let private persistOf (state: State) nodeId =
    state.graph.nodes.[nodeId].documentState.persistState

let private namedSpecial id kind name =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        kind = Special kind)

let private attachOwner parentId childId graph =
    match Graph.replace parentId 0 [] [ ChildNode.owner childId ] graph with
    | Ok next -> next
    | Error err -> failwith err

/// Workspace, a directory, a directory under that, then a file and its body.
let private nestedUnderDirectory () =
    let wsId = NodeId.New()
    let outerId = NodeId.New()
    let innerId = NodeId.New()
    let fileId = NodeId.New()
    let bodyId = NodeId.New()
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode (namedSpecial wsId Workspace "home")
        |> Graph.addDetachedNode (namedSpecial outerId Directory "outer")
        |> Graph.addDetachedNode (namedSpecial innerId Directory "inner")
        |> Graph.addDetachedNode (namedSpecial fileId File "note.txt")
        |> Graph.addDetachedNode (Node.Create(bodyId, text = "hello"))
        |> attachOwner Graph.workspacesId wsId
        |> attachOwner wsId outerId
        |> attachOwner outerId innerId
        |> attachOwner innerId fileId
        |> attachOwner fileId bodyId
    let nodes =
        graph.nodes
        |> Map.map (fun _ node ->
            Node.withPersistState PersistState.Persisted node)
    wsId, outerId, innerId, fileId, bodyId,
    { graph = { graph with nodes = nodes }
      eventId = EventId.zero }

[<Fact>]
let ``ordinary edit posts NodeUnpersisted`` () =
    let fileId, initial = fileWithText ()
    let ordered, after = runOrdinaryEdit initial (editFileText fileId)
    changeBeforeUnpersisted ordered fileId
    let node = after.graph.nodes.[fileId]
    Assert.Equal("edited", node.text)
    Assert.Equal(PersistState.Unpersisted, node.documentState.persistState)

    let wsId, outerId, innerId, nestedFileId, bodyId, nested =
        nestedUnderDirectory ()
    let nestedOrdered, nestedAfter =
        runOrdinaryEdit nested (editText bodyId "hello" "edited")
    changeBeforeUnpersisted nestedOrdered nestedFileId
    Assert.False(hasUnpersisted nestedOrdered wsId)
    Assert.False(hasUnpersisted nestedOrdered outerId)
    Assert.False(hasUnpersisted nestedOrdered innerId)
    Assert.Equal("edited", nestedAfter.graph.nodes.[bodyId].text)
    Assert.Equal(PersistState.Unpersisted, persistOf nestedAfter nestedFileId)
    Assert.Equal(PersistState.Persisted, persistOf nestedAfter wsId)
    Assert.Equal(PersistState.Persisted, persistOf nestedAfter outerId)
    Assert.Equal(PersistState.Persisted, persistOf nestedAfter innerId)

[<Fact>]
let ``a graph-only change logs no NodeUnpersisted`` () =
    task {
        let fileId, state = fileWithText ()
        let notices = ResizeArray<Notice>()
        let host = hostRecordingNotices (ref state) notices
        try
            let! posted =
                CoreMailboxApi.postGraphOnly
                    host
                    testCaller
                    (editFileText fileId)
                |> Async.StartAsTask
            requireOk "graph-only edit" posted |> ignore
            Assert.Empty(notices)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``A graph-only change pushes nothing`` () =
    task {
        let fileId, state = fileWithText ()
        let pushed = ResizeArray<NodeId>()
        let host = hostRecordingPush state pushed
        try
            let! posted =
                CoreMailboxApi.postGraphOnly
                    host
                    testCaller
                    (editFileText fileId)
                |> Async.StartAsTask
            requireOk "graph-only edit" posted |> ignore
            Assert.Empty(pushed)
        finally
            CoreMailboxApi.dispose host
    }

let private storedChange host submissionId =
    async {
        let! history = CoreMailboxApi.eventHistory host
        return
            history.events
            |> List.find (fun event ->
                event.submissionId = submissionId)
    }

let rec private storedForPosted host posted =
    match posted with
    | [] -> async.Return []
    | event :: rest ->
        async {
            let! stored = storedChange host event.submissionId
            let! more = storedForPosted host rest
            return stored :: more
        }

let private assertAckFromLog
    host
    posted
    (accepted: CoreChangesAccepted)
    =
    async {
        let! stored = storedForPosted host posted
        let lastId = (List.last stored).id
        Assert.Equal(lastId, accepted.eventId)
        Assert.Equal<Ev list>(stored, accepted.events)
        let expectedExt =
            List.zip posted stored
            |> List.exists (fun (p, s) -> Ev.ops p <> Ev.ops s)
        Assert.Equal(expectedExt, accepted.externalChanges)
    }

[<Fact>]
let ``postEvents returns the same CoreChangesAccepted from the event id`` () =
    task {
        do! withHost (fun host _ ->
            task {
                let plain = postedEvent ()
                let! plainAck =
                    CoreMailboxApi.postEvents host testCaller [ plain ]
                    |> Async.StartAsTask
                let plainAck = requireOk "plain" plainAck
                do!
                    assertAckFromLog host [ plain ] plainAck
                    |> Async.StartAsTask
                Assert.False(plainAck.externalChanges)
            })
        do! withHost (fun host _ ->
            task {
                let childId, create = addRootChildEvent "x0"
                let! created =
                    CoreMailboxApi.postEvents host testCaller [ create ]
                    |> Async.StartAsTask
                requireOk "create" created |> ignore
                let firstEdit =
                    changeEvent
                        "Edit"
                        EventId.zero
                        (Guid.NewGuid())
                        [ Op.SetText(childId, "x0", "xA") ]
                let! edited =
                    CoreMailboxApi.postEvents host testCaller [ firstEdit ]
                    |> Async.StartAsTask
                requireOk "first edit" edited |> ignore
                let stale =
                    changeEvent
                        "Edit"
                        EventId.zero
                        (Guid.NewGuid())
                        [ Op.SetText(childId, "x0", "xB") ]
                let! amendedAck =
                    CoreMailboxApi.postEvents host testCaller [ stale ]
                    |> Async.StartAsTask
                let amendedAck = requireOk "amended" amendedAck
                do!
                    assertAckFromLog host [ stale ] amendedAck
                    |> Async.StartAsTask
                Assert.True(amendedAck.externalChanges)
            })
        do! withHost (fun host _ ->
            task {
                let childA, eventA = addRootChildEvent "one"
                let childB = NodeId.New()
                let eventB =
                    changeEvent
                        ""
                        EventId.zero
                        (Guid.NewGuid())
                        [ Op.NewNode(childB, "two")
                          Op.Replace(
                              Graph.rootId,
                              [ ChildNode.owner childA ],
                              [ ChildNode.owner childA
                                ChildNode.owner childB ]) ]
                let batch = [ eventA; eventB ]
                let! batchAck =
                    CoreMailboxApi.postEvents host testCaller batch
                    |> Async.StartAsTask
                let batchAck = requireOk "batch" batchAck
                do!
                    assertAckFromLog host batch batchAck
                    |> Async.StartAsTask
                Assert.Equal(2, batchAck.events.Length)
                Assert.False(batchAck.externalChanges)
            })
    }

let private postedBody commandName body : Ev = {
    id = EventId.zero
    submissionId = Guid.NewGuid()
    authority = Authority "Browser"
    commandName = commandName
    body = body }

let private storedBody host submissionId = async {
    let! since = CoreMailboxApi.getEventsSince host EventId.zero
    return
        since
        |> List.tryFind (fun event -> event.submissionId = submissionId)
        |> Option.map (fun event -> event.body) }

[<Fact>]
let ``postEvents places Order.Change with origin Edit`` () =
    withHost (fun host _ -> task {
        let event = postedEvent ()
        let! posted =
            CoreMailboxApi.postEvents host testCaller [ event ]
            |> Async.StartAsTask
        let accepted = requireOk "place edit" posted
        let! since =
            CoreMailboxApi.getEventsSince host EventId.zero
            |> Async.StartAsTask
        let stored =
            since
            |> List.find (fun item ->
                item.submissionId = event.submissionId)
        Assert.Equal(stored.id, accepted.eventId)
        Assert.True(EventId.isAccepted accepted.eventId)
        match MailboxLog.ofEv stored with
        | Error err -> Assert.Fail(err)
        | Ok logged ->
            match logged.body with
            | MailboxLog.MailboxEventBody.Order placed ->
                match stored.body with
                | EventBody.Change ops ->
                    Assert.Equal(
                        Order.Change(ops, ChangeOrigin.Edit),
                        placed.order)
                | _ -> Assert.Fail("stored body is a Change")
            | MailboxLog.MailboxEventBody.Notice _ ->
                Assert.Fail("stored body is an Order")
    })

[<Fact>]
let ``Undo, Redo, ActorStart, and Cancel still post as PostEvent`` () =
    withHost (fun host _ -> task {
        let start =
            postedBody "Run" (EventBody.ActorStart sampleRequest)
        let! started =
            CoreMailboxApi.postEvents host testCaller [ start ]
            |> Async.StartAsTask
        requireOk "actor start" started |> ignore
        let! startedBody = storedBody host start.submissionId
        Assert.Equal(
            Some (EventBody.ActorStart sampleRequest),
            startedBody)
        let _, create = addRootChildEvent "undo-target"
        let! created =
            CoreMailboxApi.postEvents host testCaller [ create ]
            |> Async.StartAsTask
        let createdAck = requireOk "create" created
        let undo =
            postedBody "Undo" (EventBody.Undo(createdAck.eventId, []))
        let! undone =
            CoreMailboxApi.postEvents host testCaller [ undo ]
            |> Async.StartAsTask
        let undoneAck = requireOk "undo" undone
        let! undoBody = storedBody host undo.submissionId
        match undoBody with
        | Some (EventBody.Undo (target, _)) ->
            Assert.Equal(createdAck.eventId, target)
        | other -> Assert.Fail($"stored body is Undo, got {other}")
        let redo =
            postedBody "Redo" (EventBody.Redo(undoneAck.eventId, []))
        let! redone =
            CoreMailboxApi.postEvents host testCaller [ redo ]
            |> Async.StartAsTask
        requireOk "redo" redone |> ignore
        let! redoBody = storedBody host redo.submissionId
        match redoBody with
        | Some (EventBody.Redo (target, _)) ->
            Assert.Equal(undoneAck.eventId, target)
        | other -> Assert.Fail($"stored body is Redo, got {other}")
        let cancel =
            postedBody "Cancel" (EventBody.Cancel (NodeId.New()))
        let! cancelled =
            CoreMailboxApi.postEvents host testCaller [ cancel ]
            |> Async.StartAsTask
        let cancelAck = requireOk "cancel" cancelled
        Assert.Equal(EventId.zero, cancelAck.eventId)
        Assert.Empty(cancelAck.events)
        let! cancelBody = storedBody host cancel.submissionId
        Assert.Equal(None, cancelBody)
    })

[<Fact>]
let ``a resubmitted Change replies the stored id`` () =
    withHost (fun host _ -> task {
        let event = postedEvent ()
        let! first =
            CoreMailboxApi.postEvents host testCaller [ event ]
            |> Async.StartAsTask
        let first = requireOk "first" first
        let! second =
            CoreMailboxApi.postEvents host testCaller [ event ]
            |> Async.StartAsTask
        let second = requireOk "resubmit" second
        Assert.Equal(first.eventId, second.eventId)
        Assert.Equal<Ev list>(first.events, second.events)
        Assert.False(second.externalChanges)
        do! assertAckFromLog host [ event ] second |> Async.StartAsTask
    })
