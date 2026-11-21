# 16 — Poll for nearby Absent Nodes build

`Want.compose` in `src/Shared/Want.fs` keeps an id when `Map.tryFind id graph.nodes` is `None`. The included walk stays `IncludedDescendantIds.throughChildrenOfExpandedNodes`, then `plusChildrenOfEach` twice. `ChildrenStatus` and its cases `Loaded` and `Unloaded` stay. The filter no longer calls `GraphChildren.isLoaded`.

## Tests

[ ] ``compose lists Included that miss Children first`` — an Unloaded child that still has a Header is off the want. The same id with no Header is the want.
[ ] ``compose then installWantAnswer loads wanted parents and children`` — an Unloaded parent that still has a Header is off the want. An Absent parent is the first want. A grandchild Header that is Unloaded is off the second want. A grandchild id with no Header is the second want.
[ ] ``compose wants unloaded children of folded members`` — an Unloaded folded child that still has a Header is off the want. The same id with no Header is the want.
[ ] ``compose wants one rank past a folded loaded child`` — an Unloaded id one rank past a folded loaded child is off the want when its Header stays. The same id with no Header is the want. The folded loaded child stays off the want.
[ ] ``compose is empty when Included is Loaded`` — unchanged. The want stays empty.

## Check

[ ] Each named filter in `tests/Shared.Tests`: Passed 1, Failed 0
[ ] `scripts/agent-test.sh` with no arguments: Passed 15, Failed 0, Total 15
[ ] `scripts/agent-test.sh --full` Server: Passed 733, Failed 0, Skipped 1, Total 734
[ ] `scripts/agent-test.sh --full` Shared: Passed 1929, Failed 0, Skipped 3, Total 1932
[ ] Log: `/tmp/ambit-full-suite-20261103-170103.log`
[ ] Verdict is `pass` in [16 — Poll for nearby Absent Nodes check](plan/client-never-fails/reports/16-poll-nearby-absent-check.md)
