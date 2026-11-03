# 16 — Poll for nearby Absent Nodes
**Status**: 
One job: The outliner sees Poll bring nearby Absent Nodes, because `Want.compose` keeps Absent ids and Poll is the fetch. The story Poll for nearby Absent Nodes in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [06 — Server sends each Node with its child list](plan/client-never-fails/issues/06-server-sends-child-list/06-server-sends-child-list-ticket.md)

## Inputs

### Working

[ ] `Want.compose` in `src/Shared/Want.fs` has type `Graph -> SiteMap -> NodeId -> NodeId list`.
[ ] The walk is `IncludedDescendantIds.throughChildrenOfExpandedNodes`, then `plusChildrenOfEach` twice.
[ ] The filter is `not (GraphChildren.isLoaded graph id)`. That keeps Unloaded ids, which still have a Header.
[ ] Absent is an id with no Header. `Map.tryFind id graph.nodes` is that check. `ChildrenStatus`, `Loaded`, and `Unloaded` stay.
[ ] Expand already puts a Node's child ids on the walk through `ViewModelSiteMap.expandEntry`. The next Poll uses the ids `compose` returns.
[ ] ``compose lists Included that miss Children first``, ``compose then installWantAnswer loads wanted parents and children``, ``compose wants unloaded children of folded members``, and ``compose wants one rank past a folded loaded child`` in [[tests/Shared.Tests/WantTests.fs]] call `unload`, which keeps the Header and drops the child list.
[ ] ``compose is empty when Included is Loaded`` in `tests/Shared.Tests/WantTests.fs` expects an empty list when every Included id has a Header and a child list.
[ ] [[src/Shared/Want.fs]] — `compose`.

### Reference

[ ] Story order in [[plan/client-never-fails/arch.md]]. Path 7 is this ticket.

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: `type ChildrenStatus` and its cases `Loaded` and `Unloaded`.
[ ] Closed: `includeWorkspace`. That delete is 07 — Delete includeWorkspace, blocked by this ticket.
[ ] Closed: ``compose is empty when Included is Loaded``. A Resident Included id stays off the filter.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `Want.compose` so the filter keeps an id whose Header is absent. Leave the included walk and the two child ranks as they are.
3. [ ] Leave `ChildrenStatus` in place. The filter stops using `GraphChildren.isLoaded`.
4. [ ] Change the four compose facts named in Inputs so an Absent id is wanted and an Unloaded id that still has a Header is not.

## Outputs

[ ] `src/Shared/Want.fs`
[ ] `tests/Shared.Tests/WantTests.fs` — change the four compose facts

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/16-poll-nearby-absent-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[ ] Stop when ``compose is empty when Included is Loaded`` fails and fixing it would change a fifth test.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``compose lists Included that miss Children first``
  - `tests/Shared.Tests`: ``compose then installWantAnswer loads wanted parents and children``
  - `tests/Shared.Tests`: ``compose wants unloaded children of folded members``
  - `tests/Shared.Tests`: ``compose wants one rank past a folded loaded child``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/16-poll-nearby-absent-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
