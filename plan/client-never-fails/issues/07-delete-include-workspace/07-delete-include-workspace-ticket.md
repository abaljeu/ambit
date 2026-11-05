# 07 — Delete includeWorkspace
**Status**: coded
One job: Load asks for target ids only, and `includeWorkspace` is gone along one path. `LoadRefuse.MultiWorkspace` stays. The story Delete the whole-Workspace request in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [16 — Poll for nearby Absent Nodes](plan/client-never-fails/issues/16-poll-nearby-absent/16-poll-nearby-absent-ticket.md)

## Inputs

### Working

[x] `UpdateHelpers.loadTargetIntent` in `src/Client/UpdateHelpers.fs` builds `{ targetId }`.
[x] `UpdateHelpers.tryStartLoadFetch` maps selection ids through `loadTargetIntent` and passes those targets to `SyncPlanner.tryStartLoad`.
[x] `LoadTarget` in `src/Shared/ApiResponses.fs` is `{ targetId }`.
[x] `encodeLoadTarget` and `decodeLoadTargetDecoder` in `src/Shared/ApiResponseSerialization.fs` write and read the target id.
[x] `ResidentProjection.wantAnswerForTargets` in `src/Shared/ResidentProjection.fs` answers each target id. `LoadRefuse.MultiWorkspace` is the refusal when the targets span two Workspaces.
[x] [[tests/Server.Tests/ApiPostLoadTests.fs]], [[tests/Shared.Tests/LoadCaptureTests.fs]], [[tests/Shared.Tests/SerializationTests.fs]], and [[tests/Shared.Tests/SyncPlannerTests.fs]] assert the target id answer.
[x] The facts are ``postLoad returns Events and the target Header``, ``postLoad Want answer for the target``, ``postLoad missing target returns Events without Nodes``, ``postLoad shares one event id for Events and Nodes``, ``postLoad same Workspace multi-target dedupes answer Nodes``, ``postLoad refuses selection spanning two Workspaces``, ``wantAnswerForTargets returns a leaf target Header``, ``wantAnswerForTargets returns the target Header and its child list``, ``wantAnswerForTargets missing target returns empty``, ``wantAnswerForTargets refuses targets across Workspaces``, ``captureLoadResponse shares event id for Changes and Nodes``, ``LoadRequest round-trip``, ``tryStartLoad emits LoadServer when idle with empty queue``, and ``tryStartLoad returns no effects when already loading``.
[x] [[src/Client/UpdateHelpers.fs]] — `loadTargetIntent` and `tryStartLoadFetch`.
[x] [[src/Shared/ApiResponses.fs]] — `LoadTarget` is `{ targetId }`.
[x] [[src/Shared/ApiResponseSerialization.fs]] — encode and decode a target id only.
[x] [[src/Shared/ResidentProjection.fs]] — `wantAnswerForTargets` answers the target ids. `LoadRefuse.MultiWorkspace` stays.

### Reference

[x] Story order in [[plan/client-never-fails/arch.md]]. Path 8 is this ticket. `LoadRefuse.MultiWorkspace` stays.

## Do not load

[x] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[x] Closed: `ChildrenStatus` and tests that build Unloaded Nodes.
[x] Closed: the raced Load guard. That guard is the next ticket, 08 — Load without a race.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] `loadTargetIntent` builds a target id only, and `tryStartLoadFetch` passes those targets.
3. [x] `LoadTarget` is `{ targetId }`. Encode and decode write and read that id.
4. [x] `wantAnswerForTargets` answers the target ids. `LoadRefuse.MultiWorkspace` stays.
5. [x] The four test files named in Inputs assert the target id answer.

## Outputs

[x] `src/Client/UpdateHelpers.fs`
[x] `src/Shared/ApiResponses.fs`
[x] `src/Shared/ApiResponseSerialization.fs`
[x] `src/Shared/ResidentProjection.fs`
[x] `tests/Server.Tests/ApiPostLoadTests.fs`
[x] `tests/Shared.Tests/LoadCaptureTests.fs`
[x] `tests/Shared.Tests/SerializationTests.fs` — ``LoadRequest round-trip``
[x] `tests/Shared.Tests/SyncPlannerTests.fs`

## Stop when

[x] Record the stop at `plan/client-never-fails/reports/07-delete-include-workspace-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[x] Stop when another file still names `includeWorkspace`. Name that file in the stop report.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``postLoad returns Events and the target Header``
  - `tests/Server.Tests`: ``postLoad Want answer for the target``
  - `tests/Server.Tests`: ``postLoad missing target returns Events without Nodes``
  - `tests/Server.Tests`: ``postLoad shares one event id for Events and Nodes``
  - `tests/Server.Tests`: ``postLoad same Workspace multi-target dedupes answer Nodes``
  - `tests/Server.Tests`: ``postLoad refuses selection spanning two Workspaces``
  - `tests/Shared.Tests`: ``wantAnswerForTargets returns a leaf target Header``
  - `tests/Shared.Tests`: ``wantAnswerForTargets returns the target Header and its child list``
  - `tests/Shared.Tests`: ``wantAnswerForTargets missing target returns empty``
  - `tests/Shared.Tests`: ``wantAnswerForTargets refuses targets across Workspaces``
  - `tests/Shared.Tests`: ``captureLoadResponse shares event id for Changes and Nodes``
  - `tests/Shared.Tests`: ``LoadRequest round-trip``
  - `tests/Shared.Tests`: ``tryStartLoad emits LoadServer when idle with empty queue``
  - `tests/Shared.Tests`: ``tryStartLoad returns no effects when already loading``
[x] All executed tests pass.
[x] Write the verdict to `plan/client-never-fails/reports/07-delete-include-workspace-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
