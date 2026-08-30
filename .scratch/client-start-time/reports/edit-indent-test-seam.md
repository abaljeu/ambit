# Edit+indent Tab — regression test seam

Date: 2026-08-28
Branch: `w/client-start-time`
Prior: [[edit-indent-old-text-mismatch.md]]
No implement in this report.

## Verdict

The bug is real and localized: **Poll/Load can advance Graph text while overlay `returnTo` keeps a stale `Editing.originalText`; Tab indent then posts `SetText(staleOriginal, live)` and local apply returns `"old text does not match"`.** There is **no** existing focused test for this path. [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]] references Shared only — not Client — so the smallest agent-runnable seam is: **extract the pure edit-commit and post-apply mode helpers to Shared, then add one Shared.Tests file that simulates Poll apply under `CommandPalette` and asserts indent-shaped commit ops fail CAS.**

## Mode and overlay shapes

Top-level Browser mode ([[src/Shared/ViewModel.fs]] L273–281):

| Case | Shape | `returnTo` |
| --- | --- | --- |
| `Selecting` | — | — |
| `Editing` | `originalText * EditCaret` | — |
| `CommandPalette` | `query * selectedCommand * returnTo: Mode` | nested |
| `SearchDialog` | `SearchDialogState` | `returnTo: Mode` |
| `FileSearchDialog` | `FileSearchDialogState` | `returnTo: Mode` |
| `CssClassPrompt` | `returnTo * initialValue` | nested |
| `RenamePrompt` | `returnTo * initialValue` | nested |

Palette open preserves prior mode ([[src/Client/CommandPalette.fs]] L12–13): `CommandPalette ("", 0, model.mode)`. Close restores `returnTo` ([[src/Client/CommandPalette.fs]] L16–18). The same nesting pattern applies to Search, FileSearch, CssClass, and Rename overlays.

**Effective edit mode** (used for row rendering and focus, not for sync guards): [[src/Shared/ViewModelRowState.fs]] L53–65 and [[src/Client/RowView.fs]] L196–207 unwrap overlays to `returnTo` before testing `Editing`. `#edit-input` initial text comes from that effective `Editing (text, _)` ([[src/Client/RowView.fs]] L204–208), so the DOM can show stale `originalText` while Graph already moved.

## Sync guards (the gap)

### `isAutoSyncBlocked`

[[src/Client/UpdateHelpers.fs]] L205–221 — blocks Poll/Load auto-apply when:

1. `pendingChanges` is non-empty, or
2. **top-level** mode is `Editing _` **and** `readEditInputValue () <> graphText` for the focused node.

When palette is open, top-level mode is `CommandPalette`, not `Editing`. Auto-sync is **not** blocked even if nested `returnTo` is `Editing` and the live field still matches graph at Poll time. After Poll applies remote `SetText`, graph text changes but nested snapshot is untouched.

### `adjustModeAfterServerApply`

[[src/Client/UpdateHelpers.fs]] L225–237 — runs after every successful Poll/Load graph apply ([[src/Client/Update.fs]] L244, L283, L319, L334, L397). Only matches **top-level** `Editing _`. Under an overlay it is a **no-op**, so nested `Editing(originalText)` survives remote text change.

### Indent Tab commit path

[[src/Client/UpdateMove.fs]] L31–58 `trySaveContext` unwraps overlays and captures **`ctx.originalText` from nested `Editing`**, not current graph text. [[src/Client/UpdateMove.fs]] L139–145 reads live text from DOM (`readEditInputValue ()`), then [[src/Client/UpdateHelpers.fs]] L241–246 `tryTextCommitOps` emits `Op.SetText(nodeId, originalTextForHistory, newText)` when `newText <> graph.nodes.[nodeId].text`. [[src/Client/UpdateMove.fs]] L254–261 `indentSelection` calls `tryMoveNodeFromTo` → local `applyAndPost` → `SyncLogic.applyLocalChange` → [[src/Shared/GraphMutate.fs]] L55–56 CAS `"old text does not match"`.

## Poll / Load apply call sites

