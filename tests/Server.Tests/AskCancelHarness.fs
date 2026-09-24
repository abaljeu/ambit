module Gambol.Server.Tests.AskCancelHarness

open System
open System.Threading
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.CloudAgents
open Gambol.Server.Tests.TestBackend

type SeededAsk =
    { zoomId: NodeId
      commandId: NodeId
      graphIds: NodeId list }

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private eventPast host =
    async {
        let! history = CoreMailbox.eventHistory host
        return history.events
    }

let rec private waitUntil remainingMs (check: unit -> Task<bool>) =
    task {
        let! ok = check ()
        if ok then
            return true
        elif remainingMs <= 0 then
            return false
        else
            do! Task.Delay 10
            return! waitUntil (remainingMs - 10) check
    }

let private actorStops host focusId =
    task {
        let! events = eventPast host |> Async.StartAsTask
        return
            events
            |> List.choose (fun event ->
                match event.body with
                | EventBody.ActorStop(fid, result) when fid = focusId ->
                    Some result
                | _ -> None)
    }

let private createHost keys repos =
    let dataDir = newTempDir ()
    let pool = CoreActorPool.create ()
    pool.register (ActorName "test") TestActor.actorFn
    pool.register (ActorName "ai") (RunAgentActor.actorFn keys repos)
    let host =
        CoreMailbox.host
            pool
            (FileAgent.persist (FileAgent.create dataDir))
            admittedCredentials
    host, pool

let private clearFake () =
    let deadline = DateTime.UtcNow.AddSeconds 2.0
    let rec spin () =
        if AgentRunner.setFake None then
            true
        elif DateTime.UtcNow > deadline then
            false
        else
            Thread.Sleep 10
            spin ()
    Assert.True(spin ())

let private graphState host =
    task {
        let! state = CoreMailbox.getState host |> Async.StartAsTask
        return requireOk "getState" state
    }

let private sampleRequest zoomId focusId commandId graphIds : ActorStart =
    { zoomId = zoomId
      focusId = focusId
      commandId = commandId
      graphIds = graphIds
      eventId = EventId.zero }

let private actorCaller secret : Caller =
    { authority = Authority "Actor"
      name = ""
      secret = secret }

let private replaceFocusChild focusId text =
    let childId = NodeId.New()
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Actor"
      commandName = ""
      body =
        EventBody.Change
            [ Op.NewNode(childId, text)
              Op.Replace(
                  focusId,
                  [],
                  [ ChildNode.owner childId ]) ] }

let waitForActorStop host focusId timeoutMs =
    task {
        let! found =
            waitUntil timeoutMs (fun () -> task {
                let! stops = actorStops host focusId
                return not stops.IsEmpty
            })
        if found then
            let! stops = actorStops host focusId
            return List.tryHead stops
        else
            return None
    }

let private expectActorStop expected host focusId =
    task {
        let! stop = waitForActorStop host focusId 2000
        match stop with
        | Some result when result = expected -> ()
        | other ->
            Assert.Fail($"expected {expected}, got {other}")
    }

let private expectLiveGone pool focusId =
    Assert.False(Set.contains focusId (pool.liveFocusIds ()))

let fakeReply text =
    Finished
        { AgentResult.Text = text
          Git = [] }

let fakeFailed message = Failed message

let withHostKeysRepos keys repos body =
    task {
        let host, pool = createHost keys repos
        try
            do! body host pool
        finally
            CoreMailbox.dispose host
    }

let withHostKeys keys body = withHostKeysRepos keys [] body

let withHost body = withHostKeys [] body

let withFake handler body =
    task {
        Assert.True(AgentRunner.setFake (Some handler))
        try
            do! body ()
        finally
            clearFake ()
    }

let withFakeStream statusHandler streamHandler body =
    withFake statusHandler (fun () ->
        Assert.True(AgentRunner.setFakeStream (Some streamHandler))
        body ())

let actorChangeOps host =
    task {
        let! events = eventPast host |> Async.StartAsTask
        return
            events
            |> List.collect (fun event ->
                match event.authority, event.body with
                | Authority "Actor", EventBody.Change ops ->
                    ops
                | _ -> [])
    }

