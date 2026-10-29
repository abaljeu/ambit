# 07 — Delete includeWorkspace
**Status**: 
One job: Load asks for target ids only, and `includeWorkspace` is gone along one path. `LoadRefuse.MultiWorkspace` stays. The story Delete the whole-Workspace request in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [16 — Poll for nearby Absent Nodes](plan/client-never-fails/issues/16-poll-nearby-absent/16-poll-nearby-absent-ticket.md)

## Inputs

### Working

[ ] `UpdateHelpers.loadTargetIntent` in `src/Client/UpdateHelpers.fs` builds `LoadTarget` and sets `includeWorkspace` from `Graph.isLoaded`.
[ ] `UpdateHelpers.tryStartLoadFetch` is the caller. It maps selection ids through `loadTargetIntent` and passes the targets to `SyncPlanner.tryStartLoad`.
[ ] `LoadTarget` in `src/Shared/ApiResponses.fs` is `{ targetId; includeWorkspace }`.
[ ] `encodeLoadTarget` and `decodeLoadTargetDecoder` in `src/Shared/ApiResponseSerialization.fs` read and write `includeWorkspace`.
[ ] `ResidentProjection.wantAnswerForTargets` in `src/Shared/ResidentProjection.fs` keeps a target id when `includeWorkspace` is true, then calls `wantAnswer`. `LoadRefuse.MultiWorkspace` is the refusal when the targets span two Workspaces.
[ ] 23 mentions of `includeWorkspace` sit in [[tests/Server.Tests/ApiPostLoadTests.fs]] (10), [[tests/Shared.Tests/LoadCaptureTests.fs]] (6), [[tests/Shared.Tests/SerializationTests.fs]] (4), and [[tests/Shared.Tests/SyncPlannerTests.fs]] (3).
[ ] The facts are ``postLoad Ev-only when includeWorkspace false``, ``postLoad Want answer when includeWorkspace true``, ``postLoad missing target returns Events without Nodes``, ``postLoad shares one event id for Events and Nodes``, ``postLoad same Workspace multi-target dedupes answer Nodes``, ``postLoad refuses selection spanning two Workspaces``, ``wantAnswerForTargets without residency intent returns empty``, ``wantAnswerForTargets returns direct Children and pointed-at Nodes``, ``wantAnswerForTargets missing target returns empty``, ``wantAnswerForTargets refuses targets across Workspaces``, ``captureLoadResponse shares event id for Changes and Nodes``, ``LoadRequest round-trip``, ``tryStartLoad emits LoadServer when idle with empty queue``, and ``tryStartLoad returns no effects when already loading``.
[ ] [[src/Client/UpdateHelpers.fs]] — `loadTargetIntent` and `tryStartLoadFetch`.
[ ] [[src/Shared/ApiResponses.fs]] — delete the `includeWorkspace` field.
[ ] [[src/Shared/ApiResponseSerialization.fs]] — encode and decode a target id only.
[ ] [[src/Shared/ResidentProjection.fs]] — `wantAnswerForTargets` answers the target ids. Delete the `includeWorkspace` filter.

### Reference

[ ] Story order in [[plan/client-never-fails/arch.md]]. Path 8 is this ticket. `LoadRefuse.MultiWorkspace` stays.

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: `ChildrenStatus` and tests that build Unloaded Nodes.
[ ] Closed: the raced Load guard. That guard is the next ticket, 08 — Load without a race.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `loadTargetIntent` so it builds a target id only, and change `tryStartLoadFetch` to pass those targets.
3. [ ] Contract `LoadTarget` by deleting `includeWorkspace`. Change encode and decode to the target id only.
4. [ ] Change `wantAnswerForTargets` so it answers the target ids. Delete the `includeWorkspace` filter. Leave `LoadRefuse.MultiWorkspace` in place.
5. [ ] Change the 23 `includeWorkspace` mentions in the four test files named in Inputs.

## Outputs

[ ] `src/Client/UpdateHelpers.fs`
[ ] `src/Shared/ApiResponses.fs`
[ ] `src/Shared/ApiResponseSerialization.fs`
[ ] `src/Shared/ResidentProjection.fs`
[ ] `tests/Server.Tests/ApiPostLoadTests.fs` — 10 mentions
[ ] `tests/Shared.Tests/LoadCaptureTests.fs` — 6 mentions
[ ] `tests/Shared.Tests/SerializationTests.fs` — 4 mentions in ``LoadRequest round-trip``
[ ] `tests/Shared.Tests/SyncPlannerTests.fs` — 3 mentions

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/07-delete-include-workspace-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[ ] Stop when another file still names `includeWorkspace`. Name that file in the stop report.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``postLoad Ev-only when includeWorkspace false``
  - `tests/Server.Tests`: ``postLoad Want answer when includeWorkspace true``
  - `tests/Server.Tests`: ``postLoad missing target returns Events without Nodes``
  - `tests/Server.Tests`: ``postLoad shares one event id for Events and Nodes``
  - `tests/Server.Tests`: ``postLoad same Workspace multi-target dedupes answer Nodes``
  - `tests/Server.Tests`: ``postLoad refuses selection spanning two Workspaces``
  - `tests/Shared.Tests`: ``wantAnswerForTargets without residency intent returns empty``
  - `tests/Shared.Tests`: ``wantAnswerForTargets returns direct Children and pointed-at Nodes``
  - `tests/Shared.Tests`: ``wantAnswerForTargets missing target returns empty``
  - `tests/Shared.Tests`: ``wantAnswerForTargets refuses targets across Workspaces``
  - `tests/Shared.Tests`: ``captureLoadResponse shares event id for Changes and Nodes``
  - `tests/Shared.Tests`: ``LoadRequest round-trip``
  - `tests/Shared.Tests`: ``tryStartLoad emits LoadServer when idle with empty queue``
  - `tests/Shared.Tests`: ``tryStartLoad returns no effects when already loading``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/07-delete-include-workspace-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
