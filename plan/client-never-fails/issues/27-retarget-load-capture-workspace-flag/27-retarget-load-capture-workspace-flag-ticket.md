# 27 — Retarget Load capture Workspace flag
**Status**: 
One job: A developer can run these Load tests with a target id and `includeWorkspace` false. The story Delete the whole-Workspace request in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [26 — Poll for nearby Absent Nodes](plan/client-never-fails/issues/26-poll-nearby-absent-nodes/26-poll-nearby-absent-nodes-ticket.md)

## Inputs

### Working

[ ] `LoadTarget.includeWorkspace` in `src/Shared/ApiResponses.fs` stays on the record for this ticket.
[ ] The Server still answers `includeWorkspace` true until 30 — Delete includeWorkspace. These tests stop sending true.
[ ] Each named test builds a `LoadTarget` with `includeWorkspace`.
[ ] ``wantAnswerForTargets without residency intent returns empty``, ``wantAnswerForTargets returns direct Children and pointed-at Nodes``, ``wantAnswerForTargets missing target returns empty``, ``wantAnswerForTargets refuses targets across Workspaces`` in [[tests/Shared.Tests/LoadCaptureTests.fs]].
[ ] `UpdateHelpers.loadTargetIntent` already sets false in 15 — Load target omits the Workspace.
[ ] Production files stay. This ticket changes the named tests only.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/27-retarget-load-capture-workspace-flag-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: deleting the field. That delete is 30 — Delete includeWorkspace.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change each named test so `includeWorkspace` is false and the expected answer is the target ids.
3. [ ] Leave `LoadTarget` and the Server branch as they are.

## Outputs

[ ] `tests/Shared.Tests/LoadCaptureTests.fs` — change ``wantAnswerForTargets without residency intent returns empty``, ``wantAnswerForTargets returns direct Children and pointed-at Nodes``, ``wantAnswerForTargets missing target returns empty``, ``wantAnswerForTargets refuses targets across Workspaces``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/27-retarget-load-capture-workspace-flag-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``wantAnswerForTargets without residency intent returns empty``
  - `tests/Shared.Tests`: ``wantAnswerForTargets returns direct Children and pointed-at Nodes``
  - `tests/Shared.Tests`: ``wantAnswerForTargets missing target returns empty``
  - `tests/Shared.Tests`: ``wantAnswerForTargets refuses targets across Workspaces``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/27-retarget-load-capture-workspace-flag-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
