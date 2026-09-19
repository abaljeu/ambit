module Gambol.CloudAgents.Tests.AgentRunnerFakeTests

open System
open Xunit
open Gambol.CloudAgents

[<Collection("CloudAgents runner")>]
type AgentRunnerFakeTests() =

    let emptyOptions =
        { AgentOptions.DisplayName = None
          ModelHint = None }

    let unusedConfig = { RunnerConfig.ApiKey = "unused" }

    let sampleResult text =
        { AgentResult.Text = text
          Git = [] }

    let withFake handler body =
        Assert.True(AgentRunner.setFake (Some handler))
        try
            body ()
        finally
            Assert.True(AgentRunner.setFake None)

    [<Fact>]
    member _.``setFake Some routes start poll and wait without HTTP``() =
        let seen = ref None
        withFake
            (fun args ->
                seen := Some args.Prompt
                sampleResult "fake-reply")
            (fun () ->
                let started =
                    AgentRunner.start
                        unusedConfig "pack-body" None emptyOptions
                match started with
                | Error err -> Assert.Fail($"start: {err}")
                | Ok(agentId, runId) ->
                    Assert.False(String.IsNullOrEmpty agentId)
                    Assert.False(String.IsNullOrEmpty runId)
                    Assert.Equal(Some "pack-body", !seen)
                    match AgentRunner.poll unusedConfig agentId runId with
                    | Ok(Finished result) ->
                        Assert.Equal("fake-reply", result.Text)
                    | other -> Assert.Fail($"poll: {other}")
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
        | Error _ -> ()
        | Ok _ -> Assert.Fail("expected live call reject")

    [<Fact>]
    member _.``setFake refuses while a handler is in flight``() =
        let refused = ref false
        withFake
            (fun _ ->
                refused := not (AgentRunner.setFake None)
                sampleResult "during")
            (fun () ->
                let started =
                    AgentRunner.start
                        unusedConfig "x" None emptyOptions
                match started with
                | Ok _ -> Assert.True(!refused)
                | Error err -> Assert.Fail($"start: {err}"))
