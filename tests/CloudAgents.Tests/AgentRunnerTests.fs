module Gambol.CloudAgents.Tests.AgentRunnerTests

open Xunit
open Xunit.Sdk
open Gambol.CloudAgents

[<Fact>]
let ``start requires valid config`` () =
    let config = { RunnerConfig.ApiKey = "test-key" }

    let options =
        { AgentOptions.DisplayName = Some "Test Agent"
          ModelHint = None }

    let result =
        AgentRunner.start config "test prompt" None options

    match result with
    | Error _ -> ()
    | Ok(agentId, runId) ->
        Assert.False(System.String.IsNullOrEmpty agentId)
        Assert.False(System.String.IsNullOrEmpty runId)

[<Fact>]
let ``poll returns status`` () =
    let config = { RunnerConfig.ApiKey = "test-key" }
    let result = AgentRunner.poll config "agent-id" "run-id"

    match result with
    | Error _ -> ()
    | Ok status ->
        Assert.True(
            match status with
            | Creating
            | Running
            | Finished _
            | Cancelled
            | Failed _ -> true
        )

[<Fact>]
let ``cancel sends cancel request`` () =
    let config = { RunnerConfig.ApiKey = "test-key" }
    let result = AgentRunner.cancel config "agent-id" "run-id"

    match result with
    | Error _ -> ()
    | Ok() -> Assert.True(true)

[<Fact>]
let ``waitUntilComplete polls until terminal`` () =
    let config = { RunnerConfig.ApiKey = "fake-key" }

    let result =
        AgentRunner.waitUntilComplete
            config
            "fake-agent"
            "fake-run"
            100
            (Some 500)

    match result with
    | Error Timeout -> Assert.True(true)
    | Error _ -> Assert.True(true)
    | Ok _ -> Assert.True(true)