let hangUntilCancel () =
    let started = TaskCompletionSource<unit>()
    let handler (_: StartArgs) =
        started.TrySetResult() |> ignore
        AgentRunner.waitForCancel 8000 |> ignore
        fakeReply "late-complete"
    started, handler

let awaitHang (started: TaskCompletionSource<unit>) =
    started.Task.WaitAsync(TimeSpan.FromSeconds 2.0)

let askRequest (seeded: SeededAsk) =
    sampleRequest
        seeded.zoomId
        seeded.zoomId
        seeded.commandId
        seeded.graphIds

let ownedTexts (graph: Graph) focusId =
    graph.nodes.[focusId].children
    |> List.filter (fun child -> child.ref = Ownership.Owner)
    |> List.map (fun child -> graph.nodes.[child.id].text)

let waitOwnedText host focusId expected timeoutMs =
    waitUntil timeoutMs (fun () -> task {
        let! state = graphState host
        return
            List.contains
                expected
                (ownedTexts state.graph focusId)
    })

let ownedChildren host focusId =
    task {
        let! state = graphState host
        return
            state.graph.nodes.[focusId].children
            |> List.filter (fun child -> child.ref = Ownership.Owner)
            |> List.map (fun child ->
                let text =
                    match Map.tryFind child.id state.graph.nodes with
                    | Some node -> node.text
                    | None -> ""
                child.id, text)
    }

let private postBrowserChange host label ops =
    task {
        let event =
            { id = EventId.zero
              submissionId = Guid.NewGuid()
              authority = Authority "Browser"
              commandName = ""
              body = EventBody.Change ops }
        let! posted =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk label posted |> ignore
    }

let seedAskTree host commandText =
    task {
        let zoomId = NodeId.New()
        let commandId = NodeId.New()
        let noteId = NodeId.New()
        let ops =
            [ Op.NewNode(zoomId, "zoom")
              Op.NewNode(commandId, commandText)
              Op.NewNode(noteId, "visible-context")
              Op.Replace(
                  Graph.rootId,
                  [],
                  [ ChildNode.owner zoomId ])
              Op.Replace(
                  zoomId,
                  [],
                  [ ChildNode.owner commandId
                    ChildNode.owner noteId ]) ]
        do! postBrowserChange host "seed" ops
        return
            { zoomId = zoomId
              commandId = commandId
              graphIds = [ zoomId; commandId; noteId ] }
    }

let startAsk host (seeded: SeededAsk) =
    task {
        let request = askRequest seeded
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        return request
    }

let waitLive pool focusId timeoutMs =
    waitUntil timeoutMs (fun () -> task {
        return Set.contains focusId (pool.liveFocusIds ())
    })

let startLiveAsk host pool commandText =
    task {
        let! seeded = seedAskTree host commandText
        let! request = startAsk host seeded
        let! live = waitLive pool request.focusId 2000
        Assert.True(live)
        return request
    }

let seedCommand host text =
    task {
        let commandId = NodeId.New()
        let ops =
            [ Op.NewNode(commandId, text)
              Op.Replace(
                  Graph.rootId,
                  [],
                  [ ChildNode.owner commandId ]) ]
        do! postBrowserChange host "seed command" ops
        return commandId
    }

let startOnCommand host commandId =
    task {
        let request =
            sampleRequest
                commandId commandId commandId [ commandId ]
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "start command" result
        return request
    }

let registerActor pool name actorFn =
    pool.register (ActorName name) actorFn

let postChildThenWait childText =
    let posted = TaskCompletionSource<unit>()
    let actor: ActorFn =
        fun input coreChanges ->
            async {
                let event =
                    replaceFocusChild input.focusId childText
                let! accepted =
                    coreChanges.asCaller(actorCaller input.secret)
                        .postEvents [ event ]
                match accepted with
                | Ok _ -> posted.TrySetResult() |> ignore
                | Error err ->
                    posted.TrySetException(Exception err) |> ignore
                let! ct = Async.CancellationToken
                while not ct.IsCancellationRequested do
                    do! Async.Sleep 20
            }
    posted, actor

