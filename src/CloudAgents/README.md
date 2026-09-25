# CloudAgents Library

Vendor-neutral agent runner library with Cursor Cloud Agents as an
implementation detail.

## Design

The library provides a vendor-neutral public API.

### Public API

- **Types** (`PublicTypes.fs`): `AgentStatus`, `AgentResult`,
  `GitResult`, `RepoConfig`, `AgentOptions`, `ModelParam`,
  `RunnerConfig`, `StreamArgs`, `StreamFold`, `AgentStreamEvent`,
  `GrokBotConfig`, `GrokBotWakeArgs`, `GrokBotStreamArgs`
- **Runner** (`AgentRunner.fs`): `start`, `cancel`,
  `streamUntilComplete`, `setFake`, `setFakeStream`
- **Grok Bot oneshot** (`GrokBotRunner.fs`): `wake`, `cancel`,
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
- **Grok Bot Adapter** (`Internal/`): Oneshot wake POST + Unsettled
  stream seam (not Cursor HTTP; not a keep-alive inbox)
  - `GrokBotHttp.fs` - ack-only wake POST and auth seam
  - `GrokBotAdapter.fs` - maps public oneshot types to HTTP / errors

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

## Grok Bot oneshot

Sibling face to Cursor `AgentRunner`. One wake, one streamed
response, then terminate (`RunFinished`). The next query is a new
oneshot. This is not a keep-alive inbox wire and does not change
`AgentRunner.start` or Cursor HTTP.

Bot-channel Server wiring (CoreActorPool, inbound deliver door,
Run Agent Actor `gbot`) stays out of this library slice.

### Wake

`GrokBotRunner.wake` POSTs an ack-only webhook. The HTTP response
body is never bot reply text (`GrokBotHttp.interpretWakeResponse`).
Empty `WakeUrl` fails without sending and without writing secrets.

The library stays settings-blind. The caller binds User Secrets
keys `grokbot:WakeUrl`, `grokbot:WakeSecret`, and
`grokbot:InboundSecret` into `GrokBotConfig`. `InboundSecret` is
unused here (Server deliver door).

Wake auth header is `X-Ambit-Wake-Secret` with the configured
`WakeSecret` value. Empty `WakeSecret` fails closed without
sending (same class as empty `WakeUrl`).

Wake JSON follows the bot-channel map payload: `source`,
`kind: message`, `sentAt`, `commandId`, `focusId`, `sessionId`,
`text`, empty `payload`. Secrets are not in the body.

### Stream and Done seam

`streamUntilComplete` folds shared `AgentStreamEvent` values until
`RunFinished`. Response-concluded is Unsettled (`kind: close` may
be it). Until locked, `setFakeStream` emits harness `RunFinished`.
Live `streamUntilComplete` without a fake returns
`InvalidResponse` (Done seam Unsettled). Do not invent inbound
body fields for the Server deliver door here.

### Cancel

`cancel` aborts the oneshot fold mid-stream. That is not a success
`RunFinished`. There is no close-notify wake to the hub.

### Usage (fake)

```fsharp
open Gambol.CloudAgents

let config =
    { GrokBotConfig.WakeUrl = "https://unused.example/"
      WakeSecret = ""
      InboundSecret = "" }

let args =
    { GrokBotWakeArgs.Config = config
      Text = "Focus extract"
      CommandId = "cmd"
      FocusId = "focus"
      SessionId = "session" }

let fold = { Seed = []; OnEvent = fun seen ev -> ev :: seen }

GrokBotRunner.setFake (Some (fun _ ->
    Finished { Text = "hello"; Git = [] }))
|> ignore
GrokBotRunner.setFakeStream (Some (fun _ ->
    [ AssistantText "hel"
      AssistantText "lo"
      RunFinished { Text = "hello"; Git = [] } ]))
|> ignore

match GrokBotRunner.wake args with
| Error err -> printfn "Wake failed: %A" err
| Ok() ->
    let stream =
        { GrokBotStreamArgs.Config = config
          SessionId = args.SessionId
          PollIntervalMs = 50
          MaxWaitMs = Some 2000 }
    match GrokBotRunner.streamUntilComplete stream fold with
    | Ok(result, _) -> printfn "Result: %s" result.Text
    | Error err -> printfn "Error: %A" err
```

## Testing

Tests use the public API surface and do not require live Cursor API
credentials or a live Grok Bot hub for basic type/shape and fake
oneshot tests. See `tests/CloudAgents.Tests/`.

## Authentication

The library expects a Cursor Dashboard API key in `RunnerConfig.ApiKey`.
Get your key from: https://cursor.com/settings

Keys are not embedded in the library or repository.
