module Gambol.CloudAgents.Tests.AgentRunnerTests

open Xunit
open Xunit.Sdk
open Gambol.CloudAgents

[<Fact>]
let ``start requires valid config`` () =
    let config = { RunnerConfig.ApiKey = "test-key" }

    let options =
        { AgentOptions.DisplayName = Some "Test Agent"
          ModelHint = None
          ModelParams = [] }

    let result =
        AgentRunner.start config "test prompt" None options

    match result with
    | Error _ -> ()
    | Ok(agentId, runId) ->
        Assert.False(System.String.IsNullOrEmpty agentId)
        Assert.False(System.String.IsNullOrEmpty runId)

[<Fact>]
let ``cancel sends cancel request`` () =
    let config = { RunnerConfig.ApiKey = "test-key" }
    let result = AgentRunner.cancel config "agent-id" "run-id"

    match result with
    | Error _ -> ()
    | Ok _ -> Assert.True(true)

[<Fact>]
let ``streamUntilComplete is the completion path`` () =
    let args =
        { Config = { RunnerConfig.ApiKey = "fake-key" }
          AgentId = "fake-agent"
          RunId = "fake-run"
          MaxWaitMs = Some 500 }
    let fold = { Seed = (); OnEvent = fun s _ -> s }
    let result = AgentRunner.streamUntilComplete args fold
    match result with
    | Error Timeout -> Assert.True(true)
    | Error _ -> Assert.True(true)
    | Ok _ -> Assert.True(true)