let cancelFocus host focusId =
    task {
        let! result =
            CoreMailbox.cancelByFocus host testCaller focusId
            |> Async.StartAsTask
        requireOk "cancelByFocus" result
    }

let expectActorCancelled host pool focusId =
    task {
        do! expectActorStop ActorCancelled host focusId
        expectLiveGone pool focusId
    }

let expectActorSucceeded host pool focusId =
    task {
        do! expectActorStop ActorSucceeded host focusId
        expectLiveGone pool focusId
    }

let expectActorFailed host pool focusId =
    task {
        let! stop = waitForActorStop host focusId 2000
        match stop with
        | Some (ActorFailed _) -> ()
        | other -> Assert.Fail($"expected ActorFailed, got {other}")
        expectLiveGone pool focusId
    }

let expectOwnedTexts host focusId expected =
    task {
        let! state = graphState host
        Assert.Equal<string list>(
            expected,
            ownedTexts state.graph focusId)
    }

let expectNoNodeText host fragment =
    task {
        let! state = graphState host
        let texts =
            state.graph.nodes
            |> Map.toList
            |> List.map (fun (_, node) -> node.text)
        Assert.DoesNotContain(fragment, texts)
    }

let waitFakeCancelled timeoutMs =
    waitUntil timeoutMs (fun () -> task {
        return AgentRunner.fakeCancelCount() >= 1
    })

let expectChangeCount host expected =
    task {
        let! events = eventPast host |> Async.StartAsTask
        let changes =
            events
            |> List.filter (fun event ->
                match event.body with
                | EventBody.Change _ -> true
                | _ -> false)
        Assert.Equal(expected, changes.Length)
    }

let hasActorStart host focusId =
    task {
        let! events = eventPast host |> Async.StartAsTask
        return
            events
            |> List.exists (fun event ->
                match event.body with
                | EventBody.ActorStart started ->
                    started.focusId = focusId
                | _ -> false)
    }

let actorStopCount host focusId =
    task {
        let! stops = actorStops host focusId
        return stops.Length
    }

let postOwnedChild host focusId text =
    task {
        let childId = NodeId.New()
        let event =
            { id = EventId.zero
              submissionId = Guid.NewGuid()
              authority = Authority "Browser"
              commandName = ""
              body =
                EventBody.Change
                    [ Op.NewNode(childId, text)
                      Op.Replace(
                          focusId,
                          [],
                          [ ChildNode.owner childId ]) ] }
        let! posted =
            CoreMailbox.postEvents host testCaller [ event ]
            |> Async.StartAsTask
        requireOk "postOwnedChild" posted |> ignore
    }

let private seedBrowserCommand host commandText =
    task {
        let commandId = NodeId.New()
        let noteId = NodeId.New()
        let ops =
            [ Op.NewNode(commandId, commandText)
              Op.NewNode(noteId, "visible-context")
              Op.Replace(
                  Graph.rootId,
                  [],
                  [ ChildNode.owner commandId ])
              Op.Replace(
                  commandId,
                  [],
                  [ ChildNode.owner noteId ]) ]
        do! postBrowserChange host "seed Browser Command" ops
        return commandId
    }

let private startBrowserAsk host commandId =
    task {
        let! state = graphState host
        let siteMap, _ =
            ViewModel.buildSiteMapFrom
                state.graph
                commandId
                (Sid 0)
        let request =
            CommandRequest.oneNodeStart
                state.graph
                siteMap
                commandId
                state.eventId
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        return request
    }

type BrowserAskLaunch =
    { request: ActorStart
      pollAfter: EventId
      graphAfterSeed: Graph }

let launchBrowserAsk host commandText =
    task {
        let! commandId = seedBrowserCommand host commandText
        let! state = graphState host
        let! request = startBrowserAsk host commandId
        return
            { request = request
              pollAfter = state.eventId
              graphAfterSeed = state.graph }
    }

let pollEventsSince host afterId =
    CoreMailbox.getEventsSince host afterId
    |> Async.StartAsTask

let expectOneNodeStart (request: ActorStart) =
    Assert.Equal(request.commandId, request.zoomId)
    Assert.Equal(request.commandId, request.focusId)
    Assert.Contains(request.commandId, request.graphIds)

