# 25 — Delete ChildrenStatus scope

Slice: Delete ChildrenStatus, GraphChildren.isLoaded, GraphChildren.status, GraphBuild.isLoaded, GraphBuild.childrenStatus, Graph.isLoaded, and Graph.childrenStatus. Retarget the last three tests in the same ticket. The type delete is the last batch of Remove Loaded and Unloaded.

## Signatures

[ ] `type ChildrenStatus` and `GraphChildren.isLoaded` and `GraphChildren.status` in `src/Shared/Model.fs`.
[ ] `GraphBuild.isLoaded` and `GraphBuild.childrenStatus` in `src/Shared/GraphBuild.fs`.
[ ] `Graph.isLoaded` and `Graph.childrenStatus` in `src/Shared/GraphOps.fs`.
[ ] `GraphChildren.tryGet`, `GraphChildren.get`, and `Graph.children` stay.

## Match sites

[ ] Callers of `isLoaded` and `childrenStatus` move in tickets 08 — Apply Replace when parent Header is present through 15 — Load target omits the Workspace, and in the test batches 16 — Retarget model status tests through 24 — Retarget expr miss tests.

## Existing tests

[ ] ``getState zoom outside ROOT adds owning Workspace`` and ``getState without zoom keeps nested Workspace Unloaded`` in [[tests/Server.Tests/ApiGetStateTests.fs]].
[ ] ``GET state default returns ROOT closure excluding nested workspace contents`` in [[tests/Server.Tests/StateEndpointTests.fs]].

## Copy-from

[ ] `GraphChildren.tryGet` remains the child-list read.

## write-ticket

### Source files

[ ] [[src/Shared/Model.fs]] — delete `ChildrenStatus`, `GraphChildren.isLoaded`, and `GraphChildren.status`.
[ ] [[src/Shared/GraphBuild.fs]] — delete `isLoaded` and `childrenStatus`.
[ ] [[src/Shared/GraphOps.fs]] — delete `Graph.isLoaded` and `Graph.childrenStatus`.

### Tests

[ ] Change the three named tests so they use the child list or the Header.
[ ] After the delete, a search of `src` and `tests` finds none of the seven deleted names.

## Size

[ ] Source files: 3. Under the limit of 8.
[ ] Tests added or changed: 3. Under the limit of 4.
