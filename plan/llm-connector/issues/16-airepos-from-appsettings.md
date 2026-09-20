# 16 — AiRepos from appsettings

**Status:** coded
**Blocked by:** None — [15 — AiKeys from appsettings](15-aikeys-from-appsettings.md) is `done`.
**Type:** task
Estimate: 2h
Actual: 2h 15m

## Context

[15 — AiKeys from appsettings](15-aikeys-from-appsettings.md) bound API keys from Server `appsettings*.json` and selected them with `?ai` keyname. CloudAgents already accepts `RepoConfig list option` on `AgentRunner.start`. This ticket binds a parallel **`AiRepos`** name→url list and resolves an optional repo for the AI Actor. CloudAgents stays settings-blind: it only receives the resolved `RepoConfig list option`. CloudAgents.Console stays CLI-only (`--repo` / env); it does not read appsettings.

## JSON shape

Section name **`AiRepos`**. Array of objects, parallel to `AiKeys`:

- `Name` — short name to select. `?ai <keyname> <Name>` selects this entry. Match is case-insensitive.
- `Url` — git URL. Committed examples may be real public URLs; these are not secrets.
- `StartingRef` — optional. Omit or `""` means `None` (CloudAgents default).

Default when `?ai` has no reponame token: **no repos** (`None`). Do not auto-attach the first `AiRepos` entry.

Load order is existing `addAppSettings`: base `appsettings.json`, then `appsettings.Development.json` or gitignored `appsettings.Production.json`.

## Parse rules

Command text tokens after `?ai` (from [CommandRequest.behaviorFromText](../../../src/Shared/CommandRequest.fs), already lowercased):

1. No tokens (`?ai`) — default `AiKeys` entry (first), no repo.
2. Two or more tokens — first is keyname, second is reponame. Extra tokens stay unused.
3. One token that matches an `AiKeys` Name — keyname, no repo. A Name that exists on both lists is a keyname.
4. One token that matches an `AiRepos` Name but not an `AiKeys` Name — reponame with the default key.
5. One token that matches neither — keyname (unknown key → empty `ApiKey`, same as [15 — AiKeys from appsettings](15-aikeys-from-appsettings.md)).

Documented order is **keyname then reponame**. `?ai life` is a repo only when `life` is not also a key Name.

## What to build

### 1. Bind and resolve

1. [x] Server helper binds `AiRepos` from `IConfiguration` (same pattern as `AiKeys.fromConfig`).
2. [x] Resolve named entry to `Some [RepoConfig]`; omitted or missing name → `None`. Empty `StartingRef` → `None`.
3. [x] Parse optional keyname and reponame from Command text on the Actor start path.

### 2. Actor uses the resolved repos

1. [x] Composition injects the bound `AiRepos` list alongside `AiKeys` at Run Agent Actor registration. Core does not see repos.
2. [x] Run Agent Actor passes the resolved `RepoConfig list option` into `AgentRunner.start`.
3. [x] CloudAgents does not read appsettings.

### 3. Placeholder JSON

1. [x] `appsettings.json` and `appsettings.Development.json` hold a `life` example URL. No secrets.

### 4. Non-goals

1. CloudAgents.Console reading appsettings (stays `--repo` / env).
2. Model options Grok/High/Fast.
3. Changing `AiKeys` shape.

## See also

[llm-connector architecture](../arch.md), [15 — AiKeys from appsettings](15-aikeys-from-appsettings.md), [02 — Which LLM and where credentials live](02-which-llm-and-credentials.md)

## Comments

- 2026-09-20 — Filed and coded: `AiRepos.fromConfig` / `resolve`; `?ai` keyname then reponame; one-token repo-only UX; Actor injects resolved repos; CloudAgents stays settings-blind. Status `coded`.
- 2026-09-20 — `complete` takes existing CloudAgents `StartArgs`. Host `AiKeys` / `AiRepos` lists stay separate (no existing type). Status stays `coded`.

## Time

- 2026-09-20 2h — Ticket, AiRepos bind/resolve, `?ai` parse, Actor injection, placeholder JSON, focused tests (from chat)
- 2026-09-20 15m — Group `complete` args as `StartArgs` (from chat)
