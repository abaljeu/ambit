# 26 — Poll for nearby Absent Nodes
**Status**: 
One job: The outliner sees Poll bring Absent Nodes near the open section, and placeholders there fill in. The story Poll for nearby Absent Nodes in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [25 — Delete ChildrenStatus](plan/client-never-fails/issues/25-delete-children-status/25-delete-children-status-ticket.md)

## Inputs

### Working

[ ] `Want.compose` in `src/Shared/Want.fs` filters with `GraphChildren.tryGet graph id = None` after 14 — Want filters a missing child list.
[ ] `IncludedDescendantIds.throughChildrenOfExpandedNodes` and `plusChildrenOfEach` build the walk.
[ ] Expand already puts child ids on the walk through `ViewModelSiteMap.expandEntry`.
[ ] A Resident id has a Header. An Absent id has no Header. After 25 — Delete ChildrenStatus the child list is present for a Resident Node.
[ ] ``compose lists Included that miss Children first``, ``compose wants unloaded children of folded members``, ``compose wants one rank past a folded loaded child``, and ``compose then installWantAnswer loads wanted parents and children`` in [[tests/Shared.Tests/WantTests.fs]].
[ ] `Map.tryFind id graph.nodes` is the Header check.
[ ] [[src/Shared/Want.fs]] — `compose`.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/26-poll-nearby-absent-nodes-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: ``compose is empty when Included is Loaded``. A Resident Included id stays off the filter, so that fact stays as it is unless it fails.
[ ] Closed: `includeWorkspace`. The next tickets remove that flag.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `Want.compose` so the filter keeps an id whose Header is absent.
3. [ ] Leave the included walk and the two `plusChildrenOfEach` ranks as they are.
4. [ ] Change the four facts named in Outputs.

## Outputs

[ ] `src/Shared/Want.fs`
[ ] `tests/Shared.Tests/WantTests.fs` — change the four compose facts

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/26-poll-nearby-absent-nodes-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[ ] Stop when ``compose is empty when Included is Loaded`` fails and fixing it would change a fifth test.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``compose lists Included that miss Children first``
  - `tests/Shared.Tests`: ``compose wants unloaded children of folded members``
  - `tests/Shared.Tests`: ``compose wants one rank past a folded loaded child``
  - `tests/Shared.Tests`: ``compose then installWantAnswer loads wanted parents and children``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/26-poll-nearby-absent-nodes-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
