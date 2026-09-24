namespace Gambol.CloudAgents.Internal

open System
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
        | "ERROR" -> Failed "error"
        | "EXPIRED" -> Failed "expired"
        | _ -> Failed "unknown status"

    let private providerName = "Cursor"

    let private authFailed reason =
        AuthenticationFailed(
            AgentMessage.couldNotSend providerName reason)

    let private fromHttpError apiKey httpError =
        if String.IsNullOrWhiteSpace apiKey then
            authFailed "missing key"
        elif httpError = "unauthorized" then
            authFailed "unauthorized"
        else
            NetworkError httpError

    let startAgent
        (config: RunnerConfig)
        (prompt: string)
        (repos: RepoConfig list option)
        (options: AgentOptions)
        : Result<string * string, AgentError> =

        if String.IsNullOrWhiteSpace config.ApiKey then
            Error (authFailed "missing key")
        else
            let cursorRepos =
                repos
                |> Option.map (fun rs ->
                    rs
                    |> List.map (fun r ->
                        { CursorTypes.CursorRepo.url = r.Url
                          CursorTypes.CursorRepo.startingRef =
                              r.StartingRef }))
            let modelRef =
                match options.ModelHint with
                | None -> None
                | Some id ->
                    let ps =
                        options.ModelParams
                        |> List.map (fun p ->
                            { CursorTypes.CursorParamAssignment.id =
                                p.Id
                              CursorTypes.CursorParamAssignment.value =
                                  p.Value })
                    Some
                        { CursorTypes.CursorModelRef.id = id
                          CursorTypes.CursorModelRef.``params`` = ps }
            let request: CursorTypes.CursorCreateRequest =
                { prompt = { text = prompt }
                  name = options.DisplayName
                  model = modelRef
                  repos = cursorRepos }
            match CursorHttp.createAgent config.ApiKey request with
            | Error msg -> Error(fromHttpError config.ApiKey msg)
            | Ok response -> Ok(response.agent.id, response.run.id)

    let pollStatus
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        : Result<AgentStatus, AgentError> =

        if String.IsNullOrWhiteSpace config.ApiKey then
            Error (authFailed "missing key")
        else
            match CursorHttp.getRunStatus config.ApiKey agentId runId with
            | Error msg -> Error(fromHttpError config.ApiKey msg)
            | Ok status -> Ok(mapStatus status)

    let cancelRun
        (config: RunnerConfig)
        (agentId: string)
        (runId: string)
        : Result<unit, AgentError> =

        match CursorHttp.cancelRun config.ApiKey agentId runId with
        | Error msg -> Error(NetworkError msg)
        | Ok() -> Ok()

    let private fromStreamBody (body: CursorHttp.StreamBody) =
        { Text = body.text
          Git = mapGitResult body.git }

    let streamRun (args: StreamArgs) (fold: StreamFold<'a>) =
        if String.IsNullOrWhiteSpace args.Config.ApiKey then
            Error(authFailed "missing key")
        else
            let onAssistant text state =
                fold.OnEvent
                    state
                    (AgentStreamEvent.AssistantText text)

            match
                CursorHttp.streamRun
                    args.Config.ApiKey
                    args.AgentId
                    args.RunId
                    onAssistant
                    fold.Seed
            with
            | Error msg, _ ->
                Error(fromHttpError args.Config.ApiKey msg)
            | Ok body, state ->
                let result = fromStreamBody body
                let finished =
                    fold.OnEvent
                        state
                        (AgentStreamEvent.RunFinished result)
                Ok(result, finished)
