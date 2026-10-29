# 27 — Retarget Load capture Workspace flag scope

Slice: Retarget the named fixtures to includeWorkspace false and to the target ids only. The field stays on LoadTarget until 30 — Delete includeWorkspace.

## Signatures

[ ] `LoadTarget.includeWorkspace` in `src/Shared/ApiResponses.fs` stays on the record for this ticket.
[ ] The Server still answers `includeWorkspace` true until 30 — Delete includeWorkspace. These tests stop sending true.

## Match sites

[ ] Each named test builds a `LoadTarget` with `includeWorkspace`.

## Existing tests

[ ] ``wantAnswerForTargets without residency intent returns empty``, ``wantAnswerForTargets returns direct Children and pointed-at Nodes``, ``wantAnswerForTargets missing target returns empty``, ``wantAnswerForTargets refuses targets across Workspaces`` in [[tests/Shared.Tests/LoadCaptureTests.fs]].

## Copy-from

[ ] `UpdateHelpers.loadTargetIntent` already sets false in 15 — Load target omits the Workspace.

## write-ticket

### Source files

[ ] Production files stay. This ticket changes the named tests only.

### Tests

[ ] Change the 4 named tests so each target uses `includeWorkspace` false and the answer is the target ids.

## Size

[ ] Source files: 0. Under the limit of 8.
[ ] Tests added or changed: 4. Within the limit of 4.
