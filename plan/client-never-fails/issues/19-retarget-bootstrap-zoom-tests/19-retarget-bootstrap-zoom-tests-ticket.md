# 19 — Retarget bootstrap zoom tests
**Status**: 
One job: A developer can run these tests against a child list and a Header. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [18 — Retarget closure and bootstrap tests](plan/client-never-fails/issues/18-retarget-closure-bootstrap-tests/18-retarget-closure-bootstrap-tests-ticket.md)

## Inputs

### Working

[ ] `ChildrenStatus`, `Graph.childrenStatus`, `Graph.isLoaded`, `GraphChildren.isLoaded`, and `GraphChildren.status` are the calls these tests drop.
[ ] `GraphChildren.tryGet` and `Graph.children` stay. A present child-list key is the old Loaded list. An absent key is the old Unloaded list.
[ ] Each named test asserts child-list Loaded or Unloaded today.
[ ] ``rootBootstrapGraph includes Ref header without children``, ``bootstrapGraph with zoom outside ROOT adds complete owning Workspace``, ``bootstrapGraph with no zoom is ROOT only``, ``bootstrapGraph with zoom inside ROOT does not duplicate residency`` in [[tests/Shared.Tests/BootstrapScopeTests.fs]].
[ ] `GraphChildren.tryGet` in `src/Shared/Model.fs` is the read these assertions use after the change.
[ ] Production files stay. This ticket changes the named tests only.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/19-retarget-bootstrap-zoom-tests-scope.md]]
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

[ ] `tests/Shared.Tests/BootstrapScopeTests.fs` — change ``rootBootstrapGraph includes Ref header without children``, ``bootstrapGraph with zoom outside ROOT adds complete owning Workspace``, ``bootstrapGraph with no zoom is ROOT only``, ``bootstrapGraph with zoom inside ROOT does not duplicate residency``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/19-retarget-bootstrap-zoom-tests-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``rootBootstrapGraph includes Ref header without children``
  - `tests/Shared.Tests`: ``bootstrapGraph with zoom outside ROOT adds complete owning Workspace``
  - `tests/Shared.Tests`: ``bootstrapGraph with no zoom is ROOT only``
  - `tests/Shared.Tests`: ``bootstrapGraph with zoom inside ROOT does not duplicate residency``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/19-retarget-bootstrap-zoom-tests-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
