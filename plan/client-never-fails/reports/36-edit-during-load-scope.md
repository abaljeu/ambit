# 36 — Edit during a Load scope

Slice: A Load in flight leaves the one-second timer and the edit free to run. The Load reply uses applyLoadResponse from 31 — Load without a race.

## Signatures

[ ] `AutoDownloadTimer.armOrRearm` holds the edit timer after 34 — Type while others change.
[ ] `UpdateHelpers.commitTextEdit` posts while `mode` is `Editing`.
[ ] `SyncPlanner.tryStartLoad` sets `syncState` to `Loading` in `src/Shared/SyncPlanner.fs`.

## Match sites

[ ] `tryStartPoll` returns no effect for a state other than `Idle` with an empty queue. `Loading` is one of those states.
[ ] The edit timer is separate from that Poll gate.

## Existing tests

[ ] No fact shows an edit firing while `syncState` is `Loading`.

## Copy-from

[ ] `commitTextEdit` is the post the timer fires. It stays available during `Loading`.

## write-ticket

### Source files

[ ] [[src/Client/UpdateHelpers.fs]] — `commitTextEdit` still posts when `syncState` is `Loading`.
[ ] [[src/Client/RowView.fs]] — `onEditKeyDown` still re-arms the one-second timer when `syncState` is `Loading`.

### Tests

[ ] Add ``edit timer posts while Load is in flight`` in `tests/Server.Tests/BrowserResidencyClientTests.fs`.

## Size

[ ] Source files: 2. Under the limit of 8.
[ ] Tests added or changed: 1. Under the limit of 4.
