# 34 — Type while others change scope

Slice: Each key in the edit box re-arms a one-second timer. armOrRearm holds that timer. When the timer fires, commitTextEdit posts the text and the mode stays Editing.

## Signatures

[ ] `RowView.Layout.onEditKeyDown` in `src/Client/RowView.fs`.
[ ] `AutoDownloadTimer.armOrRearm` in `src/Client/AutoDownloadTimer.fs` takes `delayMs` and `onFire`.
[ ] `UpdateHelpers.commitTextEdit` in `src/Client/UpdateHelpers.fs` sets `mode` to `Selecting` after a post today.

## Match sites

[ ] `armOrRearm` clears the previous timeout id, stores the new id, and clears it when the callback fires.
[ ] `commitTextEdit` posts through `applyAndPost` when `tryTextCommitOps` returns ops.

## Existing tests

[ ] No fact calls `commitTextEdit` or `onEditKeyDown`.

## Copy-from

[ ] `AutoDownloadTimer.armOrRearm` is the timer helper. The auto-download caller passes its own delay. This ticket passes 1000 for the edit box.

## write-ticket

### Source files

[ ] [[src/Client/RowView.fs]] — `onEditKeyDown` re-arms the timer on each key.
[ ] [[src/Client/UpdateHelpers.fs]] — `commitTextEdit` posts the text and leaves `mode` as `Editing`.
[ ] [[src/Client/AutoDownloadTimer.fs]] — `armOrRearm` holds the timer. The delay for this path is 1000.

### Tests

[ ] Add ``commitTextEdit posts text and stays Editing`` in `tests/Server.Tests/BrowserResidencyClientTests.fs`.

## Size

[ ] Source files: 3. Under the limit of 8.
[ ] Tests added or changed: 1. Under the limit of 4.
