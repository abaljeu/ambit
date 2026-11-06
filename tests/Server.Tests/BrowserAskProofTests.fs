module Gambol.Server.Tests.BrowserAskProofTests

open Xunit
open Gambol.Shared
open Gambol.Server.Tests.AskCancelHarness

let private lifecycleIndexes (events: Ev list) focusId =
    let chrono = List.rev events
    let tryIndex pred = List.tryFindIndex pred chrono
    let started =
        tryIndex (fun event ->
            match event.body with
            | EventBody.Order (Order.ActorOrder order)
                when order.focusId = focusId -> true
            | _ -> false)
    let finished =
        tryIndex (fun event ->
            match event.body with
            | EventBody.ActorStop(fid, ActorSucceeded)
                when fid = focusId -> true
            | _ -> false)
    started, finished

let private applyPollChanges graph eventId events =
    let chrono = List.rev events
    let start = { graph = graph; eventId = eventId }
    let folder state event =
        match event.body with
        | EventBody.Change _ ->
            match Ev.apply event state with
            | ApplyResult.Changed next
            | ApplyResult.Unchanged next -> next
            | ApplyResult.Invalid (_, err) ->
                Assert.Fail($"poll Change: {err}")
                state
        | _ -> state
    (List.fold folder start chrono).graph

[<Collection("Agent ask runner")>]
type BrowserAskProofTests() =

    [<Fact>]
    member _.``Browser Ask from what I see finishes with Poll Focus Children and drops live``
        ()
        =
        withFake
            (fun args ->
                Assert.Contains("visible-context", args.Prompt)
                fakeReply "from-agent")
            (fun () ->
                withHost (fun host pool -> task {
                    let! launched = launchBrowserAsk host "?ai"
                    expectOneNodeStart launched.request
                    do! expectActorSucceeded
                            host pool launched.request.focusId
                    let! poll =
                        pollEventsSince host launched.pollAfter
                    let started, finished =
                        lifecycleIndexes
                            poll launched.request.focusId
                    match started, finished with
                    | Some startIdx, Some stopIdx
                        when startIdx < stopIdx ->
                        ()
                    | other ->
                        Assert.Fail(
                            $"expected ActorOrder then ActorStop, got {other}")
                    let fromPoll =
                        applyPollChanges
                            launched.graphAfterSeed
                            launched.pollAfter
                            poll
                    Assert.Equal<string list>(
                        [ "from-agent" ],
                        ownedTexts
                            fromPoll launched.request.focusId)
                    do! expectOwnedTexts
                            host
                            launched.request.focusId
                            [ "from-agent" ]
                }))

    [<Fact>]
    member _.``posted ?ai Run has ActorOrder and no ActorStart`` () =
        withFake
            (fun _ -> fakeReply "from-agent")
            (fun () ->
                withHost (fun host pool -> task {
                    let! seeded = seedAskTree host "?ai"
                    let request = askRequest seeded
                    do! postLiveAiRun host request
                    do! expectActorSucceeded
                            host pool request.focusId
                    do! expectNoActorStart host request.focusId
                    let! ordered = hasActorStart host request.focusId
                    Assert.True(ordered)
                }))
