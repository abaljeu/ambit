module Gambol.Server.Tests.AgentAskTests

open System
open System.Threading
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.CloudAgents
open Gambol.Server.Tests.TestBackend

[<CollectionDefinition("Agent ask runner", DisableParallelization = true)>]
type AgentAskRunnerCollection() =
    class
    end

[<Collection("Agent ask runner")>]
type AgentAskTests() =

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
                |> List.tryPick (fun event ->
                    match event.body with
                    | EventBody.ActorStop(fid, result) when fid = focusId ->
                        Some result
                    | _ -> None)
        }

    let waitForActorStop host focusId timeoutMs =
        task {
            let! found =
                waitUntil timeoutMs (fun () -> task {
                    let! result = tryActorStop host focusId
                    return result.IsSome
                })
            if found then
                return! tryActorStop host focusId
            else
                return None
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

    let withFake handler body =
        task {
            Assert.True(AgentRunner.setFake (Some handler))
            try
                do! body ()
            finally
                Assert.True(AgentRunner.setFake None)
        }

    let sampleResult text =
        { AgentResult.Text = text
          Git = [] }

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

    [<Fact>]
    member _.``Ask with fake completion replaces Focus Children through Core Change``
        ()
        =
        withFake
            (fun args ->
                Assert.Contains("visible-context", args.Prompt)
                sampleResult "from-agent")
            (fun () ->
                withHost (fun host pool -> task {
                    let! zoomId, commandId, graphIds =
                        seedAskTree host "?ai"
                    let! request =
                        startAsk host zoomId commandId graphIds
                    let! stop =
                        waitForActorStop host request.focusId 2000
                    match stop with
                    | Some ActorSucceeded -> ()
                    | other ->
                        Assert.Fail($"expected ActorSucceeded, got {other}")
                    Assert.False(
                        Set.contains
                            request.focusId
                            (pool.liveFocusIds ()))
                    let! state =
                        CoreMailbox.getState host |> Async.StartAsTask
                    let state = requireOk "getState" state
                    Assert.Equal<string list>(
                        [ "from-agent" ],
                        ownedTexts state.graph request.focusId)
                    let! events =
                        eventPast host |> Async.StartAsTask
                    let started =
                        events
                        |> List.exists (fun event ->
                            match event.body with
                            | EventBody.ActorStart started ->
                                started.focusId = request.focusId
                            | _ -> false)
                    Assert.True(started)
                }))

    [<Fact>]
    member _.``Ask ignores Command args and still replaces Focus Children``() =
        withFake
            (fun _ -> sampleResult "ignored-args-reply")
            (fun () ->
                withHost (fun host _ -> task {
                    let! zoomId, commandId, graphIds =
                        seedAskTree host "?ai later please"
                    let! request =
                        startAsk host zoomId commandId graphIds
                    let! stop =
                        waitForActorStop host request.focusId 2000
                    match stop with
                    | Some ActorSucceeded -> ()
                    | other ->
                        Assert.Fail($"expected ActorSucceeded, got {other}")
                    let! state =
                        CoreMailbox.getState host |> Async.StartAsTask
                    let state = requireOk "getState" state
                    Assert.Equal<string list>(
                        [ "ignored-args-reply" ],
                        ownedTexts state.graph request.focusId)
                }))

    [<Fact>]
    member _.``Ask empty success clears every Focus Child``() =
        withFake
            (fun _ -> sampleResult "")
            (fun () ->
                withHost (fun host _ -> task {
                    let! zoomId, commandId, graphIds =
                        seedAskTree host "?ai"
                    let! request =
                        startAsk host zoomId commandId graphIds
                    let! stop =
                        waitForActorStop host request.focusId 2000
                    match stop with
                    | Some ActorSucceeded -> ()
                    | other ->
                        Assert.Fail($"expected ActorSucceeded, got {other}")
                    let! state =
                        CoreMailbox.getState host |> Async.StartAsTask
                    let state = requireOk "getState" state
                    Assert.Empty(ownedTexts state.graph request.focusId)
                }))

    [<Fact>]
    member _.``Ask extract carries Graph.focus from launch``() =
        let seen = ref None
        let probe: ActorFn =
            fun input coreChanges ->
                async {
                    seen := input.graph.focus
                    let caller =
                        { authority = Authority "Actor"
                          name = ""
                          secret = input.secret }
                    let! _ =
                        coreChanges.asCaller(caller).actorStop
                            ActorSucceeded
                    return ()
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
            let! posted =
                CoreMailbox.postGraphOnly host testCaller event
                |> Async.StartAsTask
            requireOk "seed probe" posted |> ignore
            let request =
                sampleRequest
                    commandId commandId commandId [ commandId ]
            let! result =
                CoreMailbox.startActor host testCaller request
                |> Async.StartAsTask
            requireOk "start probe" result
            let! stop = waitForActorStop host request.focusId 2000
            match stop with
            | Some ActorSucceeded -> ()
            | other -> Assert.Fail($"probe stop {other}")
            Assert.Equal(Some commandId, !seen)
        })

    [<Fact>]
    member _.``second launch on the same Focus is refused``() =
        withFake
            (fun _ ->
                Thread.Sleep 300
                sampleResult "slow")
            (fun () ->
                withHost (fun host _ -> task {
                    let! zoomId, commandId, graphIds =
                        seedAskTree host "?ai"
                    let request =
                        sampleRequest
                            zoomId zoomId commandId graphIds
                    let! first =
                        CoreMailbox.startActor
                            host testCaller request
                        |> Async.StartAsTask
                    requireOk "first start" first
                    let! second =
                        CoreMailbox.startActor
                            host testCaller request
                        |> Async.StartAsTask
                    match second with
                    | Error err ->
                        Assert.Contains("focus", err)
                    | Ok () ->
                        Assert.Fail("expected Focus exclusivity")
                    let! _ =
                        waitForActorStop host request.focusId 2000
                    return ()
                }))

    [<Fact>]
    member _.``credentialed Change under Focus is accepted while Ask runs``() =
        withFake
            (fun _ ->
                Thread.Sleep 200
                sampleResult "from-agent")
            (fun () ->
                withHost (fun host _ -> task {
                    let! zoomId, commandId, graphIds =
                        seedAskTree host "?ai"
                    let! request =
                        startAsk host zoomId commandId graphIds
                    let editId = NodeId.New()
                    let edit =
                        { id = EventId.zero
                          submissionId = Guid.NewGuid()
                          authority = Authority "Browser"
                          commandName = ""
                          body =
                            EventBody.Change
                                [ Op.NewNode(editId, "browser-edit")
                                  Op.Replace(
                                      request.focusId,
                                      [],
                                      [ ChildNode.owner editId ]) ] }
                    let! posted =
                        CoreMailbox.postEvents
                            host testCaller [ edit ]
                        |> Async.StartAsTask
                    requireOk "concurrent edit" posted |> ignore
                    let! stop =
                        waitForActorStop host request.focusId 2000
                    match stop with
                    | Some ActorSucceeded -> ()
                    | other ->
                        Assert.Fail($"expected ActorSucceeded, got {other}")
                }))
