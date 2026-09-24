namespace Gambol.CloudAgents

open System
open System.Threading

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

    let poll
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        : Result<AgentStatus, AgentError> =
        match AgentRunnerFake.statusHandler () with
        | Some _ -> AgentRunnerFake.pollFake agentId runId
        | None ->
            AgentRunnerFake.withFlight (fun () ->
                Internal.CursorAdapter.pollStatus
                    config agentId runId)

    let cancel
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        : Result<unit, AgentError> =
        match AgentRunnerFake.statusHandler () with
        | Some _ -> AgentRunnerFake.cancelFake agentId runId
        | None ->
            Internal.CursorAdapter.cancelRun config agentId runId

    let private cancelledRun () =
        Error(ApiError("cancelled", "Agent run was cancelled"))

    let private pastDeadline (started: DateTime) maxWaitMs =
        match maxWaitMs with
        | None -> false
        | Some max ->
            let elapsed =
                (DateTime.UtcNow - started).TotalMilliseconds
            elapsed > float max

    let private waitLive
        config
        agentId
        runId
        (pollIntervalMs: int)
        maxWaitMs
        : Result<AgentResult, AgentError> =
        let started = DateTime.UtcNow

        let rec loop () =
            if pastDeadline started maxWaitMs then
                Error AgentError.Timeout
            else
                pollOnce ()

        and pollOnce () =
            match poll config agentId runId with
            | Error err -> Error err
            | Ok status ->
                match status with
                | Finished result -> Ok result
                | Cancelled -> cancelledRun ()
                | Failed msg -> Error(ApiError("failed", msg))
                | Creating
                | Running ->
                    Thread.Sleep pollIntervalMs
                    loop ()

        loop ()

    let waitUntilComplete
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        (pollIntervalMs: int)
        (maxWaitMs: int option)
        : Result<AgentResult, AgentError> =
        match AgentRunnerFake.statusHandler () with
        | Some _ ->
            waitLive
                config agentId runId pollIntervalMs maxWaitMs
        | None ->
            AgentRunnerFake.withFlight (fun () ->
                waitLive
                    config agentId runId pollIntervalMs maxWaitMs)

    let streamUntilComplete
        (args: StreamArgs)
        (fold: StreamFold<'a>)
        : Result<AgentResult * 'a, AgentError> =
        match AgentRunnerFake.statusHandler () with
        | Some _ -> AgentRunnerFake.streamFake args fold
        | None ->
            AgentRunnerFake.withFlight (fun () ->
                Internal.CursorAdapter.streamRun args fold)