All paths: `SyncLogic.applyServerTail` or `applyLoadResponse` → update `graph` / `revision` / `history` → `withSiteMap` → **`adjustModeAfterServerApply prevGraph`**.

| Message | File | When blocked |
| --- | --- | --- |
| `PollDone` (Loading) | [[src/Client/Update.fs]] L224–225 | always skip apply |
| `PollDone` (Uploading/Parsing + DataOutdated) | L228–244 | `isAutoSyncBlocked` |
| `PollDone` (catchUp) | L252–283 | not blocked by dirty edit |
| `PollDone` (Idle DataOutdated) | L293–319 | `changes.IsEmpty \|\| isAutoSyncBlocked` |
| `BootGraphApplied` | L324–334 | `isAutoSyncBlocked` N/A (boot) |
| `LoadDone` | L336–397 | `isAutoSyncBlocked` when `hasPayload` |

Mechanism 1 from [[edit-indent-old-text-mismatch.md]] needs only Poll or Load final stage under an open overlay; HITL uses palette + one Poll.

## Existing focused tests (gaps)

| Area | File | Covers bug? |
| --- | --- | --- |
| `Mode` / overlay storage | [[tests/Shared.Tests/SearchDialogModeTests.fs]], [[tests/Shared.Tests/FileSearchDialogModeTests.fs]] | `returnTo` round-trip only |
| `planIndentSelection` / `completeIndent` | [[tests/Shared.Tests/ViewModelMoveOpsTests.fs]] | structural indent; **Selecting** mode, no edit commit |
| `SetText` CAS | [[tests/Shared.Tests/SyncLogicTests.fs]], [[tests/Shared.Tests/HistoryTests.fs]] | wrong `oldText` in isolation |
| `applyServerTail` | [[tests/Shared.Tests/SyncLogicTests.fs]] | graph merge; **no** `Mode` / overlay |
| `EditingCaretPreserve` / `planPatchDOM` editing row | [[tests/Shared.Tests/ViewModelTests.fs]] L2558–2661 | caret/DOM plan; assumes top-level `Editing` |
| `isAutoSyncBlocked` / `adjustModeAfterServerApply` / `tryTextCommitOps` / `trySaveContext` | — | **untested** (Client-only, no Client.Tests project) |

[[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]] has no Client reference per [[.cursor/skills/add-shared-test/SKILL.md]].

## Smallest deterministic regression-test seam

### Prerequisite extraction (minimal Shared surface)

Move pure helpers from Client to Shared (new module [[src/Shared/ViewModelEditOps.fs]] suggested):

1. **`effectiveMode : Mode -> Mode`** — dedupe from [[src/Shared/ViewModelRowState.fs]] / [[src/Client/RowView.fs]].
2. **`tryEditingSnapshot : Mode -> (string * EditCaret) option`** — same unwrap chain as [[src/Client/UpdateMove.fs]] `trySaveContext` (without `rebuildMode`).
3. **`tryTextCommitOps`** — lift from [[src/Client/UpdateHelpers.fs]] L241–246 unchanged (already pure).
4. **`adjustModeAfterServerApply : Graph -> VM -> VM`** — lift from [[src/Client/UpdateHelpers.fs]] L225–237; test documents current behavior, then fix updates implementation.

Client modules re-export or call Shared; no behavior change until fix.

**Do not** use DOM in the test: pass `liveText` as a parameter (stand-in for `readEditInputValue ()`). That matches project rule: ambiguous browser behavior is not under test ([[.cursor/rules/testing-workflow.mdc]]).

### Test file and scenario

New file: [[tests/Shared.Tests/EditingOverlayPollTests.fs]] (register in fsproj after [[tests/Shared.Tests/ViewModelMoveOpsTests.fs]]).

**Primary test (red before fix):** ``Poll under CommandPalette returnTo Editing leaves stale originalText and indent-shaped SetText fails apply``

Deterministic steps:

