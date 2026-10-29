# 08 — Apply Replace when parent Header is present scope

Slice: applyOpAllowing applies Op.Replace when the parent Header is present. GraphMutate.replace uses that same Header gate so the later type delete has no isLoaded caller left on Replace.

## Signatures

[ ] `ResidentProjection.applyOpAllowing` matches `Op.Replace` in `src/Shared/ResidentProjection.fs` and requires `GraphChildren.isLoaded`.
[ ] `GraphMutate.replace` returns `Error` of a string `parent children not loaded` when `GraphChildren.isLoaded` is false. The parent Header check is the `Map.tryFind` above that gate.

## Match sites

[ ] `applyOpAllowing` returns `ApplyResult.Unchanged` when the parent child list is absent.
[ ] `Graph.replace` forwards to `GraphMutate.replace`.

## Existing tests

[ ] ``Graph.replace rejects Unloaded parent`` in [[tests/Shared.Tests/ModelTests.fs]] expects `Error` of a string `parent children not loaded`.
[ ] ``applyServerTail skips structural Replace on Unloaded parent`` in [[tests/Shared.Tests/SyncLogicTests.fs]] expects the Replace to be skipped.

## Copy-from

[ ] The Header miss in `GraphMutate.replace` already returns `Error` of a string `parent not found`.

## write-ticket

### Source files

[ ] [[src/Shared/ResidentProjection.fs]] — `applyOpAllowing` applies `Op.Replace` when the parent Header is present.
[ ] [[src/Shared/GraphMutate.fs]] — `replace` drops the `parent children not loaded` gate and keeps the Header gate.

### Tests

[ ] Change ``Graph.replace rejects Unloaded parent`` so a parent Header with no child list returns `Ok`.
[ ] Change ``applyServerTail skips structural Replace on Unloaded parent`` so the Replace applies when the parent Header is present.

## Size

[ ] Source files: 2. Under the limit of 8.
[ ] Tests added or changed: 2. Under the limit of 4.
