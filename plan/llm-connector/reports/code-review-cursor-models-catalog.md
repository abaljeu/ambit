# Code review — cursor models catalog

**Range:** uncommitted vs `HEAD` (`git diff HEAD`)  
**Spec:** [22 — Checked-in cursor-models catalog and Console model/params selection](../issues/22-cursor-models-catalog-file.md)  
**Ticket Status:** left `coded` (report is not approval)

## Standards

### Hard (documented)

**`.agents/rules/refer-by-name.md` — name with id**
- [model-params-parallel-synthesis.md](model-params-parallel-synthesis.md):25 — bare Status wording without the ticket name (BARE_ID). Line 5 already names it correctly.

**`.agents/rules/fsharp-source.md` — ≤100 chars/line**
- [Program.fs](../../../src/CloudAgents.Console/Program.fs):24 — LONG (114): usage example string.

**`.agents/rules/fsharp-source.md` — ≤40 lines/function** (scan miss: body grew; `let` headers not on `+` lines)
- [Config.fs](../../../src/CloudAgents.Console/Config.fs)::`fromElement` — 53 lines (`** OVER 40`)
- [Config.fs](../../../src/CloudAgents.Console/Config.fs)::`settingsDirectory` — 52 lines (`** OVER 40`)

**Scan listing without `** OVER 40`** (`parseParamPair`, `readParam`, `validateParam`, etc.) — under threshold; not size violations.

**`.agents/rules/fsharp-source.md` — tests exempt**
- [CursorHttpJsonTests.fs](../../../tests/CloudAgents.Tests/CursorHttpJsonTests.fs):57 LONG (285) — not a hard hit.

### Soft (smell baseline — judgement)

**Duplicated Code** — public `ModelParam` vs internal `CursorParamAssignment` (same id/value shape); adapter remaps. Quote:
```fsharp
{ CursorTypes.CursorParamAssignment.id = p.Id
  CursorTypes.CursorParamAssignment.value = p.Value }
```

**Duplicated Code** — [Config.fs](../../../src/CloudAgents.Console/Config.fs) `readParam` repeats Id/id and Value/value branches.

**Data Clumps** — `ModelHint` + `ModelParams` travel together while `CursorModelRef` already bundles them.

**Surgical / scope** ([core-agent-behavior.md](../../../.agents/rules/core-agent-behavior.md)) — deleting tracked [src/Server/appsettings.json](../../../src/Server/appsettings.json) (Auth, Logging, AiKeys, …) is broader than model-params wiring; judgement whether that belongs in this hunk.

No `mutable` / TAB hits. `core-api.md` EventId rules N/A here.

## Spec

### (a) Missing / partial

- **Variants not printed.** Spec: “Print catalog ids / display names / variants (and params as needed).” `printModels` prints id, displayName, aliases, and params only; the prior `variant …` loop is gone.
- **Hard fail if catalog missing.** Spec: “do not call live `/v1/models` for the start list when the file is present.” `printCatalog` always uses `CursorModelsFile.load` and exits on missing file—no live fallback when absent (stricter than the “when present” wording).
- **Variant `params` not in runtime types.** Spec: “Preserve … variant param bindings needed for create `{ id, params? }`.” Catalog JSON keeps variant `params`, but `parseVariants` only keeps `id` / `displayName`, so loaded models drop those bindings.

### (b) Scope creep

- **Beyond sibling-owned edit list.** Spec: “Edit only sibling-owned surfaces (`CursorHttp.fs`, `PublicTypes.fs`, `Program.fs`, `Config.fs`) as required.” Diff also changes `CursorTypes.fs`, `CursorAdapter.fs`, new `CursorModelsFile.fs`, `RunAgentActor.fs`, README, tests (some needed to compile/wire; still outside the named list).
- **Server settings deleted.** Spec non-goal: “Changing Server `AiKeys` / `AiRepos` binding.” Diff deletes tracked `src/Server/appsettings.json` (AiKeys/AiRepos scaffold), not just Console catalog work.
- **Unasked Console plumbing.** Spec does not ask to delete `CloudAgents.Console/appsettings.json`, change `launchSettings.json` `workingDirectory`, or rewrite `settingsDirectory` to prefer repo-root/`src` via `gambol.sln`/`.git`.

### (c) Looks done but wrong / weak

- **Catalog print claim vs behavior.** Same print requirement as (a): header says `cursor-models.json`, but omitting variants means the start list does not match the specified catalog surface (variants are seeded and parsed, then unused in output/selection).
- **`Models` removal not visible as a tracked edit.** Spec: “Remove any `Models` array from `src/appsettings.Development.json`.” Workspace file has no `Models` and keeps `AiKeys`, but the file is untracked, so that requirement is not evidenced in `git diff HEAD` against a previously tracked Development path.

**On-spec and present:** seeded `cursor-models.json` (`models[]` with id/displayName/aliases/parameters/variants), fsproj `Content`/`PreserveNewest`, Console load from file (no live list when loaded), CLI/file `--param`/`ModelParams` → create `{ id, params? }` via `CursorModelRef`.

## Summary

Standards: 4 hard (BARE_ID, Program LONG, Config `fromElement`/`settingsDirectory` over 40) + soft smells; worst: Config functions over 40 lines. Spec: 3 missing/partial + 3 scope + 2 weak; worst: variants not printed / variant params dropped at parse.
