# IgnoredDestination batch `classify`

Disk-effect validation on `POST /ambit/changes` now passes the full destination name list to `GitCheckIgnore.classify` (`git check-ignore --stdin`). It does not fold `isIgnored` per path.

## What changed

[[src/Server/IgnoredDestination.fs]] `validateGraphDiskEffects`:

1. Still builds new/changed document-root File and Directory paths (`destinationEffects`). Skips Directory File names.
2. Drops `.gitignore` destinations (`isGitignorePath`) and empty scoped relatives. Those stay allowed.
3. Groups remaining names by work tree (named Workspace vs data-dir ROOT/TRASH).
4. One `classify` per work tree with that tree’s full relative list. Empty list: no git process (`classify` already returns `Ok []`).
5. Walks node-id order and returns the first ignored-destination error. Message text is unchanged.

FileAgent and DbAgent still call this only through [[src/Server/DocumentPersistence.fs]]. No persist-timeout change. No stub-Change split.

A typical Upload stub Change is one named Workspace, so one git process.

## Tests

Added `many new destinations reject ignored and keep gitignore` in [[tests/Server.Tests/IgnoredDestinationValidationTests.fs]]: 20 allowed files plus `.gitignore` under `.*` plus `blocked.txt`. Did not assert process count (flaky).

Command:

`dotnet test tests/Server.Tests -c Debug --filter FullyQualifiedName~IgnoredDestination`

Passed: 9, Failed: 0, Skipped: 0. Did not run all tests. Did not edit Shared, so no Client compile gate.

## Remaining per-path `isIgnored`

- [[src/Server/WorkspaceWebDav.fs]] `isOmitted`: one `isEffectivelyIgnored` per WebDAV PUT omit. Not the Change disk-effect fold. Left as-is.
- [[src/Shared/dotnet/GitCheckIgnore.fs]] `isEffectivelyIgnored` still calls `isIgnored`. Helpers and Shared tests still use both.
- Batch `classify` also: [[src/Shared/dotnet/WorkspaceLocalInventory.fs]] `applyIgnoreFilter`; [[src/Server/WorkspaceWebDav.fs]] `keepUncertainDirs`.

## Board

`move` [[changes-post-timeout.md]] — leftover HITL: Upload stub `POST /ambit/changes` (~1000 ops) fail at ~60s vs ~100s vs HTTP 400 persist. Batch FileAgent check-ignore `classify` landed (this report, [[src/Server/IgnoredDestination.fs]]).
