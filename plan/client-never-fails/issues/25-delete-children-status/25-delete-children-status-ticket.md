# 25 — Delete ChildrenStatus
**Status**: 
One job: A developer sees `ChildrenStatus` gone, and a Resident Node carries its Header and its child list. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [24 — Retarget expr miss tests](plan/client-never-fails/issues/24-retarget-expr-miss-tests/24-retarget-expr-miss-tests-ticket.md)

## Inputs

### Working

[ ] `type ChildrenStatus` and `GraphChildren.isLoaded` and `GraphChildren.status` in `src/Shared/Model.fs`.
[ ] `GraphBuild.isLoaded` and `GraphBuild.childrenStatus` in `src/Shared/GraphBuild.fs`.
[ ] `Graph.isLoaded` and `Graph.childrenStatus` in `src/Shared/GraphOps.fs`.
[ ] `GraphChildren.tryGet`, `GraphChildren.get`, and `Graph.children` stay.
[ ] Callers of `isLoaded` and `childrenStatus` move in tickets 08 — Apply Replace when parent Header is present through 15 — Load target omits the Workspace, and in the test batches 16 — Retarget model status tests through 24 — Retarget expr miss tests.
[ ] ``getState zoom outside ROOT adds owning Workspace`` and ``getState without zoom keeps nested Workspace Unloaded`` in [[tests/Server.Tests/ApiGetStateTests.fs]].
[ ] ``GET state default returns ROOT closure excluding nested workspace contents`` in [[tests/Server.Tests/StateEndpointTests.fs]].
[ ] `GraphChildren.tryGet` remains the child-list read.
[ ] [[src/Shared/Model.fs]] — delete `ChildrenStatus`, `GraphChildren.isLoaded`, and `GraphChildren.status`.
[ ] [[src/Shared/GraphBuild.fs]] — delete `isLoaded` and `childrenStatus`.
[ ] [[src/Shared/GraphOps.fs]] — delete `Graph.isLoaded` and `Graph.childrenStatus`.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/25-delete-children-status-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: `GraphChildren.tryGet`, `GraphChildren.get`, and `Graph.children`.
[ ] Closed: Absent fetch. That path starts at 26 — Poll for nearby Absent Nodes.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change the three named tests so they use the child list or the Header.
3. [ ] Contract `src/Shared/Model.fs`, `src/Shared/GraphBuild.fs`, and `src/Shared/GraphOps.fs` by deleting the seven names in the slice line.
4. [ ] Stop when any other file still names one of those seven. Name that file in the stop report.

## Outputs

[ ] `src/Shared/Model.fs`
[ ] `src/Shared/GraphBuild.fs`
[ ] `src/Shared/GraphOps.fs`
[ ] `tests/Server.Tests/ApiGetStateTests.fs` — change the two getState facts
[ ] `tests/Server.Tests/StateEndpointTests.fs` — change ``GET state default returns ROOT closure excluding nested workspace contents``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/25-delete-children-status-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``getState zoom outside ROOT adds owning Workspace``
  - `tests/Server.Tests`: ``getState without zoom keeps nested Workspace Unloaded``
  - `tests/Server.Tests`: ``GET state default returns ROOT closure excluding nested workspace contents``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/25-delete-children-status-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
