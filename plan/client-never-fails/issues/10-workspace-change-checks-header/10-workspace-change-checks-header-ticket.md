# 10 — Workspace change checks Header
**Status**: 
One job: A workspace change checks the workspace Header. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [09 — Parent change checks Header](plan/client-never-fails/issues/09-parent-change-checks-header/09-parent-change-checks-header-ticket.md)

## Inputs

### Working

[ ] `WorkspaceUploadStructure.planStubOps` in `src/Shared/WorkspaceUploadStructure.fs` returns `Ok []` when the workspace Header is present and `GraphChildren.isLoaded` is false.
[ ] A missing workspace Header falls through to stub planning.
[ ] The skip exists so an empty child list would not invent ids that collide with server names.
[ ] ``Load Unloaded stub plan must not name-conflict on resident server`` in [[tests/Shared.Tests/WorkspaceUploadStructureTests.fs]] expects no stub ops while the workspace child list is absent.
[ ] ``planStubOps creates Directory named .agents with file child`` in `tests/Shared.Tests/WorkspaceUploadStructureTests.fs` plans stubs when the workspace child list is present.
[ ] `workspaceByLabel` is the lookup `planStubOps` already uses before the loaded check.
[ ] [[src/Shared/WorkspaceUploadStructure.fs]] — `planStubOps`.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/10-workspace-change-checks-header-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: ``planStubOps creates Directory named .agents with file child``. That fact is 24 — Retarget expr miss tests, in the same batch as the status assertion.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `planStubOps` so the workspace Header is the check. Drop the `GraphChildren.isLoaded` skip.
3. [ ] Change the fact named in Outputs. Leave `Graph.childrenStatus` uncalled in that fact.

## Outputs

[ ] `src/Shared/WorkspaceUploadStructure.fs`
[ ] `tests/Shared.Tests/WorkspaceUploadStructureTests.fs` — change ``Load Unloaded stub plan must not name-conflict on resident server``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/10-workspace-change-checks-header-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``Load Unloaded stub plan must not name-conflict on resident server``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/10-workspace-change-checks-header-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
