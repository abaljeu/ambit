# 17 — Retarget want install tests
**Status**: 
One job: A developer can run these tests against a child list and a Header. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [16 — Retarget model status tests](plan/client-never-fails/issues/16-retarget-model-status-tests/16-retarget-model-status-tests-ticket.md)

## Inputs

### Working

[ ] `ChildrenStatus`, `Graph.childrenStatus`, `Graph.isLoaded`, `GraphChildren.isLoaded`, and `GraphChildren.status` are the calls these tests drop.
[ ] `GraphChildren.tryGet` and `Graph.children` stay. A present child-list key is the old Loaded list. An absent key is the old Unloaded list.
[ ] Each named test asserts child-list Loaded or Unloaded today.
[ ] ``compose then installWantAnswer loads wanted parents and children``, ``installWantAnswer keeps absent key Unloaded and [] Loaded``, ``installWantAnswer accepts a Child already Resident``, ``visibleClosureGraph loads reserved Children and Zoom ancestors`` in [[tests/Shared.Tests/WantTests.fs]].
[ ] `GraphChildren.tryGet` in `src/Shared/Model.fs` is the read these assertions use after the change.
[ ] Production files stay. This ticket changes the named tests only.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/17-retarget-want-install-tests-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: the `ChildrenStatus` type delete. That delete is 25 — Delete ChildrenStatus.
[ ] Closed: later behavior paths, including Absent fetch and the reload prompt.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change each named test so the assertion uses the child list or the Header.
3. [ ] Leave `ChildrenStatus`, `Graph.childrenStatus`, `Graph.isLoaded`, `GraphChildren.isLoaded`, and `GraphChildren.status` uncalled in those tests.

## Outputs

[ ] `tests/Shared.Tests/WantTests.fs` — change ``compose then installWantAnswer loads wanted parents and children``, ``installWantAnswer keeps absent key Unloaded and [] Loaded``, ``installWantAnswer accepts a Child already Resident``, ``visibleClosureGraph loads reserved Children and Zoom ancestors``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/17-retarget-want-install-tests-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``compose then installWantAnswer loads wanted parents and children``
  - `tests/Shared.Tests`: ``installWantAnswer keeps absent key Unloaded and [] Loaded``
  - `tests/Shared.Tests`: ``installWantAnswer accepts a Child already Resident``
  - `tests/Shared.Tests`: ``visibleClosureGraph loads reserved Children and Zoom ancestors``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/17-retarget-want-install-tests-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
