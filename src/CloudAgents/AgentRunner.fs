namespace Gambol.CloudAgents

module AgentRunner =

    let setFake
        (handler: (StartArgs -> AgentStatus) option)
        : bool =
        AgentRunnerFake.setFake handler

    /// Optional fake stream sequence (requires setFake Some).
    let setFakeStream
        (handler: (StartArgs -> AgentStreamEvent list) option)
        : bool =
        AgentRunnerFake.setFakeStream handler

    /// Block a setFake handler until cancel, or until timeoutMs.
    let waitForCancel (timeoutMs: int) : bool =
        AgentRunnerFake.waitForCancel timeoutMs

    let fakeCancelCount () = AgentRunnerFake.fakeCancelCount ()

    let start
        (config: RunnerConfig)
        (prompt: string)
        (repos: RepoConfig list option)
        (options: AgentOptions)
        : Result<string * string, AgentError> =
        match AgentRunnerFake.statusHandler () with
        | Some f ->
            AgentRunnerFake.startFake f config prompt repos options
        | None ->
            AgentRunnerFake.withFlight (fun () ->
                Internal.CursorAdapter.startAgent
                    config prompt repos options)

    let cancel
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        : Result<unit, AgentError> =
        match AgentRunnerFake.statusHandler () with
        | Some _ -> AgentRunnerFake.cancelFake agentId runId
        | None ->
            Internal.CursorAdapter.cancelRun config agentId runId

    let streamUntilComplete
        (args: StreamArgs)
        (fold: StreamFold<'a>)
        : Result<AgentResult * 'a, AgentError> =
        match AgentRunnerFake.statusHandler () with
        | Some _ -> AgentRunnerFake.streamFake args fold
        | None ->
            AgentRunnerFake.withFlight (fun () ->
                Internal.CursorAdapter.streamRun args fold)
