# Model params parallel synthesis

**Date:** 2026-09-22  
**Workplace:** `dev` (`scripts/gitstatus.sh`)  
**Ticket:** [22 — Checked-in cursor-models catalog and Console model/params selection](../issues/22-cursor-models-catalog-file.md) — Status `coded`  
**Stage:** stays `done`  
**Commit:** none

## Sources

- [cursor-models-catalog-file](cursor-models-catalog-file.md) — catalog seed, fsproj Content, Development `Models` removal
- [model-params-runtime-wire](model-params-runtime-wire.md) — `ModelParams`, create JSON, Console load/validate/`--param`
- [catalog-handoff-status](catalog-handoff-status.md) — handoff snapshot (catalog done; Console was sibling-owned)

## Integration check

| Check | Result |
| --- | --- |
| `src/CloudAgents/cursor-models.json` present | Yes (~40 models, `{ "models": [...] }`) |
| fsproj `Content` + `CopyToOutputDirectory` | Yes; also in CloudAgents + test output dirs |
| Console loads catalog (`CursorModelsFile.loadStartCatalog`) | Yes — file when present; live list only when absent |
| `AgentOptions.ModelParams` / `ModelParam` | Yes in `PublicTypes.fs` |
| Create JSON emits `model.params` when non-empty | Yes (`CursorHttp.modelJson`) |
| `Models` removed from appsettings | Yes — no `"Models"` under `src/` appsettings; Development has no `Models` |
| [22 — Checked-in cursor-models catalog and Console model/params selection](../issues/22-cursor-models-catalog-file.md) Status | `coded` (both workstream checklists marked) |

## What landed

1. **Catalog** — Checked-in `cursor-models.json` with ids, aliases, structured parameters/values, and synthesized variant ids; copied beside the CloudAgents DLL.
2. **Public API** — `ModelParam` + `AgentOptions.ModelParams`; adapter maps into Cursor `model: { id, params? }`.
3. **Console** — Prints file catalog; `--param id=value` (repeatable) and file `ModelParams`; CLI overrides file; fail-fast validation against catalog; passes params into `AgentOptions`.
4. **Server** — Mechanical `ModelParams = []` on `RunAgentActor` only (no browser picker).

## Gaps

- Browser / `?ai` model picker still out of scope (ticket non-goal).
- `appsettings.Development.json` stays local/untracked (secrets); do not commit it.
- Ticket Status is `coded`, not review-`done`.
- No commit on this synthesis pass.

## Spot-check

`dotnet test tests/CloudAgents.Tests/Gambol.CloudAgents.Tests.fsproj` — **29 passed** (includes create JSON with/without params, catalog file load, `--param` / `pickParams`).

## How to try Console

Needs a Cursor API key (`--api-key`, appsettings `ApiKey`, or `CURSOR_API_KEY`).

```bash
dotnet run --project src/CloudAgents.Console -- "Explain F# options" --model default
```

With params (must match catalog values for that model; params require `--model`):

```bash
dotnet run --project src/CloudAgents.Console -- "Add a short README note" \
  --model <catalog-id> \
  --param context=256k \
  --repo https://github.com/user/repo --ref main
```

At start, Console prints `=== Cursor models (cursor-models.json) ===` from the checked-in file. Invalid model id or param value exits with an error before create.
