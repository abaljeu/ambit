module Gambol.CloudAgents.Tests.AgentRunnerFakeTests

open System
open System.Threading
open Xunit
open Gambol.CloudAgents

[<Collection("CloudAgents runner")>]
type AgentRunnerFakeTests() =

    let emptyOptions =
        { AgentOptions.DisplayName = None
          ModelHint = None
          ModelParams = [] }

    let unusedConfig = { RunnerConfig.ApiKey = "unused" }

    let sampleResult text =
        { AgentResult.Text = text
          Git = [] }

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
        Assert.True(AgentRunner.setFake (Some handler))
        try
            body ()
        finally
            clearFake ()

    let waitFinished agentId runId remainingMs =
        let rec spin left =
            match AgentRunner.poll unusedConfig agentId runId with
            | Ok(Finished result) -> Some result
            | Ok Running when left > 0 ->
                Thread.Sleep 10
                spin (left - 10)
            | _ -> None
        spin remainingMs

    let waitFailed agentId runId remainingMs =
        let rec spin left =
            match AgentRunner.poll unusedConfig agentId runId with
            | Ok(Failed msg) -> Some msg
            | Ok Running when left > 0 ->
                Thread.Sleep 10
                spin (left - 10)
            | _ -> None
        spin remainingMs

    [<Fact>]
    member _.``setFake Some routes start poll and wait without HTTP``() =
        let seen = ref None
        withFake
            (fun args ->
                seen := Some args.Prompt
                Finished(sampleResult "fake-reply"))
            (fun () ->
                let started =
                    AgentRunner.start
                        unusedConfig "pack-body" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    Assert.False(String.IsNullOrEmpty agentId)
                    Assert.False(String.IsNullOrEmpty runId)
                    match waitFinished agentId runId 2000 with
                    | None -> Assert.Fail("poll did not finish")
                    | Some result ->
                        Assert.Equal("fake-reply", result.Text)
                    Assert.Equal(Some "pack-body", !seen)
                    match
                        AgentRunner.waitUntilComplete
                            unusedConfig agentId runId 10 None
                    with
                    | Ok result ->
                        Assert.Equal("fake-reply", result.Text)
                    | Error err -> Assert.Fail($"wait: {err}"))

    [<Fact>]
    member _.``setFake None restores CursorAdapter reject``() =
        Assert.True(AgentRunner.setFake None)
        let result =
            AgentRunner.start
                { RunnerConfig.ApiKey = "" }
                "live-reject"
                None
                emptyOptions
        match result with
        | Error(AuthenticationFailed msg) ->
            Assert.Equal(
                AgentMessage.couldNotSend "Cursor" "missing key",
                msg)
        | other -> Assert.Fail($"expected missing-key auth, {other}")

    [<Fact>]
    member _.``setFake yields Failed unauthorized named Cursor``() =
        let named =
            AgentMessage.couldNotSend "Cursor" "unauthorized"
        withFake
            (fun _ -> Failed named)
            (fun () ->
                let started =
                    AgentRunner.start
                        unusedConfig "pack" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    match waitFailed agentId runId 2000 with
                    | None -> Assert.Fail("poll did not fail")
                    | Some msg -> Assert.Equal(named, msg))

    [<Fact>]
    member _.``setFake refuses while a handler is in flight``() =
        let refused = ref false
        withFake
            (fun _ ->
                refused := not (AgentRunner.setFake None)
                Finished(sampleResult "during"))
            (fun () ->
                let started =
                    AgentRunner.start
                        unusedConfig "x" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    let deadline = DateTime.UtcNow.AddSeconds 2.0
                    while not !refused && DateTime.UtcNow < deadline do
                        Thread.Sleep 10
                    Assert.True(!refused)
                    waitFinished agentId runId 2000 |> ignore)

    [<Fact>]
    member _.``hanging fake stays Running until cancel``() =
        withFake
            (fun _ ->
                AgentRunner.waitForCancel 8000 |> ignore
                Finished(sampleResult "late"))
            (fun () ->
                let started =
                    AgentRunner.start
                        unusedConfig "hang" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    match AgentRunner.poll unusedConfig agentId runId with
                    | Ok Running -> ()
                    | other -> Assert.Fail($"expected Running, {other}")
                    match
                        AgentRunner.cancel unusedConfig agentId runId
                    with
                    | Ok() -> ()
                    | Error err -> Assert.Fail($"cancel: {err}")
                    match AgentRunner.poll unusedConfig agentId runId with
                    | Ok Cancelled -> ()
                    | other ->
                        Assert.Fail($"expected Cancelled, {other}")
                    Assert.True(AgentRunner.fakeCancelCount() >= 1)
                    match
                        AgentRunner.waitUntilComplete
                            unusedConfig agentId runId 10 (Some 500)
                    with
                    | Error(ApiError("cancelled", _)) -> ()
                    | other ->
                        Assert.Fail($"expected cancelled wait, {other}"))

    [<Fact>]
    member _.``streamUntilComplete emits fake stream events``() =
        let deltas = ref []
        withFake
            (fun _ -> Finished(sampleResult "ignored"))
            (fun () ->
                Assert.True(
                    AgentRunner.setFakeStream (Some (fun _ ->
                        [ AssistantText "hel"
                          AssistantText "lo"
                          RunFinished(sampleResult "hello") ]))
                )
                let started =
                    AgentRunner.start
                        unusedConfig "pack" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    match
                        AgentRunner.streamUntilComplete
                            unusedConfig
                            agentId
                            runId
                            10
                            None
                            (fun ev -> deltas := ev :: !deltas)
                    with
                    | Ok result ->
                        Assert.Equal("hello", result.Text)
                        let expected =
                            [ AssistantText "hel"
                              AssistantText "lo"
                              RunFinished(sampleResult "hello") ]
                        Assert.Equal<AgentStreamEvent list>(expected, List.rev !deltas)
                    | Error err -> Assert.Fail($"stream: {err}"))

    [<Fact>]
    member _.``setFake handler yields Failed not Finished``() =
        withFake
            (fun _ -> Failed "provider-boom")
            (fun () ->
                let started =
                    AgentRunner.start
                        unusedConfig "pack" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    match waitFailed agentId runId 2000 with
                    | None -> Assert.Fail("poll did not fail")
                    | Some msg ->
                        Assert.Equal("provider-boom", msg)
                    match
                        AgentRunner.waitUntilComplete
                            unusedConfig agentId runId 10 None
                    with
                    | Error(ApiError("failed", msg)) ->
                        Assert.Equal("provider-boom", msg)
                    | other ->
                        Assert.Fail($"expected failed wait, {other}"))

