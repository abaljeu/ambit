# 06 — Send child list on want answer scope

Slice: wantAnswer and visibleClosureWantAnswer send each returned Node's Header and its child list. The list is ids only. A child id with no Header stays in the list.

## Signatures

[ ] `ResidentProjection.wantAnswer` and `ResidentProjection.visibleClosureWantAnswer` in `src/Shared/ResidentProjection.fs`.
[ ] `SyncResponse.childMap` is `Map<NodeId, ChildNode list>` in `src/Shared/ApiResponses.fs`.

## Match sites

[ ] `wantAnswer` builds edges with `GraphChildren.tryGet`. A parent with no child-list key is omitted.
[ ] A complete edge omits a parent when a child Header is missing. This ticket keeps that child id on the list.

## Existing tests

[ ] ``wantAnswer omits an edge with an absent target Node`` in [[tests/Shared.Tests/WantTests.fs]] expects the edge to be omitted.
[ ] ``wantAnswer returns wanted edges and pointed-at Nodes`` in `tests/Shared.Tests/WantTests.fs` covers a complete edge.
[ ] ``visibleClosureWantAnswer installs through installWantAnswer`` in `tests/Shared.Tests/WantTests.fs` installs the closure answer.

## Copy-from

[ ] `GraphChildren.tryGet` in `src/Shared/Model.fs` is the child-list read.

## write-ticket

### Source files

[ ] [[src/Shared/ResidentProjection.fs]] — `wantAnswer` and `visibleClosureWantAnswer`.

### Tests

[ ] Change ``wantAnswer omits an edge with an absent target Node`` so the child id with no Header stays in the list.
[ ] Add ``visibleClosureWantAnswer keeps a child id with no Header`` in `tests/Shared.Tests/WantTests.fs`.

## Size

[ ] Source files: 1. Under the limit of 8.
[ ] Tests added or changed: 2. Under the limit of 4.
