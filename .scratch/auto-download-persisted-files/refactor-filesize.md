# Refactor: correct over-400 filesize growth (auto-download)

Stage artifact for the Standards hard violations from the auto-download review. Interview locked 2026-08-08.

## Problem Statement

Three Shared/Client files were already over the 400-line F# source limit. The auto-download work grew each without a split, which is a hard Standards violation (“if a file is already longer, only restructure to split up the code if your changes would increase it”).

Approximate sizes at plan time: Client app runtime ~754, workspace-sync update module ~625, core ViewModel types file ~462. Growth from auto-download was modest (timer scheduling, accumulate/tick helpers, pending-download field + effect/msg), but any growth of an already-over-limit file is the violation.

Primitive Obsession on naked string pairs was fixed separately. Soft judgement smells (list append accumulate, thin Middle Man wrapper) are not the problem this plan solves.

## Solution

Make a good-faith push toward under-400 with **safe peels only**. Prefer matching existing Client `Update*` module splits and the Shared `ViewModel*` companion-file pattern. Do not attempt a broad App effects-module extract (nested `and` handlers close over mutable timeout ids). Leave a file still over 400 only when further splitting would be risky.

Concrete approach:

1. **Workspace sync update module** — peel desktop HTTP/mapping helpers, then the download + auto-download family (and Load command surface if still over 400). Aim for the remaining sync/push/upload module under 400.
2. **App runtime** — peel only auto-download debounce timer scheduling into a tiny helper; accept App remaining over 400.
3. **ViewModel types file** — move the sync-queue + Effect chunk (`SyncState`, queued-request type, `SyncInfo` + helpers, `Effect`) into a new Shared companion file compiled before the core ViewModel file, keeping the same namespace so call sites stay stable. Aim under 400 without relocating `VM` / `SystemMsg` / site-map types.

## Steps

Each step leaves the tree compiling (Shared then Client as touched). Prefer focused Shared tests after Shared moves; Client peels verify by compile.

1. **Baseline counts** — Record line counts for the three hotspots and confirm which bindings are auto-download growth vs pre-existing bulk. No code move yet.

2. **Shared: new sync/effect companion file** — Create a new Shared source file in the same namespace as the core ViewModel types. Move, verbatim, the sync-state DU, the queued-request DU, the sync-info record + its helper module, and the Effect DU (including the auto-download schedule case). Do not move `VM`, `SystemMsg`, `Msg`, site-map types, desktop file indicators, or edit/search dialog types.

3. **Shared: compile order** — Register the new file in the Shared project **before** the core ViewModel file. Remove the moved definitions from the core file. Build Shared. Run existing SyncInfo-focused Shared tests (SyncLogic suite) to confirm helpers still resolve.

4. **Shared: confirm ViewModel size** — Re-count the core ViewModel file. Target under 400. If somehow still over, stop and reassess; do not start relocating `VM` fields or SystemMsg in this plan.

5. **Client: auto-download timer helper** — Add a small Client helper module that only arms/re-arms a debounce timeout: clear previous id, setTimeout, dispatch the auto-download system message, clear the stored id on fire. Keep the mutable timeout id owned by `createRuntime` (same pattern as the existing retry timer). Replace the nested `runScheduleAutoDownloadTick` body with a call into the helper. Compile Client. Accept App still over 400.

6. **Client: desktop HTTP/mapping peel** — Create a new Client `Update*` module for desktop workspace HTTP and mapping helpers currently private in the workspace-sync update module: JSON encode helpers for path/scope, sync post/put wrappers, pick-folder, upsert mapping, lookup mapped path, ensure-mapped, and closely related pure helpers they need. Leave shared result helpers (`withResult` / `fail` / `okDetail` / poll helpers) in the parent sync module unless a helper is used only by the peeled code. Wire the parent module to call the new module. Compile Client. Re-count parent size.

7. **Client: download + auto-download peel** — Create a new Client `Update*` module for the download command path and auto-download accumulate/tick helpers: scoped download POST, download command handler, download job poll/fail completions that belong with download, local-mapping predicate used by auto-download, debounce constant, accumulate-from-ops/changes, and run-tick. Update the main Client update dispatcher call sites that currently invoke those functions on the workspace-sync module so they call the new module. Keep push/upload/inventory/structure/reconcile in the parent. Compile Client. Re-count parent size.

8. **Client: Load peel only if needed** — If the parent workspace-sync module is still over 400 after steps 6–7, extract the Load command surface (`loadOp`, availability helper, queue-load-when-blocked, and only the private helpers used solely by Load) into a third `Update*` module. Do not move push/upload create flows unless they are exclusively Load-owned and blocking the under-400 goal. Compile and re-count. Stop once the parent is under 400 or further cuts would drag push/upload into a risky tangle.

9. **Final size gate** — Confirm: ViewModel core under 400; workspace-sync parent under 400; App may remain over 400 after the timer-only peel. Confirm no behaviour change intended: auto-download still debounces, coalesces via existing Shared helpers, and no-ops on plain web; Load/Download/Upload paths unchanged.

10. **Focused verification** — Run Shared tests covering SyncInfo helpers and WorkspaceSyncScope coalesce / autoDownloadFileTargets. Build Client (and Desktop host if that is the usual local check). No new behaviour tests required for pure file moves unless a step accidentally changes signatures that tests construct.

## Decision Document

- Goal is a good-faith under-400 result with safe peels; App may remain over 400 after a timer-only extract because a broad effects-module split is too risky with nested handlers and mutable timeout ids.
- Judgement smells (list-append accumulate style, thin Middle Man wrapper) are out of scope.
- Named AutoDownloadTarget type remains; this plan does not revisit that fix.
- Shared sync/effect types move as one chunk into a companion file in the same namespace, compiled before the core ViewModel file, so SyncPlanner and Client opens keep working without type renames.
- Do not relocate VM, SystemMsg, Msg, or site-map construction as part of this effort.
- Client workspace sync follows the existing Update-module family: first desktop HTTP/mapping, then download + auto-download; Load is a conditional third peel only if still over the limit.
- Auto-download timer mutable state stays in the App runtime factory; only the schedule/clear/setTimeout body moves to a helper.
- No API contract or wire-protocol changes; no server changes; no roadmap feature behaviour changes.
- Publish and implement under the existing auto-download-persisted-files scratch project / project branch.

## Testing Decisions

- Good tests assert external behaviour (coalesce rules, SyncInfo state transitions, auto-download target extraction), not which Client file owns a helper.
- Shared modules under test: SyncInfo helpers (existing SyncLogic tests) and WorkspaceSyncScope / upload-structure auto-download target extraction (existing WorkspaceSyncScope tests).
- Client module splits: compile is the primary check; no mandatory new Client unit tests for move-only steps.
- Prior art: SyncLogicTests for SyncInfo; WorkspaceSyncScopeTests for coalesceDownloadTargets and autoDownloadFileTargets; other ViewModel companion files already live beside the core ViewModel file without duplicating those suites.

## Out of Scope

- Fixing or rewriting `@` accumulate / Middle Man judgement items.
- Broad App effects-module extraction or restructuring of retry/poll/parse handlers.
- Splitting unrelated over-400 Client files (e.g. UpdateOps) or other ViewModel companions already over 400.
- Behaviour changes to auto-download, Load, Download, or Upload.
- Editing the original Cursor plan file under `.cursor/plans/`.
- Getting App under 400 in this effort.

## Further Notes

Verified at interview time: project branch `w/auto-download-persisted-files`; line counts matched the Standards review; Primitive Obsession already addressed via AutoDownloadTarget; WorkspaceUploadStructure remained under 400 after auto-download adds.
