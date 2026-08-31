# Implement session localStorage fallback

Branch: `w/login-context-restore`. Tree left dirty. No commit. No push.

## What changed

[[src/Client/SessionState.fs]] only. Same key `gambol-session-v1`, same snapshot JSON (`z` / `b` / `e`).

- **Write:** `saveSessionState` writes sessionStorage then localStorage (same payload). localStorageSet is try/with so a blocked/quota local write cannot break the Refresh path.
- **Read:** `tryGetItem` wraps getItem (can throw). `tryReadSessionJson` reads sessionStorage first; if null/empty/throw, localStorage. `tryReadSavedZoomId` and `restoreSessionState` both use that helper.
- Comments updated for dual store + read order. Folds (`e`) still ride on the blob. No new key/version. Auth cookies and Desktop AuthStore untouched. Existing JsInterop `sessionGet`/`sessionSet` / `localStorageGet`/`localStorageSet` used as-is.

Ticket: [[issues/06-restore-active-workspace-on-cold-init.md]]. Map: [[map.md]].

## Test / seam status

No SessionState tests in-tree. Storage get/set is JS interop. No pre-agreed Shared seam for the medium switch. Did not invent a harness. Did not extract a Shared first-non-empty helper (would be tautological). Client-only change, no new tests. No focused tests run.

## Typecheck

`dotnet build src/Client -c Debug` — succeeded, 0 warning, 0 error. Did not run `dotnet fable` (no watch running; .NET Client build typechecks the F#). Did not run the full suite.

## Code review vs HEAD

Range: uncommitted `git diff HEAD` on `src/Client/SessionState.fs`. `measure-fs-size.py --diff HEAD`: no over-limit bindings, no added long lines (`tryGetItem` 6, `tryReadSessionJson` 5). Review inline (single-file diff); no extra sub-agents.

### Standards

Sources: [[.cursor/rules/fsharp-source.mdc]] + smell baseline.

No hard violations: 4-space indent, ≤40-line bindings (`restoreSessionState` 38), ≤100-char lines, no mutable, match existing SessionState/JsInterop style. try/with for storage matches the prior sessionGet wrap (JS getItem can throw); not a new exception policy.

Smell baseline: none material. Helpers are not Speculative Generality — two readers share one read order. Swallowing localStorageSet failure is a judgement call; keeps Refresh working if localStorage is blocked.

### Spec

Sources: [[issues/06-restore-active-workspace-on-cold-init.md]] Answer; [[map.md]] Destination.

Implemented: Refresh-parity Workspace (`b` → `/state?zoom=`) + Zoom (`z`); same snapshot; write both stores; read session then local; both readers; folds ride along; no new credential store; comments match dual store.

Companion not in the Answer text: treat sessionGet throw as empty and fall through to localStorage; wrap localStorageSet. Needed so blocked storage cannot crash init/save. Not a new feature.

HITL experiment not run here (agent cannot).

Summary: Standards 0 hard / 0 smells worth acting on. Spec 0 missing / 1 justified companion (throw → fallback + wrap local write) / 0 wrong.

## Suggested commit message

```
Write gambol-session-v1 to localStorage and read it after sessionStorage so Safari cold init matches Refresh.

```

Include [[src/Client/SessionState.fs]]. Do not commit unless asked.

## HITL experiment and result

iPad/iPhone, regular Safari tab still open: after iOS memory-unload cold-reload, pass = already authenticated **and** previously active Workspace Loaded + Zoom restored (Refresh-parity). Kill/quit Safari is out of scope. ITP may still purge localStorage after 7 days without site interaction.

The user confirmed the Workspace + Zoom check passed on 2026-08-15. Device model and iOS version were unspecified. See the authoritative [[pending-audit-cold-reload.md]].

## WORK.md mutations (for parent)

- `remove` Blocked: [[plan/login-context-restore/map.md]] — iPad/iPhone still-open Safari cold-reload HITL passed

Stage is now `done`; [[plan/index.md]] was regenerated. Do not edit WORK.md from this worker.

## Blockers

None.
