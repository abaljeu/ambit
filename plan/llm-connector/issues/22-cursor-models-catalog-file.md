# 22 — Checked-in cursor-models catalog and Console model/params selection

**Status:** done
**Blocked by:** None — [21 — Console lists models and fills CLI gaps from appsettings](21-console-lists-models-and-appsettings.md) is `done`.
**Type:** coding
Estimate: 2h
Actual: 3h

## Context

[21 — Console lists models and fills CLI gaps from appsettings](21-console-lists-models-and-appsettings.md) prints the live Cursor catalog at Console start and fills omitted CLI fields from appsettings. The live `GET /v1/models` list is not versioned with the repo, and a `Models` array in local Development appsettings is the wrong place for a catalog.

This ticket checks in a seed catalog at [cursor-models.json](../../../src/CloudAgents/cursor-models.json) (copied to CloudAgents output) and selects Console runtime model id plus params from that file. CloudAgents stays settings-blind for secrets; the catalog is data, not `AiKeys`. Sibling owns CursorHttp / PublicTypes / Console `Program` / `Config` edits.

## What to build

### 1. Checked-in catalog (this workstream)

1. [x] Seed [src/CloudAgents/cursor-models.json](../../../src/CloudAgents/cursor-models.json) from live `GET https://api.cursor.com/v1/models` (Basic auth with Development `AiKeys[0].ApiKey`; key never printed or committed).
2. [x] Shape: top-level `models[]` with `id`, `displayName`, `aliases`, `parameters` (`id` / `displayName` / `values[]` of `value` / `displayName`), and `variants` (`id` / `displayName`). Preserve API parameter values and variant param bindings needed for create `{ id, params? }`. Live variants have no `id`; synthesize a stable id from param pairs (empty params → `default`).
3. [x] Copy the file to output in [Gambol.CloudAgents.fsproj](../../../src/CloudAgents/Gambol.CloudAgents.fsproj) (`Content` / `CopyToOutputDirectory` `PreserveNewest`).
4. [x] Remove any `Models` array from [src/appsettings.Development.json](../../../src/appsettings.Development.json). Keep `AiKeys` / `AiRepos` / other secrets. Do not commit secrets.

### 2. Console runtime selection (sibling workstream)

1. [x] Load the checked-in catalog at Console start (output-adjacent path); do not call live `/v1/models` for the start list when the file is present.
2. [x] Print catalog ids / display names / variants (and params as needed).
3. [x] Resolve CLI / file model id and optional params for create `{ "id", "params"? }` from the catalog.
4. [x] Edit only sibling-owned surfaces (`CursorHttp.fs`, `PublicTypes.fs`, `Program.fs`, `Config.fs`) as required.

### 3. Non-goals

1. Browser / `?ai` model picker.
2. Committing `appsettings.Development.json` or any API key.
3. Changing Server `AiKeys` / `AiRepos` binding.

## See also

[llm-connector map](../map.md), [21 — Console lists models and fills CLI gaps from appsettings](21-console-lists-models-and-appsettings.md), reports [cursor-models-catalog-file](../reports/cursor-models-catalog-file.md), [model-params-runtime-wire](../reports/model-params-runtime-wire.md), [model-params-parallel-synthesis](../reports/model-params-parallel-synthesis.md)

## Comments

- 2026-09-24 — Already on `origin/ready` tip; included in staging publish. Status `done`.
- 2026-09-22 — Both workstreams closed: catalog + Console runtime selection integrated; synthesis [model-params-parallel-synthesis](../reports/model-params-parallel-synthesis.md). Status `coded`. Spot-check: CloudAgents.Tests 29 passed.
- 2026-09-22 — Catalog workstream coded: seeded 40 models into `cursor-models.json`, fsproj Content copy, removed Development `Models` array. Console selection left for sibling. Status `coded` for this workstream's deliverables.
- 2026-09-22 — Filed: checked-in catalog + Console model id / params selection. Status `defined`.

## Time

- 2026-09-22 30m — Parallel synthesis: verify integration, close ticket checkboxes, focused tests, report (from chat)
- 2026-09-22 1.5h — Sibling runtime wire: ModelParams, create JSON params, Console catalog load/validate/--param (from chat)
- 2026-09-22 1h — Ticket, live seed of cursor-models.json, fsproj Content, remove Development Models, report (from chat)
