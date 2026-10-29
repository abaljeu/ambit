# 30 — Delete includeWorkspace
**Status**: 
One job: Load asks for target ids only, and the whole-Workspace flag is gone. The story Delete the whole-Workspace request in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [29 — Retarget remaining Load Workspace flag](plan/client-never-fails/issues/29-retarget-remaining-load-workspace-flag/29-retarget-remaining-load-workspace-flag-ticket.md)

## Inputs

### Working

[ ] `LoadTarget` in `src/Shared/ApiResponses.fs`.
[ ] `encodeLoadTarget` and `decodeLoadTargetDecoder` in `src/Shared/ApiResponseSerialization.fs`.
[ ] `UpdateHelpers.loadTargetIntent` in `src/Client/UpdateHelpers.fs` sets the flag false.
[ ] `ResidentProjection.wantAnswerForTargets` filters on `includeWorkspace` in `src/Shared/ResidentProjection.fs`.
[ ] `LoadRefuse.MultiWorkspace` stays on the refusal union.
[ ] `tryStartLoad` and `tryStartLoadFetch` pass `LoadTarget` lists through.
[ ] ``tryStartLoad emits LoadServer when idle with empty queue`` and ``tryStartLoad returns no effects when already loading`` in [[tests/Shared.Tests/SyncPlannerTests.fs]] build a target with `includeWorkspace`.
[ ] Target-id answers in `wantAnswer` are the copy for `wantAnswerForTargets` after the flag goes.
[ ] [[src/Shared/ApiResponses.fs]] — delete `includeWorkspace` from `LoadTarget`.
[ ] [[src/Shared/ApiResponseSerialization.fs]] — encode and decode the target id only.
[ ] [[src/Client/UpdateHelpers.fs]] — `loadTargetIntent` builds a target id only.
[ ] [[src/Shared/ResidentProjection.fs]] — `wantAnswerForTargets` answers the target ids. Delete the `includeWorkspace` filter.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/30-delete-include-workspace-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: `LoadRefuse.MultiWorkspace`. It stays.
[ ] Closed: the raced Load guard. That guard is 31 — Load without a race.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change the two named tests so each target is a target id only.
3. [ ] Contract `LoadTarget`, its encode and decode, `loadTargetIntent`, and the `includeWorkspace` filter in `wantAnswerForTargets`.
4. [ ] Leave `LoadRefuse.MultiWorkspace` in place.

## Outputs

[ ] `src/Shared/ApiResponses.fs`
[ ] `src/Shared/ApiResponseSerialization.fs`
[ ] `src/Client/UpdateHelpers.fs`
[ ] `src/Shared/ResidentProjection.fs`
[ ] `tests/Shared.Tests/SyncPlannerTests.fs` — change the two tryStartLoad facts

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/30-delete-include-workspace-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[ ] Stop when another file still builds `includeWorkspace`. Name that file in the stop report.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``tryStartLoad emits LoadServer when idle with empty queue``
  - `tests/Shared.Tests`: ``tryStartLoad returns no effects when already loading``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/30-delete-include-workspace-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
