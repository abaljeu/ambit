module Gambol.Server.Tests.AgentGrokBotStreamTests

open Xunit
open Gambol.Shared
open Gambol.CloudAgents
open Gambol.Server
open Gambol.Server.Tests.AskCancelHarness

[<CollectionDefinition("GrokBot actor", DisableParallelization = true)>]
type GrokBotActorCollection() =
    class
    end

let private xmlStream texts resultText =
    let deltas = texts |> List.map AssistantText
    deltas
    @ [ RunFinished
            { AgentResult.Text = resultText
              Git = [] } ]

let private lastActorStop host focusId =
    task {
        let! history = CoreMailbox.eventHistory host
        return
            history.events
            |> List.tryFind (fun event ->
                match event.body with
                | EventBody.ActorStop(fid, _) when fid = focusId ->
                    true
                | _ -> false)
    }

let private emptyWakeGrok =
    { unusedGrokConfig with
        WakeUrl = ""
        WakeSecret = "secret-must-not-leak"
        InboundSecret = "inbound-must-not-leak" }

let private emptySecretGrok =
    { unusedGrokConfig with
        WakeUrl = "http://unused.example/"
        WakeSecret = ""
        InboundSecret = "inbound-must-not-leak" }

let private ambitPrompt =
    "You are Ambit AI. You edit outline children under Focus."
    + System.Environment.NewLine
    + System.Environment.NewLine
    + "The next message is the XML Zoom-rooted extract. "
    + "The Focus / prompt node is the one with css class prompt."
    + System.Environment.NewLine
    + System.Environment.NewLine
    + "Return ONLY a <> XML fragment that replaces every "
    + "child of Focus."
    + System.Environment.NewLine
    + "- Each element is one outline node; element text "
    + "is the node text."
    + System.Environment.NewLine
    + "- Nest elements for parent/child. Do not use "
    + "indent-outline."
    + System.Environment.NewLine
    + "- No preamble, no markdown fences, no explanation "
    + "outside the fragment."
    + System.Environment.NewLine
    + "- Do not rewrite Focus itself."

let private packedExtract host (seeded: SeededAsk) =
    task {
        let! state =
            CoreMailbox.getState host |> Async.StartAsTask
        match state with
        | Error err ->
            Assert.Fail(err)
            return ""
        | Ok s ->
            match
                AiExtractPack.packExtract
                    s.graph
                    seeded.zoomId
                    seeded.zoomId
            with
            | Error err ->
                Assert.Fail(err)
                return ""
            | Ok xml -> return xml
    }

