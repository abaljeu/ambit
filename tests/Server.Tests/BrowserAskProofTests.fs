module Gambol.Server.Tests.BrowserAskProofTests

open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.AskCancelHarness

let private lifecycleIndexes (events: Ev list) focusId =
    let chrono = List.rev events
    let tryIndex pred = List.tryFindIndex pred chrono
    let started =
        tryIndex (fun event ->
            match event.body with
            | EventBody.ActorStart body
                when body.focusId = focusId -> true
            | _ -> false)
    let finished =
        tryIndex (fun event ->
            match event.body with
            | EventBody.ActorStop(fid, ActorSucceeded)
                when fid = focusId -> true
            | _ -> false)
    started, finished

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
                    let! before =
                        CoreMailbox.getEventId host
                        |> Async.StartAsTask
                    let! request = launchBrowserAsk host "?ai"
                    expectOneNodeStart request
                    do! expectActorSucceeded
                            host pool request.focusId
                    do! expectOwnedTexts
                            host
                            request.focusId
                            [ "from-agent" ]
                    let! poll = pollEventsSince host before
                    let started, finished =
                        lifecycleIndexes poll request.focusId
                    match started, finished with
                    | Some startIdx, Some stopIdx
                        when startIdx < stopIdx ->
                        ()
                    | other ->
                        Assert.Fail(
                            $"expected ActorStarted then ActorFinished, got {other}")
                    let hasReplace =
                        poll
                        |> List.exists (fun event ->
                            match event.body with
                            | EventBody.Change _ -> true
                            | _ -> false)
                    Assert.True(hasReplace)
                }))
