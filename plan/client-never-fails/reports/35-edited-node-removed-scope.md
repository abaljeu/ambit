# 35 — Edited Node removed scope

Slice: adjustModeAfterServerApply sets the mode to Selecting when the edited NodeId has no Header. The view draws from the Graph.

## Signatures

[ ] `UpdateHelpers.adjustModeAfterServerApply` in `src/Client/UpdateHelpers.fs` switches to `Selecting` when the edited Node's text changes.
[ ] `ViewModelDomPlan.planPatchDOM` draws rows from the Graph.

## Match sites

[ ] The function reads `Map.tryFind editingId` on the previous Graph and on the new Graph and compares text.
[ ] A missing Header is `None` from `Map.tryFind`.

## Existing tests

[ ] No fact calls `adjustModeAfterServerApply`.

## Copy-from

[ ] `planPatchDOM` in `src/Shared/ViewModelDomPlan.fs` is the draw path. This ticket leaves that file as it is when it already draws from the Graph.

## write-ticket

### Source files

[ ] [[src/Client/UpdateHelpers.fs]] — `adjustModeAfterServerApply`.

### Tests

[ ] Add ``adjustModeAfterServerApply selects when edited Header is absent`` in `tests/Server.Tests/BrowserResidencyClientTests.fs`.

## Size

[ ] Source files: 1. Under the limit of 8.
[ ] Tests added or changed: 1. Under the limit of 4.
