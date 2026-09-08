# CloudAgents Library

Vendor-neutral agent runner library with Cursor Cloud Agents as an
implementation detail.

## Design

The library provides a vendor-neutral public API with no references to
Cursor-specific types, URLs, or identifiers in the public surface.

### Public API

- **Types** (`PublicTypes.fs`): `AgentStatus`, `AgentResult`,
  `GitResult`, `RepoConfig`, `AgentOptions`, `RunnerConfig`
- **Runner** (`AgentRunner.fs`): `start`, `poll`, `cancel`,
  `waitUntilComplete`

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
      ModelHint = None }

match AgentRunner.start config "Explain F# computation expressions" None options with
| Error err -> printfn "Failed: %A" err
| Ok (agentId, runId) ->
    match AgentRunner.waitUntilComplete config agentId runId 5000 None with
    | Ok result -> printfn "Result: %s" result.Text
    | Error err -> printfn "Error: %A" err
```

### With repository (optional)

```fsharp
open Gambol.CloudAgents

let config = { RunnerConfig.ApiKey = "your-cursor-api-key" }

let options =
    { AgentOptions.DisplayName = Some "Add Tests"
      ModelHint = None }

let repos =
    Some [ { RepoConfig.Url = "https://github.com/org/repo"
             StartingRef = Some "main" } ]

match AgentRunner.start config "Add unit tests" repos options with
| Error err -> printfn "Failed: %A" err
| Ok (agentId, runId) ->
    match AgentRunner.waitUntilComplete config agentId runId 5000 None with
    | Ok result ->
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
