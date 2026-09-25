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

    let streamOnce agentId runId =
        AgentRunner.streamUntilComplete
            { Config = unusedConfig
              AgentId = agentId
              RunId = runId
              PollIntervalMs = 10
              MaxWaitMs = Some 2000 }
            { Seed = ()
              OnEvent = fun () _ -> () }

    [<Fact>]
    member _.``setFake Some routes start and stream without HTTP``() =
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
                    match streamOnce agentId runId with
                    | Ok(result, _) ->
                        Assert.Equal("fake-reply", result.Text)
                    | Error err -> Assert.Fail($"stream: {err}")
                    Assert.Equal(Some "pack-body", !seen))

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
                    match streamOnce agentId runId with
                    | Error(ApiError("failed", msg)) ->
                        Assert.Equal(named, msg)
                    | other -> Assert.Fail($"expected failed, {other}"))

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
                    streamOnce agentId runId |> ignore)

    [<Fact>]
    member _.``hanging fake stream waits until cancel``() =
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
                    let seen = ref "none"
                    let worker =
                        Thread(fun () ->
                            match streamOnce agentId runId with
                            | Error(ApiError("cancelled", _)) ->
                                seen := "cancelled"
                            | _ -> seen := "other")
                    worker.Start()
                    match
                        AgentRunner.cancel unusedConfig agentId runId
                    with
                    | Ok() -> ()
                    | Error err -> Assert.Fail($"cancel: {err}")
                    Assert.True(worker.Join 2000)
                    Assert.Equal("cancelled", !seen)
                    Assert.True(AgentRunner.fakeCancelCount() >= 1))

    [<Fact>]
    member _.``streamUntilComplete emits fake stream events``() =
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
                            { Config = unusedConfig
                              AgentId = agentId
                              RunId = runId
                              PollIntervalMs = 10
                              MaxWaitMs = None }
                            { Seed = []
                              OnEvent = fun seen ev -> ev :: seen }
                    with
                    | Ok(result, seen) ->
                        Assert.Equal("hello", result.Text)
                        let expected =
                            [ AssistantText "hel"
                              AssistantText "lo"
                              RunFinished(sampleResult "hello") ]
                        Assert.Equal<AgentStreamEvent list>(
                            expected, List.rev seen)
                    | Error err -> Assert.Fail($"stream: {err}"))

    [<Fact>]
    member _.``partial fake stream waits until cancel``() =
        withFake
            (fun _ ->
                AgentRunner.waitForCancel 8000 |> ignore
                Finished(sampleResult "late"))
            (fun () ->
                Assert.True(
                    AgentRunner.setFakeStream (Some (fun _ ->
                        [ AssistantText "kept" ]))
                )
                let started =
                    AgentRunner.start
                        unusedConfig "pack" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    let sawText = new ManualResetEvent(false)
                    let seen = ref "none"
                    let worker =
                        Thread(fun () ->
                            match
                                AgentRunner.streamUntilComplete
                                    { Config = unusedConfig
                                      AgentId = agentId
                                      RunId = runId
                                      PollIntervalMs = 10
                                      MaxWaitMs = None }
                                    { Seed = ()
                                      OnEvent =
                                        fun () ev ->
                                            match ev with
                                            | AssistantText _ ->
                                                sawText.Set()
                                                |> ignore
                                            | _ -> () }
                            with
                            | Error(ApiError("cancelled", _)) ->
                                seen := "cancelled"
                            | _ -> seen := "other")
                    worker.Start()
                    Assert.True(sawText.WaitOne 2000)
                    match
                        AgentRunner.cancel
                            unusedConfig agentId runId
                    with
                    | Ok() -> ()
                    | Error err -> Assert.Fail($"cancel: {err}")
                    Assert.True(worker.Join 2000)
                    Assert.Equal("cancelled", !seen))

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
                    match streamOnce agentId runId with
                    | Error(ApiError("failed", msg)) ->
                        Assert.Equal("provider-boom", msg)
                    | other ->
                        Assert.Fail($"expected failed stream, {other}"))

