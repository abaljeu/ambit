# Download no-parse fix

## Root cause

Download file transfer (`WorkspaceFileSync.getStaged`) already uses currency/ledger rules and does not depend on parse state.

The failure happened **after** a successful download, when the client polled the async job and tried to align graph `updateTime` stamps:

1. `UpdateWorkspaceDownload.pollWorkspaceDownloadJob` (completed state)
2. `WorkspaceUploadStructure.planAlignFileStampOps` → `Op.SetUpdateTime` ops
3. `applyAndPostSync (displayName Download) change model`
4. `Op.apply` in `History.fs` blocked `SetUpdateTime` via `isBlockedByInaccessibleDocument` because the File node was `Unparsed`

That produced the UI error `Download: operation cannot modify an unparsed document; parse it first` even though only mtime metadata was being updated, not document content.

## Fix

Exempt `Op.SetUpdateTime` from the inaccessible-document gate in `History.fs`. Stamp alignment after download (and persist tails) is metadata-only and must not require parsing.

## Files changed

| File | Change |
|------|--------|
| `src/Shared/History.fs` | Allow `SetUpdateTime` through `isBlockedByInaccessibleDocument` |
| `tests/Shared.Tests/HistoryTests.fs` | Add `SetUpdateTime on unparsed file root succeeds` |

## Test results

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~HistoryTests|FullyQualifiedName~WorkspaceUploadStructureTests"
```

**Passed:** 79 tests, 0 failed.

## Branch note

Work done on `selective-client-sync` (not `w/*`).
