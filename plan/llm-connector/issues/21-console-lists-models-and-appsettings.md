# 21 — Console lists models and fills CLI gaps from appsettings

**Status:** coded
**Blocked by:** None — [20 — Gitignore Development appsettings](20-gitignore-development-appsettings.md) is `done`.
**Type:** coding
Estimate: 2h
Actual: 2h

## Context

[CloudAgents.Console](../../../src/CloudAgents.Console/Program.fs) is a standalone diagnostic exe for Cursor create. Ready wires `ModelHint` as a JSON string on create. Cursor create expects `model` as `{ id, params? }` (omit `model` for the Cursor default). Alan needs the live catalog (`id` / displayName / variants) printed at start so a create id is real. Anything not on the CLI is read from `appsettings.<level>.json`. CloudAgents stays settings-blind. This is not a Browser / `?ai` model-selection UI.

[16 — AiRepos from appsettings](16-airepos-from-appsettings.md) left Console CLI-only. This ticket supersedes that non-goal for Console only.

## JSON shape (Console)

Level is `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT` (default `Development`). Load base `appsettings.json`, then overlay `appsettings.<level>.json`. Search the current directory, then the exe directory.

Tracked [appsettings.json](../../../src/CloudAgents.Console/appsettings.json) holds empty placeholders. `appsettings.Development.json` and `appsettings.Production.json` are gitignored like Server ([20 — Gitignore Development appsettings](20-gitignore-development-appsettings.md)).

Fields when CLI omits them:

- `ApiKey` — Cursor key. Empty means omit.
- `Model` — model id string (maps to `AgentOptions.ModelHint`). Empty means omit (`model` omitted on create).
- `Repo` — git URL.
- `Ref` — starting ref.
- `Name` — display name.

Prompt stays a CLI positional argument.

## Resolve order

1. CLI flag when present and non-empty.
2. File value when present and non-empty.
3. `CURSOR_API_KEY` for `ApiKey` only.

CLI wins over file. File wins over env. CloudAgents still receives only `RunnerConfig.ApiKey` and `AgentOptions`.

## What to build

### 1. CursorHttp catalog and create JSON

1. [x] `listModels` — `GET /v1/models` with the same Basic auth as other CursorHttp calls.
2. [x] Parse items: `id`, `displayName`, optional `description`, `aliases`, `parameters`, `variants`.
3. [x] Create JSON encodes `model` as `{ "id": … }` plus optional `params`. Omit `model` when `ModelHint` is `None`.
4. [x] `AgentOptions.ModelHint` stays a string id.

### 2. Console start

1. [x] Resolve CLI > file > env as above. Add `--model` and `--api-key` flags.
2. [x] After the key is known: list models and print `id` / displayName / variants.
3. [x] Then start / wait as today.
4. [x] Tracked empty placeholders; gitignore Development and Production for Console.
5. [x] Document usage in [CloudAgents.Console README](../../../src/CloudAgents.Console/README.md).

### 3. Proofs

1. [x] Parse tests for the models catalog (no live HTTP).
2. [x] Create-request JSON tests: `model` is an object with `id`; omitted when none.

### 4. Non-goals

1. Browser / `?ai` model picker.
2. CloudAgents reading appsettings.
3. Changing Server `AiKeys` / `AiRepos`.
4. Stream work on [17 — CloudAgents Console stream](17-cloudagents-console-stream.md).

## See also

[llm-connector map](../map.md), [15 — AiKeys from appsettings](15-aikeys-from-appsettings.md), [16 — AiRepos from appsettings](16-airepos-from-appsettings.md), [20 — Gitignore Development appsettings](20-gitignore-development-appsettings.md)

## Comments

- 2026-09-21 — Independent re-review after `ModelHint` revert: Good (with nits). Report [independent-review-21-console-lists-models-and-appsettings](../reports/independent-review-21-console-lists-models-and-appsettings.md). Status stays `coded`.
- 2026-09-21 — Coded: `listModels` + create `model` object; Console CLI > file > env; catalog print then start. Status `coded`.
- 2026-09-21 — Filed from chat: list models at Console start; fill CLI gaps from `appsettings.<level>.json`; create `model` is `{ id, params? }`. Status `defined`.

## Time

- 2026-09-21 2h — Ticket, listModels parse, create JSON object, Console appsettings, focused tests (from chat)
