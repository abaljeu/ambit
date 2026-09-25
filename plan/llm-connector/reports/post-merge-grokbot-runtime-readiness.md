# Post-merge Grok Bot runtime readiness

Verdict: **not ready**. Documented secrets are not enough to run a live Grok Bot oneshot in the Gambol program. Console and Server differ.

## Checks

Read [doc/reference/secrets.md](../../../doc/reference/secrets.md), [23 — Outbound Cursor API key secrets](../issues/23-outbound-cursor-api-key-secrets.md), [GrokBotSettings.fs](../../../src/Server/GrokBotSettings.fs), [RouteRegistration.fs](../../../src/Server/RouteRegistration.fs), [RunAgentActor.fs](../../../src/Server/RunAgentActor.fs), [GrokBotRunner.fs](../../../src/CloudAgents/GrokBotRunner.fs), [GrokBotAdapter.fs](../../../src/CloudAgents/Internal/GrokBotAdapter.fs), [GrokBotHttp.fs](../../../src/CloudAgents/Internal/GrokBotHttp.fs), [CloudAgents.Console Program.fs](../../../src/CloudAgents.Console/Program.fs) / [Config.fs](../../../src/CloudAgents.Console/Config.fs), tracked appsettings, launch settings. [doc/reference/browser-reg.md](../../../doc/reference/browser-reg.md) is Cursor Origin registration and is unused on this path. Did not call an external hub and did not read real secret values. Did not finish a `dotnet test` pass in this write-up; the live Unsettled stream and missing Console path are in source, not in test-only comments.

## Secret input to HTTP

Development Server (and Desktop sharing the same host) loads User Secrets `a6b5ead7-8f2d-4482-acb0-0232585d7c6e` after env JSON. Production uses Azure App Settings, not tracked JSON and not `/home/appsettings.Production.json`. Bind names:

| Store name | Azure / env |
| --- | --- |
| `grokbot:WakeUrl` | `grokbot__WakeUrl` |
| `grokbot:WakeSecret` | `grokbot__WakeSecret` |
| `grokbot:InboundSecret` | `grokbot__InboundSecret` |

`GrokBotSettings.fromConfig` copies those three strings into `GrokBotConfig`. Tracked [src/Server/appsettings.json](../../../src/Server/appsettings.json) and [src/appsettings.Development.json](../../../src/appsettings.Development.json) have no `grokbot` section; empty bind is `""`. `CreateBoot` passes that config into Actor `ai`. First behavior token `gbot` (`?ai gbot`) calls `GrokBotRunner.wake`, then `streamUntilComplete`. Cursor `AiKeys` / `CURSOR_API_KEY` / `--api-key` are unused on this branch. There is no Grok model catalog or `--model` for Grok Bot.

Wake POST: `HttpClient` POST to `WakeUrl`, JSON `source=ambit`, `kind=message`, `sentAt`, `commandId`, `focusId`, `sessionId`, `text`, empty `payload`. Success is HTTP 2xx ack only (body is not reply text). `applyWakeAuth` takes `WakeSecret` and adds no header. `InboundSecret` is unused. There is no `/ambit/actors/deliver` (or other inbound) route.

## Console

**Not ready.** [CloudAgents.Console](../../../src/CloudAgents.Console/Program.fs) is Cursor-only (`AgentRunner`, `AiKeys:desktop` / `CURSOR_API_KEY`). It never reads `grokbot:*` and never calls `GrokBotRunner`. Launch settings only set a working directory.

## Server / program

**Not ready for a complete run.** Wiring exists: secrets bind, `?ai gbot` selects Grok, wake can POST if `WakeUrl` is nonempty. After a successful wake, live `GrokBotAdapter.streamRun` always returns `InvalidResponse` (`stream Done seam Unsettled`). Tests that Finish use `GrokBotRunner.setFake` / `setFakeStream`. Empty `WakeUrl` fails closed (`missing wake URL`) without leaking secrets.

## Blockers

1. Live inbound / Done stream is Unsettled. After wake, Server `?ai gbot` cannot collect bot text or Finish. `InboundSecret` has no door.
2. Console cannot start Grok Bot at all. Secrets do not change that.
3. `WakeSecret` is stored and passed into HTTP but not sent. A hub that requires wake auth will reject the POST even with the documented pair filled.
