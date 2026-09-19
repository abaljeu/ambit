module Gambol.Server.Tests.CancelByFocusTests

open System
open System.Threading
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.CloudAgents
open Gambol.Server.Tests.TestBackend

[<Collection("Agent ask runner")>]
type CancelByFocusTests() =

    let requireOk label result =
        match result with
        | Ok value -> value
        | Error err ->
            Assert.Fail($"{label}: {err}")
            Unchecked.defaultof<_>

    let eventPast host =
        async {
            let! history = CoreMailbox.eventHistory host
            return history.events
        }

    let rec waitUntil remainingMs (check: unit -> Task<bool>) =
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

    let tryActorStop host focusId =
        task {
            let! events = eventPast host |> Async.StartAsTask
            return
                events
                |> List.choose (fun event ->
                    match event.body with
                    | EventBody.ActorStop(fid, result)
                        when fid = focusId ->
                        Some result
                    | _ -> None)
        }

    let waitForActorStop host focusId timeoutMs =
        task {
            let! found =
                waitUntil timeoutMs (fun () -> task {
                    let! stops = tryActorStop host focusId
                    return not stops.IsEmpty
                })
            if found then
                return! tryActorStop host focusId
            else
                return []
        }

    let createHost () =
        let dataDir = newTempDir ()
        let pool = CoreActorPool.create ()
        pool.register (ActorName "test") TestActor.actorFn
        pool.register (ActorName "ai") RunAgentActor.actorFn
        let host =
            CoreMailbox.host
                pool
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
        host, pool

    let withHost body =
        task {
            let host, pool = createHost ()
            try
                do! body host pool
            finally
                CoreMailbox.dispose host
        }

    let clearFake () =
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

    let withFake handler body =
        task {
            Assert.True(AgentRunner.setFake (Some handler))
            try
                do! body ()
            finally
                clearFake ()
        }

    let sampleResult text =
        { AgentResult.Text = text
          Git = [] }

    let hangUntilCancel () =
        let started = TaskCompletionSource<unit>()
        let handler (_: StartArgs) =
            started.TrySetResult() |> ignore
            AgentRunner.waitForCancel 8000 |> ignore
            sampleResult "late-complete"
        started, handler

    let sampleRequest zoomId focusId commandId graphIds : ActorStart =
        { zoomId = zoomId
          focusId = focusId
          commandId = commandId
          graphIds = graphIds
          eventId = EventId.zero }

    let ownedTexts (graph: Graph) focusId =
        graph.nodes.[focusId].children
        |> List.filter (fun child -> child.ref = Ownership.Owner)
        |> List.map (fun child -> graph.nodes.[child.id].text)

    let changeEvents (events: Ev list) =
        events
        |> List.filter (fun event ->
            match event.body with
            | EventBody.Change _ -> true
            | _ -> false)

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
            let event =
                { id = EventId.zero
                  submissionId = Guid.NewGuid()
                  authority = Authority "Browser"
                  commandName = ""
                  body = EventBody.Change ops }
            let! posted =
                CoreMailbox.postGraphOnly host testCaller event
                |> Async.StartAsTask
            requireOk "seed" posted |> ignore
            return
                zoomId,
                commandId,
                [ zoomId; commandId; noteId ]
        }

    let startAsk host zoomId commandId graphIds =
        task {
            let request =
                sampleRequest zoomId zoomId commandId graphIds
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

    [<Fact>]
    member _.``cancel by Focus accepts Cancelled and drops the live row``
        ()
        =
        let hangStarted, hang = hangUntilCancel ()
        withFake hang (fun () ->
            withHost (fun host pool -> task {
                let! zoomId, commandId, graphIds =
                    seedAskTree host "?ai"
                let! request =
                    startAsk host zoomId commandId graphIds
                let! live =
                    waitLive pool request.focusId 2000
                Assert.True(live)
                do! hangStarted.Task.WaitAsync(TimeSpan.FromSeconds 2.0)
                let! cancelled =
                    CoreMailbox.cancelByFocus
                        host testCaller request.focusId
                    |> Async.StartAsTask
                requireOk "cancelByFocus" cancelled
                let! stops =
                    waitForActorStop host request.focusId 2000
                match stops with
                | [ ActorCancelled ] -> ()
                | other ->
                    Assert.Fail($"expected ActorCancelled, got {other}")
                Assert.False(
                    Set.contains
                        request.focusId
                        (pool.liveFocusIds ()))
                let! state =
                    CoreMailbox.getState host |> Async.StartAsTask
                let state = requireOk "getState" state
                Assert.Equal<string list>(
                    [ "?ai"; "visible-context" ],
                    ownedTexts state.graph request.focusId)
                let! sawCancel =
                    waitUntil 2000 (fun () -> task {
                        return AgentRunner.fakeCancelCount() >= 1
                    })
                Assert.True(sawCancel)
            }))

    [<Fact>]
    member _.``Cancel before Change rejects later Agent replace``() =
        let hangStarted, hang = hangUntilCancel ()
        withFake hang (fun () ->
            withHost (fun host pool -> task {
                let! zoomId, commandId, graphIds =
                    seedAskTree host "?ai"
                let! request =
                    startAsk host zoomId commandId graphIds
                let! live =
                    waitLive pool request.focusId 2000
                Assert.True(live)
                do! hangStarted.Task.WaitAsync(TimeSpan.FromSeconds 2.0)
                let! _ =
                    CoreMailbox.cancelByFocus
                        host testCaller request.focusId
                    |> Async.StartAsTask
                let! stops =
                    waitForActorStop host request.focusId 2000
                match stops with
                | [ ActorCancelled ] -> ()
                | other ->
                    Assert.Fail($"expected ActorCancelled, got {other}")
                do! Task.Delay 200
                let! state =
                    CoreMailbox.getState host |> Async.StartAsTask
                let state = requireOk "getState" state
                Assert.Equal<string list>(
                    [ "?ai"; "visible-context" ],
                    ownedTexts state.graph request.focusId)
                Assert.DoesNotContain(
                    "late-complete",
                    ownedTexts state.graph request.focusId)
                let! events = eventPast host |> Async.StartAsTask
                Assert.Equal(1, changeEvents events |> List.length)
            }))

    [<Fact>]
    member _.``Change before Cancel keeps the accepted children``() =
        let posted = TaskCompletionSource<unit>()
        let probe: ActorFn =
            fun input coreChanges ->
                async {
                    let childId = NodeId.New()
                    let caller =
                        { authority = Authority "Actor"
                          name = ""
                          secret = input.secret }
                    let event =
                        { id = EventId.zero
                          submissionId = Guid.NewGuid()
                          authority = Authority "Actor"
                          commandName = ""
                          body =
                            EventBody.Change
                                [ Op.NewNode(childId, "kept-change")
                                  Op.Replace(
                                      input.focusId,
                                      [],
                                      [ ChildNode.owner childId ]) ] }
                    let! accepted =
                        coreChanges.asCaller(caller).postEvents
                            [ event ]
                    match accepted with
                    | Ok _ -> posted.TrySetResult() |> ignore
                    | Error err ->
                        posted.TrySetException(
                            Exception(err))
                        |> ignore
                    let! ct = Async.CancellationToken
                    while not ct.IsCancellationRequested do
                        do! Async.Sleep 20
                }
        withHost (fun host pool -> task {
            pool.register (ActorName "probe") probe
            let commandId = NodeId.New()
            let event =
                { id = EventId.zero
                  submissionId = Guid.NewGuid()
                  authority = Authority "Browser"
                  commandName = ""
                  body =
                    EventBody.Change
                        [ Op.NewNode(commandId, "?probe")
                          Op.Replace(
                              Graph.rootId,
                              [],
                              [ ChildNode.owner commandId ]) ] }
            let! seeded =
                CoreMailbox.postGraphOnly host testCaller event
                |> Async.StartAsTask
            requireOk "seed probe" seeded |> ignore
            let request =
                sampleRequest
                    commandId commandId commandId [ commandId ]
            let! started =
                CoreMailbox.startActor host testCaller request
                |> Async.StartAsTask
            requireOk "start probe" started
            do! posted.Task.WaitAsync(TimeSpan.FromSeconds 2.0)
            let! cancelled =
                CoreMailbox.cancelByFocus
                    host testCaller request.focusId
                |> Async.StartAsTask
            requireOk "cancelByFocus" cancelled
            let! stops =
                waitForActorStop host request.focusId 2000
            match stops with
            | [ ActorCancelled ] -> ()
            | other ->
                Assert.Fail($"expected ActorCancelled, got {other}")
            Assert.False(
                Set.contains request.focusId (pool.liveFocusIds ()))
            let! state =
                CoreMailbox.getState host |> Async.StartAsTask
            let state = requireOk "getState" state
            Assert.Equal<string list>(
                [ "kept-change" ],
                ownedTexts state.graph request.focusId)
        })

    [<Fact>]
    member _.``late duplicate completion after cancel is ignored``() =
        let hangStarted, hang = hangUntilCancel ()
        withFake hang (fun () ->
            withHost (fun host pool -> task {
                let! zoomId, commandId, graphIds =
                    seedAskTree host "?ai"
                let! request =
                    startAsk host zoomId commandId graphIds
                let! live =
                    waitLive pool request.focusId 2000
                Assert.True(live)
                do! hangStarted.Task.WaitAsync(TimeSpan.FromSeconds 2.0)
                let! first =
                    CoreMailbox.cancelByFocus
                        host testCaller request.focusId
                    |> Async.StartAsTask
                requireOk "first cancel" first
                let! second =
                    CoreMailbox.cancelByFocus
                        host testCaller request.focusId
                    |> Async.StartAsTask
                requireOk "late cancel" second
                let! stops =
                    waitForActorStop host request.focusId 2000
                Assert.Equal(1, stops.Length)
                Assert.Equal(ActorCancelled, stops.Head)
                do! Task.Delay 200
                let! again = tryActorStop host request.focusId
                Assert.Equal(1, again.Length)
                Assert.False(
                    Set.contains
                        request.focusId
                        (pool.liveFocusIds ()))
            }))
