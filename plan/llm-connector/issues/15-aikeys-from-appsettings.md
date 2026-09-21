# 15 — AiKeys from appsettings

**Status:** done
**Blocked by:** None — [14 — Provider-named AI errors](14-provider-named-ai-errors.md) is `done`.
**Type:** task
Estimate: 2h
Actual: 2h

## Context

[02 — Which LLM and where credentials live](02-which-llm-and-credentials.md) locked keys under **`AiKeys`** in Server `appsettings*.json`. [RunAgentActor](../../../src/Server/RunAgentActor.fs) still reads `CURSOR_API_KEY` from the environment. CloudAgents must stay settings-blind: it only receives `RunnerConfig.ApiKey`. Missing or empty key stays the ticket 14 path (Cursor `missing key` / `unauthorized`).

## JSON shape

Section name **`AiKeys`**. Array of objects:

- `Name` — keyname. `?ai <Name>` selects this entry. Match is case-insensitive.
- `ApiKey` — secret string. Committed files use `""` only.

Default when `?ai` has no keyname: the **first** array entry. A missing Name, an empty list, or an empty `ApiKey` yields `""` (CloudAgents names Cursor). Extra tokens after the keyname stay unused. Cursor Grok/High/Fast options are not in this ticket.

Load order is existing `addAppSettings`: base `appsettings.json`, then `appsettings.Development.json` or gitignored `appsettings.Production.json`.

## What to build

### 1. Bind and resolve

1. [x] Server helper binds `AiKeys` from `IConfiguration` (same pattern as `RouteAuthentication.create` + `Auth:Username`).
2. [x] Resolve named entry, first-entry default, and missing → `""`.
3. [x] Parse optional keyname from Command text `?ai <keyname> …` on the Actor start path (`ActorInput` already has Command Node text).

### 2. Actor uses the resolved string

1. [x] Composition injects the bound list into Run Agent Actor registration. Core does not see keys.
2. [x] Run Agent Actor passes the resolved string into `RunnerConfig.ApiKey` only.
3. [x] Remove `CURSOR_API_KEY` from Run Agent Actor. CloudAgents does not read appsettings.

### 3. Placeholder JSON

1. [x] `appsettings.json` and `appsettings.Development.json` hold an empty-`ApiKey` example. No real secret. Production stays gitignored.

### 4. Non-goals

1. CloudAgents settings or model-option defaults.
2. Env fallback for `CURSOR_API_KEY`.
3. Graph, cookie, or DataDir key storage.

## See also

[llm-connector architecture](../arch.md), [02 — Which LLM and where credentials live](02-which-llm-and-credentials.md), [14 — Provider-named AI errors](14-provider-named-ai-errors.md)

## Comments

- 2026-09-19 — Independent review Good; squash-landed on staging. Status `done`.
- 2026-09-20 — Filed from locked AiKeys placement. Default is first entry. Fields are `Name` + `ApiKey`.
- 2026-09-20 — Coded: `AiKeys.fromConfig` / `resolve`; `?ai` keyname from Command text; composition injects into Run Agent Actor; `CURSOR_API_KEY` removed. Status `coded`.

## Time

- 2026-09-20 2h — Ticket, AiKeys bind/resolve, Actor injection, placeholder JSON, focused tests (from chat)
