# 26 — Poll for nearby Absent Nodes scope

Slice: Want.compose keeps ids whose Header is absent. The included walk and the two child ranks already list those ids. Expand puts a Node's child ids on that walk. The next Poll brings the missing Nodes.

## Signatures

[ ] `Want.compose` in `src/Shared/Want.fs` filters with `GraphChildren.tryGet graph id = None` after 14 — Want filters a missing child list.
[ ] `IncludedDescendantIds.throughChildrenOfExpandedNodes` and `plusChildrenOfEach` build the walk.

## Match sites

[ ] Expand already puts child ids on the walk through `ViewModelSiteMap.expandEntry`.
[ ] A Resident id has a Header. An Absent id has no Header. After 25 — Delete ChildrenStatus the child list is present for a Resident Node.

## Existing tests

[ ] ``compose lists Included that miss Children first``, ``compose wants unloaded children of folded members``, ``compose wants one rank past a folded loaded child``, and ``compose then installWantAnswer loads wanted parents and children`` in [[tests/Shared.Tests/WantTests.fs]].

## Copy-from

[ ] `Map.tryFind id graph.nodes` is the Header check.

## write-ticket

### Source files

[ ] [[src/Shared/Want.fs]] — `compose`.

### Tests

[ ] Change the four compose facts so Want keeps an id whose Header is absent, and a Resident id with a child list stays off that filter.

## Size

[ ] Source files: 1. Under the limit of 8.
[ ] Tests added or changed: 4. At the limit of 4.
