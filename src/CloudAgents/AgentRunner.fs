namespace Gambol.CloudAgents

open System.Threading

module AgentRunner =

    let start
        (config: RunnerConfig)
        (prompt: string)
        (repos: RepoConfig list option)
        (options: AgentOptions)
        : Result<string * string, AgentError> =
        Internal.CursorAdapter.startAgent config prompt repos options

    let poll
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        : Result<AgentStatus, AgentError> =
        Internal.CursorAdapter.pollStatus config agentId runId

    let cancel
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        : Result<unit, AgentError> =
        Internal.CursorAdapter.cancelRun config agentId runId

    let waitUntilComplete
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        (pollIntervalMs: int)
        (maxWaitMs: int option)
        : Result<AgentResult, AgentError> =

        let start = System.DateTime.UtcNow

        let rec loop () =
            match maxWaitMs with
            | Some max ->
                let elapsed =
                    (System.DateTime.UtcNow - start).TotalMilliseconds
                if elapsed > float max then
                    Error AgentError.Timeout
                else
                    pollOnce ()
            | None -> pollOnce ()

        and pollOnce () =
            match poll config agentId runId with
            | Error err -> Error err
            | Ok status ->
                match status with
                | Finished result -> Ok result
                | Cancelled ->
                    Error(
                        ApiError("cancelled", "Agent run was cancelled")
                    )
                | Failed msg -> Error(ApiError("failed", msg))
                | Creating
                | Running ->
                    Thread.Sleep pollIntervalMs
                    loop ()

        loop ()
