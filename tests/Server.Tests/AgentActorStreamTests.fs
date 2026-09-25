module Gambol.Server.Tests.AgentActorStreamTests

open Xunit
open Gambol.Shared
open Gambol.CloudAgents
open Gambol.Server.Tests.AskCancelHarness

let private xmlStream texts resultText =
    let deltas =
        texts
        |> List.map AssistantText
    deltas
    @ [ RunFinished
            { AgentResult.Text = resultText
              Git = [] } ]

let private newNodeTexts ops =
    ops
    |> List.choose (function
        | Op.NewNode(_, text) -> Some text
        | _ -> None)

let private hasSetText ops =
    ops
    |> List.exists (function
        | Op.SetText _ -> true
        | _ -> false)

[<Collection("Agent ask runner")>]
type AgentActorStreamTests() =

    [<Fact>]
    member _.``fake stream deltas add Focus children without text-edit``
        ()
        =
        withFakeStream
            (fun _ -> fakeReply "ignored")
            (fun _ ->
                xmlStream
                    [ "<n>Te"; "xt</n>"; "<n>two</n>" ]
                    "<n>Text</n><n>two</n>")
            (fun () ->
                withHost (fun host pool -> task {
                    let! seeded = seedAskTree host "?ai"
                    let! request = startAsk host seeded
                    do! expectActorSucceeded
                            host pool request.focusId
                    do! expectOwnedTexts
                            host
                            request.focusId
                            [ "Text"; "two" ]
                    let! ops = actorChangeOps host
                    Assert.False(hasSetText ops)
                    let news = newNodeTexts ops
                    Assert.Equal(2, news.Length)
                    Assert.Contains("Text", news)
                    Assert.Contains("two", news)
                }))

    [<Fact>]
    member _.``cancel mid-stream drops live and keeps streamed children``
        ()
        =
        let hangStarted, hang = hangUntilCancel ()
        withFakeStream
            hang
            (fun _ -> [ AssistantText "<n>kept-stream</n>" ])
            (fun () ->
                withHost (fun host pool -> task {
                    let! request = startLiveAsk host pool "?ai"
                    do! awaitHang hangStarted
                    let! seen =
                        waitOwnedText
                            host
                            request.focusId
                            "kept-stream"
                            2000
                    Assert.True(seen)
                    do! cancelFocus host request.focusId
                    do! expectActorCancelled
                            host pool request.focusId
                    do! expectOwnedTexts
                            host
                            request.focusId
                            [ "kept-stream" ]
                    let! ops = actorChangeOps host
                    Assert.False(hasSetText ops)
                    let! sawCancel = waitFakeCancelled 2000
                    Assert.True(sawCancel)
                }))
