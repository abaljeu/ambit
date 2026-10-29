# 14 — Want filters a missing child list scope

Slice: The isLoaded call in Want.compose is deleted. Until Absent fetch, the filter is an id with no child list.

## Signatures

[ ] `Want.compose` in `src/Shared/Want.fs` filters with `not (GraphChildren.isLoaded graph id)`.
[ ] `GraphChildren.isLoaded` is true when `childMap` contains the id.

## Match sites

[ ] `IncludedDescendantIds.throughChildrenOfExpandedNodes` builds the walk. `plusChildrenOfEach` runs twice.
[ ] An absent child-list key and `not isLoaded` name the same ids today.

## Existing tests

[ ] ``compose lists Included that miss Children first`` in [[tests/Shared.Tests/WantTests.fs]] lists an id after `unload`.

## Copy-from

[ ] `GraphChildren.tryGet` returns `None` for an absent child list.

## write-ticket

### Source files

[ ] [[src/Shared/Want.fs]] — `compose`.

### Tests

[ ] Add ``compose keeps an id whose child list is absent`` in `tests/Shared.Tests/WantTests.fs`.

## Size

[ ] Source files: 1. Under the limit of 8.
[ ] Tests added or changed: 1. Under the limit of 4.
