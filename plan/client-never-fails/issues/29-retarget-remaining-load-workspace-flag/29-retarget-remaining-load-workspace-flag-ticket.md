# 29 — Retarget remaining Load Workspace flag
**Status**: 
One job: A developer can run these Load tests with a target id and `includeWorkspace` false. The story Delete the whole-Workspace request in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [28 — Retarget Server Load Workspace flag](plan/client-never-fails/issues/28-retarget-server-load-workspace-flag/28-retarget-server-load-workspace-flag-ticket.md)

## Inputs

### Working

[ ] `LoadTarget.includeWorkspace` in `src/Shared/ApiResponses.fs` stays on the record for this ticket.
[ ] The Server still answers `includeWorkspace` true until 30 — Delete includeWorkspace. These tests stop sending true.
[ ] Each named test builds a `LoadTarget` with `includeWorkspace`.
[ ] ``postLoad shares one event id for Events and Nodes``, ``postLoad same Workspace multi-target dedupes answer Nodes``, ``postLoad refuses selection spanning two Workspaces`` in [[tests/Server.Tests/ApiPostLoadTests.fs]].
[ ] ``LoadRequest round-trip`` in [[tests/Shared.Tests/SerializationTests.fs]].
[ ] `UpdateHelpers.loadTargetIntent` already sets false in 15 — Load target omits the Workspace.
[ ] Production files stay. This ticket changes the named tests only.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/29-retarget-remaining-load-workspace-flag-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: deleting the field. That delete is 30 — Delete includeWorkspace.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change each named test so `includeWorkspace` is false and the expected answer is the target ids.
3. [ ] Leave `LoadTarget` and the Server branch as they are.

## Outputs

[ ] `tests/Server.Tests/ApiPostLoadTests.fs` — change ``postLoad shares one event id for Events and Nodes``, ``postLoad same Workspace multi-target dedupes answer Nodes``, ``postLoad refuses selection spanning two Workspaces``
[ ] `tests/Shared.Tests/SerializationTests.fs` — change ``LoadRequest round-trip``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/29-retarget-remaining-load-workspace-flag-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``postLoad shares one event id for Events and Nodes``
  - `tests/Server.Tests`: ``postLoad same Workspace multi-target dedupes answer Nodes``
  - `tests/Server.Tests`: ``postLoad refuses selection spanning two Workspaces``
  - `tests/Shared.Tests`: ``LoadRequest round-trip``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/29-retarget-remaining-load-workspace-flag-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
