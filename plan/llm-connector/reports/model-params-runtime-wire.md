# Model params runtime wire

**Date:** 2026-09-22
**Workplace:** `dev` (scripts/gitstatus.sh)
**Scope:** CloudAgents + Console runtime model id and params (sibling owns ticket + `cursor-models.json` seed)

## Verdict

Coded and verified. Create requests emit `{ id }` plus optional `params: [{ id, value }]`. Console loads the checked-in catalog, prints it, validates model/params, and passes `ModelParams` through `AgentOptions`. No commit.

## What changed

### CloudAgents public / Cursor types

- [PublicTypes.fs](../../../src/CloudAgents/PublicTypes.fs): added `ModelParam` (`Id`, `Value`) and `AgentOptions.ModelParams`.
- [CursorTypes.fs](../../../src/CloudAgents/Internal/CursorTypes.fs): structured catalog `CursorModelParameter` / `CursorParamValue`; create-side `CursorParamAssignment` + `CursorModelRef` (`id` + ``params``); `CursorCreateRequest.model` is now `CursorModelRef option`.

### HTTP parse / emit

- [CursorHttp.fs](../../../src/CloudAgents/Internal/CursorHttp.fs): parses `parameters` as a structured array (non-array → empty list); `modelJson` emits `{ id }` and omits `params` when empty; omits `model` when none.
- [CursorAdapter.fs](../../../src/CloudAgents/Internal/CursorAdapter.fs): maps `ModelHint` + `ModelParams` into `CursorModelRef`.

### Catalog loader

- New [CursorModelsFile.fs](../../../src/CloudAgents/Internal/CursorModelsFile.fs): loads `cursor-models.json` from `AppContext.BaseDirectory` / assembly dir; parses via `CursorHttp.parseModelCatalog`.
- Registered in [Gambol.CloudAgents.fsproj](../../../src/CloudAgents/Gambol.CloudAgents.fsproj). Sibling already seeded the JSON and Content copy.

### Console

- [Config.fs](../../../src/CloudAgents.Console/Config.fs): repeatable `--param id=value`; file `ModelParams` array (selected values only); resolve CLI > file via `pickParams`.
- [Program.fs](../../../src/CloudAgents.Console/Program.fs): prints catalog from file (id, displayName, param ids + allowed values); fail-fast validate against catalog (id/alias + param id/value); passes `ModelParams` into `AgentOptions`. No live `/v1/models` for the start list.

### Mechanical compile fix (Server)

- [RunAgentActor.fs](../../../src/Server/RunAgentActor.fs): `ModelParams = []` only. `ModelHint` unchanged (out of scope for selection wiring).

### Docs / tests

- [README.md](../../../src/CloudAgents/README.md): `ModelParam`, catalog note, sample records updated.
- Tests: create JSON with/without params; structured parameters parse; catalog file load; `--param` / resolve CLI > file.

## Verify

| Check | Result |
| --- | --- |
| `dotnet test tests/CloudAgents.Tests/Gambol.CloudAgents.Tests.fsproj` | Passed 29 |
| `dotnet build src/CloudAgents.Console/... -o /tmp/gambol-console-build` | Succeeded; `cursor-models.json` present in output |

## Coordination notes

- Did not edit plan tickets / `project.md` (sibling).
- Did not remove appsettings `Models` (sibling already did for Development).
- Did not invent a fake catalog; used sibling `cursor-models.json`.
- Did not commit.

## See also

[22 — Checked-in cursor-models catalog](../issues/22-cursor-models-catalog-file.md), [cursor-models-catalog-file](cursor-models-catalog-file.md)