[<Collection("GrokBot actor")>]
type AgentGrokBotStreamTests() =

    [<Fact>]
    member _.``gbot wake text is the Ambit prompt plus the extract``() =
        let seen = ref ""
        withGrokFakeStream
            (fun args ->
                seen := args.Text
                fakeReply "ignored")
            (fun _ ->
                xmlStream [ "<n>prompted</n>" ] "<n>prompted</n>")
            (fun () ->
                withHost (fun host pool -> task {
                    let! seeded = seedAskTree host "?ai gbot"
                    let! document = packedExtract host seeded
                    let! request = startAsk host seeded
                    do! expectActorSucceeded
                            host pool request.focusId
                    let expected =
                        ambitPrompt
                        + System.Environment.NewLine
                        + System.Environment.NewLine
                        + document
                    Assert.Equal(expected, !seen)
                    Assert.Contains("visible-context", document)
                    Assert.StartsWith("<node", document)
                }))

    [<Fact>]
    member _.``inbound deliver stays live for a second chunk``() =
        let session = ref ""
        withGrokFake
            (fun args ->
                session := args.SessionId
                Running)
            (fun () ->
                withHost (fun host pool -> task {
                    let! seeded = seedAskTree host "?ai gbot"
                    let! request = startAsk host seeded
                    let deadline = System.DateTime.UtcNow.AddSeconds 2.0
                    while !session = ""
                          && System.DateTime.UtcNow < deadline do
                        do! System.Threading.Tasks.Task.Delay 10
                    Assert.False(System.String.IsNullOrEmpty !session)
                    match pool.deliver (!session, "<n>In</n>") with
                    | Error err -> Assert.Fail($"pool: {err}")
                    | Ok() -> ()
                    match
                        GrokBotRunner.deliver !session "<n>In</n>"
                    with
                    | Error err -> Assert.Fail($"deliver: {err}")
                    | Ok() -> ()
                    match pool.deliver (!session, "") with
                    | Error err -> Assert.Fail($"pool done: {err}")
                    | Ok() -> ()
                    match GrokBotRunner.deliver !session "" with
                    | Error err -> Assert.Fail($"done: {err}")
                    | Ok() -> ()
                    let! seenIn =
                        waitOwnedText host request.focusId "In" 2000
                    Assert.True(seenIn)
                    Assert.True(
                        Set.contains
                            request.focusId
                            (pool.liveFocusIds ()))
                    let! stops =
                        actorStopCount host request.focusId
                    Assert.Equal(0, stops)
                    match pool.deliver (!session, "<n>Two</n>") with
                    | Error err -> Assert.Fail($"pool two: {err}")
                    | Ok() -> ()
                    match
                        GrokBotRunner.deliver !session "<n>Two</n>"
                    with
                    | Error err -> Assert.Fail($"two: {err}")
                    | Ok() -> ()
                    let! seenTwo =
                        waitOwnedText host request.focusId "Two" 2000
                    Assert.True(seenTwo)
                    do! expectOwnedTexts
                            host request.focusId [ "In"; "Two" ]
                    Assert.True(
                        Set.contains
                            request.focusId
                            (pool.liveFocusIds ()))
                    do! cancelFocus host request.focusId
                    do! expectActorCancelled
                            host pool request.focusId
                    match pool.deliver (!session, "late") with
                    | Error "not live" -> ()
                    | other ->
                        Assert.Fail($"late deliver: {other}")
                }))

    [<Fact>]
    member _.``fake Grok stream adds Focus children then Finishes``() =
        withGrokFakeStream
            (fun _ -> fakeReply "ignored")
            (fun _ ->
                xmlStream
                    [ "<n>Te"; "xt</n>"; "<n>two</n>" ]
                    "<n>Text</n><n>two</n>")
            (fun () ->
                withHost (fun host pool -> task {
                    let! seeded = seedAskTree host "?ai gbot"
                    let! request = startAsk host seeded
                    do! expectActorSucceeded
                            host pool request.focusId
                    do! expectOwnedTexts
                            host
                            request.focusId
                            [ "Text"; "two" ]
                }))

    [<Fact>]
    member _.``gbot extra tokens still use the Grok backend``() =
        withGrokFakeStream
            (fun _ -> fakeReply "ignored")
            (fun _ ->
                xmlStream [ "<n>extra-ok</n>" ] "<n>extra-ok</n>")
            (fun () ->
                withHost (fun host pool -> task {
                    let! seeded =
                        seedAskTree host "?ai gbot ignored-token"
                    let! request = startAsk host seeded
                    do! expectActorSucceeded
                            host pool request.focusId
                    do! expectOwnedTexts
                            host
                            request.focusId
                            [ "extra-ok" ]
                }))

    [<Fact>]
    member _.``cancel mid Grok stream drops live and keeps children``() =
        let hangStarted, hang = hangUntilGrokCancel ()
        withGrokFakeStream
            hang
            (fun _ -> [ AssistantText "<n>kept-stream</n>" ])
            (fun () ->
                withHost (fun host pool -> task {
                    let! request = startLiveAsk host pool "?ai gbot"
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
                    let! sawCancel = waitGrokFakeCancelled 2000
                    Assert.True(sawCancel)
                }))

    [<Fact>]
    member _.``empty WakeUrl fails without writing secrets``() =
        Assert.True(GrokBotRunner.setFake None)
        let named =
            AgentMessage.couldNotSend "Grok Bot" "missing wake URL"
        withHostGrok emptyWakeGrok (fun host pool -> task {
            let! seeded = seedAskTree host "?ai gbot"
            let! request = startAsk host seeded
            do! expectActorFailed host pool request.focusId
            let! stopEvent = lastActorStop host request.focusId
            match stopEvent with
            | None -> Assert.Fail("missing ActorStop")
            | Some event ->
                match event.body with
                | EventBody.ActorStop(_, ActorFailed msg) ->
                    Assert.Equal(named, msg)
                    Assert.DoesNotContain(
                        "secret-must-not-leak", msg)
                    Assert.DoesNotContain(
                        "inbound-must-not-leak", msg)
                | other ->
                    Assert.Fail($"bad stop, {other}")
            do! expectNoNodeText host "secret-must-not-leak"
            do! expectNoNodeText host "inbound-must-not-leak"
        })

    [<Fact>]
    member _.``empty WakeSecret fails without writing secrets``() =
        Assert.True(GrokBotRunner.setFake None)
        let named =
            AgentMessage.couldNotSend
                "Grok Bot" "missing wake secret"
        withHostGrok emptySecretGrok (fun host pool -> task {
            let! seeded = seedAskTree host "?ai gbot"
            let! request = startAsk host seeded
            do! expectActorFailed host pool request.focusId
            let! stopEvent = lastActorStop host request.focusId
            match stopEvent with
            | None -> Assert.Fail("missing ActorStop")
            | Some event ->
                match event.body with
                | EventBody.ActorStop(_, ActorFailed msg) ->
                    Assert.Equal(named, msg)
                    Assert.DoesNotContain(
                        "inbound-must-not-leak", msg)
                | other ->
                    Assert.Fail($"bad stop, {other}")
            do! expectNoNodeText host "inbound-must-not-leak"
        })

    [<Fact>]
    member _.``cursor Ask still uses AgentRunner not GrokBot``() =
        withGrokFakeStream
            (fun _ -> fakeReply "<n>gbot-child</n>")
            (fun _ ->
                xmlStream [ "<n>gbot-child</n>" ] "<n>gbot-child</n>")
            (fun () ->
                withFakeStream
                    (fun _ -> fakeReply "ignored")
                    (fun _ ->
                        xmlStream
                            [ "<n>cursor-child</n>" ]
                            "<n>cursor-child</n>")
                    (fun () ->
                        withHost (fun host pool -> task {
                            let! seeded = seedAskTree host "?ai"
                            let! request = startAsk host seeded
                            do! expectActorSucceeded
                                    host pool request.focusId
                            do! expectOwnedTexts
                                    host
                                    request.focusId
                                    [ "cursor-child" ]
                        })))
