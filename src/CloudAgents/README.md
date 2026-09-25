# CloudAgents Library

Vendor-neutral agent runner library with Cursor Cloud Agents as an
implementation detail.

## Design

The library provides a vendor-neutral public API.

### Public API

- **Types** (`PublicTypes.fs`): `AgentStatus`, `AgentResult`,
  `GitResult`, `RepoConfig`, `AgentOptions`, `ModelParam`,
  `RunnerConfig`, `StreamArgs`, `StreamFold`, `AgentStreamEvent`
- **Runner** (`AgentRunner.fs`): `start`, `cancel`,
  `streamUntilComplete`, `setFake`, `setFakeStream`
- **Catalog** (`cursor-models.json`): checked-in model ids,
  parameters, and allowed values; loaded via
  `Internal/CursorModelsFile.fs`

### Internal Implementation

- **Cursor Adapter** (`Internal/`): Maps vendor-neutral types to
  Cursor Cloud Agents API (https://api.cursor.com/v1)
  - `CursorTypes.fs` - Cursor API request/response DTOs
  - `CursorHttp.fs` - HTTP client for Cursor endpoints
  - `CursorAdapter.fs` - Maps public types to/from Cursor types

## Usage

### No-repo agent (primary use case)

```fsharp
open Gambol.CloudAgents

let config = { RunnerConfig.ApiKey = "your-cursor-api-key" }

let options =
    { AgentOptions.DisplayName = Some "Research Agent"
      ModelHint = None
      ModelParams = [] }

let fold = { Seed = (); OnEvent = fun s _ -> s }

match AgentRunner.start config "Explain F# computation expressions" None options with
| Error err -> printfn "Failed: %A" err
| Ok (agentId, runId) ->
    let args =
        { Config = config
          AgentId = agentId
          RunId = runId
          PollIntervalMs = 50
          MaxWaitMs = None }
    match AgentRunner.streamUntilComplete args fold with
    | Ok (result, _) -> printfn "Result: %s" result.Text
    | Error err -> printfn "Error: %A" err
```

### With repository (optional)

Gambol repo work: send finished git to `staging` ([[.agents/skills/cloud-agent-git/SKILL.md]]). The library still returns the vendor branch and PR URL; that is not the drop.

```fsharp
open Gambol.CloudAgents

let config = { RunnerConfig.ApiKey = "your-cursor-api-key" }

let options =
    { AgentOptions.DisplayName = Some "Add Tests"
      ModelHint = None
      ModelParams = [] }

let repos =
    Some [ { RepoConfig.Url = "https://github.com/org/repo"
             StartingRef = Some "main" } ]

let fold = { Seed = (); OnEvent = fun s _ -> s }

match AgentRunner.start config "Add unit tests" repos options with
| Error err -> printfn "Failed: %A" err
| Ok (agentId, runId) ->
    let args =
        { Config = config
          AgentId = agentId
          RunId = runId
          PollIntervalMs = 50
          MaxWaitMs = None }
    match AgentRunner.streamUntilComplete args fold with
    | Ok (result, _) ->
        printfn "Result: %s" result.Text
        for git in result.Git do
            printfn "Branch: %s" (git.Branch |> Option.defaultValue "none")
            match git.PullRequestUrl with
            | Some pr -> printfn "PR: %s" pr
            | None -> ()
    | Error err -> printfn "Error: %A" err
```

## Testing

Tests use the public API surface and do not require live Cursor API
credentials for basic type/shape tests. See `tests/CloudAgents.Tests/`.

## Authentication

The library expects a Cursor Dashboard API key in `RunnerConfig.ApiKey`.
Get your key from: https://cursor.com/settings

Keys are not embedded in the library or repository.