1. Build indentable graph (reuse `buildFlat` / `modelWithSelection` from [[tests/Shared.Tests/ViewModelMoveOpsTests.fs]]): e.g. container with `"a"`, `"b"`, select `"b"` at index 2.
2. `originalText = "b"`. Model: `mode = Editing (originalText, EditCaret.EndOfText)`, selection on `"b"`.
3. Open palette: `mode = CommandPalette ("", 0, Editing (originalText, …))`.
4. Poll tail: `SyncLogic.applyServerTail` with one `Op.SetText(bNodeId, "b", "b-remote")`; merge into VM (`graph`, `revision`, `history`).
5. Run **`adjustModeAfterServerApply prevGraph`** (currently no-op for overlay).
6. Close palette: restore `returnTo` → top-level `Editing (originalText, …)` (still `"b"`).
7. `liveText = originalText` (RecreateRow seeds `#edit-input` from stale snapshot; user need not type).
8. `textOps = tryTextCommitOps bNodeId originalText liveText graph` → non-empty; contains `SetText(bNodeId, "b", "b")`.
9. Optional full pattern: `planIndentSelection` → append `replaceOps` as [[src/Client/UpdateMove.fs]] L150–152.
10. `SyncLogic.applyLocalChange "Indent" change clientState` → **`Error "old text does not match"`**.

**Secondary test (guard):** same setup but **top-level** `Editing` (no palette) and assert `adjustModeAfterServerApply` switches to `Selecting` — proves the existing guard works only at top level.

**Not sufficient alone:** HITL, DOM integration, or Server.Tests — too slow and non-deterministic for agent loop ([[edit-indent-old-text-mismatch.md]] HITL section).

### Exact focused test command

After `dotnet build tests/Shared.Tests/Gambol.Shared.Tests.fsproj -c Debug`:

```bash
dotnet test tests/Shared.Tests/Gambol.Shared.Tests.fsproj -c Debug --no-build --filter "FullyQualifiedName~EditingOverlayPollTests"
```

Single-test filter once named:

```bash
dotnet test tests/Shared.Tests/Gambol.Shared.Tests.fsproj -c Debug --no-build --filter "FullyQualifiedName~Poll under CommandPalette returnTo Editing"
```

## Smallest likely fix (do not implement here)

**Rank 1 — extend `adjustModeAfterServerApply` (and Poll/Load apply path) to use effective edit snapshot:** Unwrap overlays like `effectiveMode`. If focused node's text changed between `prevGraph` and new graph, either (a) rewrite nested `Editing (originalText, caret)` in the full mode tree to `Editing (newGraphText, clamped caret)`, or (b) set top-level `Selecting`. Option (a) preserves edit session after remote non-conflicting update; option (b) matches today's top-level behavior.

Implementation sketch: one Shared function `refreshEditingSnapshotsInMode : Graph -> Graph -> Mode -> Mode` called from `adjustModeAfterServerApply`, walking `CommandPalette` / `SearchDialog` / `FileSearchDialog` / `CssClassPrompt` / `RenamePrompt` `returnTo` chains.

**Rank 2 — overlay close refresh:** In `closeCommandPaletteOp` (and sibling close ops), if restored `Editing` `originalText <> graph.nodes.[editingId].text`, refresh from graph. Fixes post-close only; Poll while palette still open leaves stale nested snapshot until close — Rank 1 is strictly better.

**Rank 3 — `tryTextCommitOps` uses graph text as CAS old:** Would mask the bug and break undo/history pairing; **reject**.

**Do not fix only `isAutoSyncBlocked`:** Unwrapping effective `Editing` for dirty-check would still allow Poll when live matches graph at Poll instant; the stale snapshot problem remains.

## What would refute the seam

- Test passes **before** fix while production HITL still fails → seam missing DOM/live mismatch; add parameterized `liveText` cases.
- Test fails for wrong reason (e.g. indent plan `None`) → fixture must keep `sel.range.start > 0` and expanded previous sibling per [[tests/Shared.Tests/ViewModelMoveOpsTests.fs]] indent fixtures.
- Fix only Rank 2 and primary test still red with Poll applied **before** close → confirms Rank 1 is required.

## Board

No mutations. Active item [[WORK.md]] stays; this report is the implementation handoff for test-first fix on `w/client-start-time`.
