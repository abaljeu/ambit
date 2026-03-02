# ViewModel Refactor Plan

Move client-side view-model types and pure logic into `Shared` so they
can be compiled on both .NET (for unit tests) and Fable (for the browser).

## Goal

All logic that doesn't touch the DOM or Fable JS interop should live in
`Shared/ViewModel.fs`, making it directly testable with XUnit in
`tests/Shared.Tests/`.

## What moves where

### New file: `src/Shared/ViewModel.fs`

Pulled from `src/Client/Model.fs` (types) and `src/Client/Update.fs`
(pure helpers):

**Types**
- `Mode` — `Selecting | Editing of originalText * cursorPos option`
- `Selection` — `{ range: NodeRange; focus: int }`
- `Model` — `{ graph; revision; selectedNodes: Selection option; mode: Mode }`
- `Msg` — all messages (unchanged)

**Pure helpers** (no DOM/Fable deps)
- `tryFindParentAndIndex`
- `singleSelection`
- `firstSelectedNodeId`
- `focusedNodeId`
- `getVisibleRowIds`
- `shiftArrow`
- `collapseToFocus`
- `moveSelectionBy`
- `applyMoveSelectionUp` / `applyMoveSelectionDown` — extract the pure
  portion of the `MoveSelectionUp`/`Down` match arms (no edit commit, no
  dispatch); accept `Model` and return `Model`.

### `src/Client/Model.fs`

Delete entirely.  All types now come from `Gambol.Shared.ViewModel`.

### `src/Client/Update.fs`

- Remove moved helpers.
- `open Gambol.Shared` already brings `ViewModel` namespace in.
- Keep: `readEditInputValue`, `postJson`, `applyAndPost`,
  `commitTextEdit`, `splitNode`, `encodeChangeBody`,
  `decodeStateResponse`, `currentFile`, `update`.
- `MoveSelectionUp`/`Down` in `update`: call `applyMoveSelectionUp/Down`
  for the pure path; handle edit-commit branch locally (needs `dispatch`).

### `src/Client/Gambol.Client.fsproj`

- Remove `Model.fs`.
- `Update.fs` and `View.fs` remain.

### `src/Shared/Gambol.Shared.fsproj`

- Add `ViewModel.fs` after `Model.fs` (depends on `NodeRange`, `Graph`,
  `Revision`, `NodeId` from `Model.fs`).

### `tests/Shared.Tests/`

Add `ViewModelTests.fs` to exercise (all pure, no DOM):

- `singleSelection` — returns correct `Selection` with `focus = start`
- `shiftArrow -1` / `+1` — extend, shrink, no-op at bounds
- `shiftArrow` single-node — always extends
- `applyMoveSelectionDown` with 3-item range, focus at start → focus moves to `endd-1`, range unchanged
- `applyMoveSelectionUp` with 3-item range, focus at end → focus moves to `start`, range unchanged
- `applyMoveSelectionDown` with focus at end → collapses and moves
- `moveSelectionBy` — advances visible row index

Add `ViewModelTests.fs` to `.fsproj` `<Compile>` list.

## Implementation order

1. Create `Shared/ViewModel.fs` with types + pure helpers.
2. Add `ViewModel.fs` to `Shared/Gambol.Shared.fsproj`.
3. Delete `Client/Model.fs`, remove from `Client/Gambol.Client.fsproj`.
4. Trim `Client/Update.fs` — remove moved helpers, adjust `update`.
5. Fix any remaining references in `Client/View.fs`.
6. Build `src/Server` (which pulls in Shared) to confirm no regressions.
7. Create `tests/Shared.Tests/ViewModelTests.fs` + test cases.
8. Run `dotnet test`.
