namespace Gambol.CloudAgents.Internal

open Gambol.CloudAgents

module CursorAdapter =

    let private mapGitResult
        (git: CursorTypes.CursorGit option)
        : GitResult list =
        match git with
        | None -> []
        | Some g ->
            g.branches
            |> List.map (fun b ->
                { GitResult.RepoUrl = b.repoUrl
                  Branch = b.branch
                  PullRequestUrl = b.prUrl })

    let private mapStatus
        (status: CursorTypes.CursorRunStatus)
        : AgentStatus =
        match status.status.ToUpperInvariant() with
        | "CREATING" -> Creating
        | "RUNNING" -> Running
        | "FINISHED" ->
            let text = status.result |> Option.defaultValue ""
            let git = mapGitResult status.git
            Finished { Text = text; Git = git }
        | "CANCELLED" -> Cancelled
        | "ERROR" ->
            let msg = status.result |> Option.defaultValue "Unknown error"
            Failed msg
        | "EXPIRED" -> Failed "Run expired"
        | other -> Failed $"Unknown status: {other}"

    let startAgent
        (config: RunnerConfig)
        (prompt: string)
        (repos: RepoConfig list option)
        (options: AgentOptions)
        : Result<string * string, AgentError> =

        let cursorRepos =
            repos
            |> Option.map (fun rs ->
                rs
                |> List.map (fun r ->
                    { CursorTypes.CursorRepo.url = r.Url
                      CursorTypes.CursorRepo.startingRef =
                          r.StartingRef }))

        let request: CursorTypes.CursorCreateRequest =
            { prompt = { text = prompt }
              name = options.DisplayName
              repos = cursorRepos }

        match CursorHttp.createAgent config.ApiKey request with
        | Error msg -> Error(NetworkError msg)
        | Ok response -> Ok(response.agent.id, response.run.id)

    let pollStatus
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        : Result<AgentStatus, AgentError> =

        match CursorHttp.getRunStatus config.ApiKey agentId runId with
        | Error msg -> Error(NetworkError msg)
        | Ok status -> Ok(mapStatus status)

    let cancelRun
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        : Result<unit, AgentError> =

        match CursorHttp.cancelRun config.ApiKey agentId runId with
        | Error msg -> Error(NetworkError msg)
        | Ok() -> Ok()
