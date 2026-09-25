# Code review — cursor models catalog fixes

**Date:** 2026-09-22  
**Review:** [code-review-cursor-models-catalog](code-review-cursor-models-catalog.md)  
**Ticket:** [22 — Checked-in cursor-models catalog and Console model/params selection](../issues/22-cursor-models-catalog-file.md) — Status unchanged `coded`  
**Tests:** `dotnet test tests/CloudAgents.Tests/Gambol.CloudAgents.Tests.fsproj -o tests/CloudAgents.Tests/_testout` — 30 passed  
**Console build:** `dotnet build src/CloudAgents.Console/Gambol.CloudAgents.Console.fsproj -o src/CloudAgents.Console/_buildout` — succeeded  
**Commit:** none

## Standards (hard)

| Finding | Fix |
| --- | --- |
| BARE_ID in [model-params-parallel-synthesis](model-params-parallel-synthesis.md) | Table row now links [22 — Checked-in cursor-models catalog and Console model/params selection](../issues/22-cursor-models-catalog-file.md) by name + id. |
| `Program.fs` usage example LONG line | Split example across two `printfn` lines (≤100 chars). |
| `Config.fs` `fromElement` / `settingsDirectory` over 40 lines | Restored `readFirstArrayString`; extracted `findSettingsDir` / `settingsFilenames`; both bindings now ≤12 lines (measurer verified). |
| `readParam` Id/id duplication (soft, actionable) | `readJsonString` + `readParamField` shared helper. |

## Spec

| Finding | Fix |
| --- | --- |
| Variants not printed | Restored variant loop in `printModels`; shows id, display name, and variant `params` when present. |
| Hard fail when catalog file absent | `CursorModelsFile.loadStartCatalog` uses checked-in file when `tryFindPath` succeeds; otherwise `CursorHttp.listModels` (live list only when file missing). Console `printCatalog` takes `apiKey` and uses this path. |
| Variant `params` dropped at parse | `CursorModelVariant` carries ``params``; `parseParamAssignment` / `parseParamAssignments` in `CursorHttp.fs`. Test `parseModelCatalog reads variant param bindings`. |
| Server `appsettings.json` deleted | Restored from `HEAD` (`src/Server/appsettings.json`). |
| Unasked Console plumbing | Restored `src/CloudAgents.Console/appsettings.json` from `HEAD`; reverted `launchSettings.json` `workingDirectory` to `src/Server`; reverted `settingsDirectory` to upward walk from cwd/exe (no `gambol.sln`/`.git` repo-root hop). |
| `Models` removal not in `git diff` | No code change: ticket non-goal forbids committing `appsettings.Development.json` (secrets). Local `src/appsettings.Development.json` has no `Models` array; removal recorded in [model-params-parallel-synthesis](model-params-parallel-synthesis.md) and catalog reports. |
| Sibling-only edit list exceeded | **Left as-is:** `CursorModelsFile.fs`, `cursor-models.json`, fsproj `Content`, and tests are required for catalog + compile; not reverted. `RunAgentActor.fs` `ModelParams = []` remains mechanical Server wiring only. |

## Soft smells deliberately left

| Smell | Reason |
| --- | --- |
| `ModelParam` vs `CursorParamAssignment` duplicate shape | Public vendor-neutral type vs internal Cursor JSON; adapter mapping is the intended seam. |
| `ModelHint` + `ModelParams` clump vs `CursorModelRef` | Console/settings use public `AgentOptions`; no spec ask to collapse types. |
| Deleting Console `appsettings.json` vs keeping it | Restored tracked Console scaffold; catalog work does not depend on deleting it. |

## Files touched (fix pass)

- `src/CloudAgents.Console/Config.fs`, `Program.fs`, `Properties/launchSettings.json`
- `src/CloudAgents.Console/appsettings.json` (restored)
- `src/Server/appsettings.json` (restored)
- `src/CloudAgents/Internal/CursorTypes.fs`, `CursorHttp.fs`, `CursorModelsFile.fs`
- `tests/CloudAgents.Tests/CursorHttpJsonTests.fs`
- `plan/llm-connector/reports/model-params-parallel-synthesis.md`
