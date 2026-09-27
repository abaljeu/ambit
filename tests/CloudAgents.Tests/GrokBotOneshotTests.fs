module Gambol.CloudAgents.Tests.GrokBotOneshotTests

open System
open System.Net.Http
open System.Threading
open FSharp.Data
open Xunit
open Gambol.CloudAgents
open Gambol.CloudAgents.Internal

[<Collection("CloudAgents grokbot")>]
type GrokBotOneshotTests() =

    let unusedConfig =
        { GrokBotConfig.WakeUrl = "http://unused.example/"
          WakeSecret = "wake-secret-value"
          InboundSecret = "inbound-secret-value" }

    let emptyUrlConfig =
        { unusedConfig with WakeUrl = "" }

    let sampleResult text =
        { AgentResult.Text = text
          Git = [] }

    let sampleResponseUrl =
        "https://collaborative-systems.org/ambit/actors/deliver"

    let wakeArgs config sessionId text : GrokBotWakeArgs =
        { Config = config
          Text = text
          CommandId = "cmd-1"
          FocusId = "focus-1"
          SessionId = sessionId
          ResponseUrl = sampleResponseUrl }

    let streamArgs sessionId : GrokBotStreamArgs =
        { Config = unusedConfig
          SessionId = sessionId
          PollIntervalMs = 10
          MaxWaitMs = Some 2000 }

    let collectFold =
        { Seed = []
          OnEvent = fun seen ev -> ev :: seen }

    let clearFake () =
        let deadline = DateTime.UtcNow.AddSeconds 2.0
        let rec spin () =
            if GrokBotRunner.setFake None then
                true
            elif DateTime.UtcNow > deadline then
                false
            else
                Thread.Sleep 10
                spin ()
        Assert.True(spin ())

    let withFake handler body =
        Assert.True(GrokBotRunner.setFake (Some handler))
        try
            body ()
        finally
            clearFake ()

    let hangingLateFinish _ =
        GrokBotRunner.waitForCancel 8000 |> ignore
        Finished(sampleResult "late")

    let keptTextOnly _ = [ AssistantText "kept" ]

    let noteCancelEvent
        (sawText: ManualResetEvent)
        (seen: string ref)
        ev =
        match ev with
        | AssistantText _ ->
            sawText.Set() |> ignore
        | RunFinished _ ->
            seen := "finished"
        | _ -> ()

    let recordCancelOutcome seen result =
        match result with
        | Error(ApiError("cancelled", _)) ->
            seen := "cancelled"
        | Ok _ -> seen := "ok"
        | Error _ -> seen := "other"

    let streamUntilCancel sessionId sawText seen =
        let fold =
            { Seed = ()
              OnEvent =
                fun () ev -> noteCancelEvent sawText seen ev }
        GrokBotRunner.streamUntilComplete
            { streamArgs sessionId with MaxWaitMs = None }
            fold
        |> recordCancelOutcome seen

    let assertCancelledMidStream sessionId =
        let sawText = new ManualResetEvent(false)
        let seen = ref "none"
        let worker =
            Thread(fun () ->
                streamUntilCancel sessionId sawText seen)
        worker.Start()
        Assert.True(sawText.WaitOne 2000)
        match GrokBotRunner.cancel unusedConfig sessionId with
        | Error err -> Assert.Fail($"cancel: {err}")
        | Ok() ->
            Assert.True(worker.Join 2000)
            Assert.Equal("cancelled", !seen)
            Assert.True(GrokBotRunner.fakeCancelCount() >= 1)

    [<Fact>]
    member _.``GrokBotConfig holds the three grokbot keys``() =
        Assert.Equal(
            "http://unused.example/", unusedConfig.WakeUrl)
        Assert.Equal("wake-secret-value", unusedConfig.WakeSecret)
        Assert.Equal(
            "inbound-secret-value", unusedConfig.InboundSecret)

    [<Fact>]
    member _.``wake JSON is the map payload and omits secrets``() =
        let args =
            wakeArgs unusedConfig "sess-1" "extract pack"
        let json =
            GrokBotHttp.wakeRequestJson "2026-09-25T00:00:00Z" args
        Assert.Equal("ambit", json.["source"].AsString())
        Assert.Equal("message", json.["kind"].AsString())
        Assert.Equal(
            "2026-09-25T00:00:00Z", json.["sentAt"].AsString())
        Assert.Equal("cmd-1", json.["commandId"].AsString())
        Assert.Equal("focus-1", json.["focusId"].AsString())
        Assert.Equal("sess-1", json.["sessionId"].AsString())
        Assert.Equal("extract pack", json.["text"].AsString())
        Assert.Equal(sampleResponseUrl, json.["responseUrl"].AsString())
        match json.["payload"] with
        | JsonValue.Record fields ->
            Assert.Empty(fields)
        | other -> Assert.Fail($"payload: {other}")
        match json with
        | JsonValue.Record fields ->
            let names = fields |> Array.map fst
            Assert.DoesNotContain("inboundSecret", names)
            Assert.DoesNotContain("InboundSecret", names)
        | other -> Assert.Fail($"record: {other}")
        let raw = json.ToString()
        Assert.DoesNotContain("wake-secret-value", raw)
        Assert.DoesNotContain("inbound-secret-value", raw)

    [<Fact>]
    member _.``applyWakeAuth sends Authorization Bearer``() =
        use req = new HttpRequestMessage()
        let got =
            GrokBotHttp.applyWakeAuth req "wake-secret-value"
        Assert.NotNull(got.Headers.Authorization)
        Assert.Equal("Bearer", got.Headers.Authorization.Scheme)
        Assert.Equal(
            "wake-secret-value",
            got.Headers.Authorization.Parameter)
        Assert.False(
            got.Headers.Contains("X-Ambit-Wake-Secret"))
        Assert.False(
            got.Headers.Contains("X-Ambit-Inbound-Secret"))

    [<Theory>]
    [<InlineData("")>]
    [<InlineData(" ")>]
    member _.``applyWakeAuth skips header when secret is blank``
        (secret: string)
        =
        use req = new HttpRequestMessage()
        let got = GrokBotHttp.applyWakeAuth req secret
        Assert.Null(got.Headers.Authorization)
        Assert.False(
            got.Headers.Contains("X-Ambit-Wake-Secret"))
        Assert.Empty(got.Headers)

    [<Fact>]
    member _.``interpretWakeResponse is ack-only``() =
        match
            GrokBotHttp.interpretWakeResponse
                200 "I am the bot reply"
        with
        | Ok() -> ()
        | Error err -> Assert.Fail($"200: {err}")
        match
            GrokBotHttp.interpretWakeResponse 204 "still-not-text"
        with
        | Ok() -> ()
        | Error err -> Assert.Fail($"204: {err}")
        match GrokBotHttp.interpretWakeResponse 401 "nope" with
        | Error "unauthorized" -> ()
        | other -> Assert.Fail($"401: {other}")

    [<Fact>]
    member _.``empty WakeSecret fails without sending``() =
        Assert.True(GrokBotRunner.setFake None)
        let config =
            { unusedConfig with WakeSecret = "" }
        let args = wakeArgs config "sess-nosecret" "pack"
        match GrokBotRunner.wake args with
        | Error(AuthenticationFailed msg) ->
            Assert.Equal(
                AgentMessage.couldNotSend
                    "Grok Bot" "missing wake secret",
                msg)
            Assert.DoesNotContain("wake-secret-value", msg)
            Assert.DoesNotContain("inbound-secret-value", msg)
        | other ->
            Assert.Fail($"expected missing wake secret, {other}")

    [<Fact>]
    member _.``empty WakeUrl fails without writing the secret``() =
        Assert.True(GrokBotRunner.setFake None)
        let args = wakeArgs emptyUrlConfig "sess-empty" "pack"
        match GrokBotRunner.wake args with
        | Error(AuthenticationFailed msg) ->
            Assert.Equal(
                AgentMessage.couldNotSend
                    "Grok Bot" "missing wake URL",
                msg)
            Assert.DoesNotContain("wake-secret-value", msg)
            Assert.DoesNotContain("inbound-secret-value", msg)
        | other ->
            Assert.Fail($"expected missing wake URL, {other}")

    [<Fact>]
    member _.``empty Done does not complete streamUntilComplete``() =
        Assert.True(GrokBotRunner.setFake None)
        let sessionId = "sess-live"
        match GrokBotRunner.deliver sessionId "hel" with
        | Error err -> Assert.Fail($"chunk1: {err}")
        | Ok() -> ()
        match GrokBotRunner.deliver sessionId "lo" with
        | Error err -> Assert.Fail($"chunk2: {err}")
        | Ok() -> ()
        match GrokBotRunner.deliver sessionId "" with
        | Error err -> Assert.Fail($"done: {err}")
        | Ok() -> ()
        match
            GrokBotRunner.streamUntilComplete
                { streamArgs sessionId with MaxWaitMs = Some 200 }
                collectFold
        with
        | Error AgentError.Timeout -> ()
        | other ->
            Assert.Fail($"expected keep-open Timeout, {other}")

    [<Fact>]
    member _.``empty Done keeps listening for a later inbound chunk``() =
        Assert.True(GrokBotRunner.setFake None)
        let sessionId = "sess-keep"
        match GrokBotRunner.deliver sessionId "one" with
        | Error err -> Assert.Fail($"chunk1: {err}")
        | Ok() -> ()
        let sawTwo = new ManualResetEvent(false)
        let seen = ref "none"
        let events = ref List.empty<AgentStreamEvent>
        let fold =
            { Seed = ()
              OnEvent =
                fun () ev ->
                    events := ev :: !events
                    match ev with
                    | AssistantText "two" ->
                        sawTwo.Set() |> ignore
                    | _ -> () }
        let worker =
            Thread(fun () ->
                GrokBotRunner.streamUntilComplete
                    { streamArgs sessionId with MaxWaitMs = None }
                    fold
                |> recordCancelOutcome seen)
        worker.Start()
        match GrokBotRunner.deliver sessionId "" with
        | Error err -> Assert.Fail($"done: {err}")
        | Ok() -> ()
        match GrokBotRunner.deliver sessionId "two" with
        | Error err -> Assert.Fail($"chunk2: {err}")
        | Ok() -> ()
        Assert.True(sawTwo.WaitOne 2000)
        match GrokBotRunner.cancel unusedConfig sessionId with
        | Error err -> Assert.Fail($"cancel: {err}")
        | Ok() ->
            Assert.True(worker.Join 2000)
            Assert.Equal("cancelled", !seen)
            let expected =
                [ AssistantText "one"
                  RunFinished(sampleResult "")
                  AssistantText "two" ]
            Assert.Equal<AgentStreamEvent list>(
                expected, List.rev !events)

    [<Fact>]
    member _.``live cancel mid inbound is cancelled not Finish``() =
        Assert.True(GrokBotRunner.setFake None)
        let sessionId = "sess-live-c"
        match GrokBotRunner.deliver sessionId "kept" with
        | Error err -> Assert.Fail($"chunk: {err}")
        | Ok() -> ()
        let sawText = new ManualResetEvent(false)
        let seen = ref "none"
        let worker =
            Thread(fun () ->
                streamUntilCancel sessionId sawText seen)
        worker.Start()
        Assert.True(sawText.WaitOne 2000)
        match GrokBotRunner.cancel unusedConfig sessionId with
        | Error err -> Assert.Fail($"cancel: {err}")
        | Ok() ->
            Assert.True(worker.Join 2000)
            Assert.Equal("cancelled", !seen)

    [<Fact>]
    member _.``fake oneshot emits AssistantText then RunFinished``() =
        withFake
            (fun _ -> Finished(sampleResult "ignored"))
            (fun () ->
                Assert.True(
                    GrokBotRunner.setFakeStream (Some (fun _ ->
                        [ AssistantText "hel"
                          AssistantText "lo"
                          RunFinished(sampleResult "hello") ]))
                )
                let args = wakeArgs unusedConfig "sess-ok" "pack"
                match GrokBotRunner.wake args with
                | Error err -> Assert.Fail($"wake: {err}")
                | Ok() ->
                    match
                        GrokBotRunner.streamUntilComplete
                            (streamArgs "sess-ok")
                            collectFold
                    with
                    | Ok(result, seen) ->
                        Assert.Equal("hello", result.Text)
                        let expected =
                            [ AssistantText "hel"
                              AssistantText "lo"
                              RunFinished(sampleResult "hello") ]
                        Assert.Equal<AgentStreamEvent list>(
                            expected, List.rev seen)
                    | Error err ->
                        Assert.Fail($"stream: {err}"))

    [<Fact>]
    member _.``cancel mid-stream yields cancelled not Finish``() =
        withFake hangingLateFinish (fun () ->
            Assert.True(
                GrokBotRunner.setFakeStream (Some keptTextOnly))
            match
                GrokBotRunner.wake
                    (wakeArgs unusedConfig "sess-c" "pack")
            with
            | Error err -> Assert.Fail($"wake: {err}")
            | Ok() -> assertCancelledMidStream "sess-c")
