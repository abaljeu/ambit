# 06 — Server sends each Node with its child list
**Status**: coded
One job: Alan's clarification of 2026-11-02 states the return for a requested NodeId. Was: the Server returns the children of that Node and their content, the children's Headers. Is: the Server returns that Node's own content, its Header, and its children as a list of child ids. The children's content is not returned. A child id with no Header in the Browser is Absent and shows as a placeholder. `wantAnswer`, `visibleClosureWantAnswer`, and `Api.nodesForRequest` return that package for each requested id. Every returned Node carries its child list, so it is Loaded. `ChildrenStatus` stays.

Type: coding
Blockers: [05 — Fill a placeholder](plan/client-never-fails/issues/05-fill-placeholder/05-fill-placeholder-ticket.md)

## Inputs

### Working

[x] `ResidentProjection.wantAnswer` and `ResidentProjection.visibleClosureWantAnswer` in `src/Shared/ResidentProjection.fs` build the Poll, post, and boot answers.
[x] `Api.nodesForRequest` in `src/Server/Api.fs` maps requested ids to resident Headers.
[x] `UniversalResponse` in `src/Shared/ApiResponses.fs` is `{ nodes; events; latestId }` and carries no child list.
[x] `encodeUniversalResponse` and `decodeUniversalResponseDecoder` in `src/Shared/ApiResponseSerialization.fs` round-trip that record.
[x] `SyncResponse.childMap` in `src/Shared/ApiResponses.fs` is the child-list shape already used on Poll and post replies. The list is ids only.
[x] ``wantAnswer keeps a child id when that Header is absent`` in [[tests/Shared.Tests/WantTests.fs]] expects a child id with no Header to leave the edge.
[x] ``UniversalResponse round-trips nodes events and latestId`` in [[tests/Shared.Tests/SerializationTests.fs]] round-trips the record without a child list.
[x] `gitResponse` in `tests/Server.Tests/LoadSaveCommandClientTests.fs` builds `Some { nodes = []; events = []; latestId = EventId.zero }`. Alan's decision of 2026-11-01 adds this file so that helper carries the child list.
[x] ``Poll answer chains the next Included residency Want`` in `tests/Server.Tests/BrowserResidencyClientTests.fs` reads the boot closure and the next Want. Alan's clarification of 2026-11-02 adds this file so the expected answer can follow the return above.
[x] ``wantAnswerForTargets returns the target Header and its child list`` and ``captureLoadResponse shares event id for Changes and Nodes`` in `tests/Shared.Tests/LoadCaptureTests.fs` read the `wantAnswer` package.
[x] ``getState zoom outside ROOT adds owning Workspace`` and ``getState without zoom keeps nested Workspace Unloaded`` in `tests/Server.Tests/ApiGetStateTests.fs` read the boot closure.
[x] ``GET state default returns ROOT closure excluding nested workspace contents`` in `tests/Server.Tests/StateEndpointTests.fs` reads that same closure.
[x] ``postLoad Want answer when includeWorkspace true`` and ``postLoad shares one event id for Events and Nodes`` in `tests/Server.Tests/ApiPostLoadTests.fs` read the `wantAnswer` package. The `includeWorkspace` field stays.
[x] [[src/Shared/ResidentProjection.fs]] — `wantAnswer` and `visibleClosureWantAnswer`.
[x] [[src/Server/Api.fs]] — `nodesForRequest`.
[x] [[src/Shared/ApiResponses.fs]] — `UniversalResponse` gains the child list.
[x] [[src/Shared/ApiResponseSerialization.fs]] — encode and decode carry that list.

### Reference

[x] Story order in [[plan/client-never-fails/arch.md]]. Paths 6.1 and 6.2 are this ticket. `ChildrenStatus` stays.

## Do not load

[x] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[x] Closed: `type ChildrenStatus` and its `Loaded` and `Unloaded` cases in `src/Shared/Model.fs`.
[x] Closed: the wire field `childrenStatus` in `src/Shared/Serialization.fs`.
[x] Closed: `includeWorkspace`. That delete is [07 — Delete includeWorkspace](plan/client-never-fails/issues/07-delete-include-workspace/07-delete-include-workspace-ticket.md).

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change `wantAnswer` and `visibleClosureWantAnswer`. For each requested id that has a Header, return that Header and that Node's child-id list. Leave each child id in the list when that child has no Header. Leave the children's Headers out of the returned nodes.
3. [x] Expand `UniversalResponse` with that child list. Change encode and decode so the list round-trips.
4. [x] Change `nodesForRequest` so each requested id returns that Header and that list.
5. [x] Change a test when its expected answer changes because of the return in step 2. The named tests are:

- ``wantAnswer returns the requested Header and its child list`` in `tests/Shared.Tests/WantTests.fs`
- ``wantAnswer keeps a child id when that Header is absent`` in `tests/Shared.Tests/WantTests.fs`
- ``visibleClosureGraph loads reserved Children and Zoom ancestors`` in `tests/Shared.Tests/WantTests.fs`
- ``visibleClosureGraph bad Zoom stays inside reserved set`` in `tests/Shared.Tests/WantTests.fs`
- ``UniversalResponse round-trips nodes events and latestId`` in `tests/Shared.Tests/SerializationTests.fs`
- ``Poll answer chains the next Included residency Want`` in `tests/Server.Tests/BrowserResidencyClientTests.fs`
- ``wantAnswerForTargets returns the target Header and its child list`` in `tests/Shared.Tests/LoadCaptureTests.fs`
- ``captureLoadResponse shares event id for Changes and Nodes`` in `tests/Shared.Tests/LoadCaptureTests.fs`
- ``getState zoom outside ROOT adds owning Workspace`` in `tests/Server.Tests/ApiGetStateTests.fs`
- ``getState without zoom keeps nested Workspace Unloaded`` in `tests/Server.Tests/ApiGetStateTests.fs`
- ``GET state default returns ROOT closure excluding nested workspace contents`` in `tests/Server.Tests/StateEndpointTests.fs`
- ``postLoad Want answer when includeWorkspace true`` in `tests/Server.Tests/ApiPostLoadTests.fs`
- ``postLoad shares one event id for Events and Nodes`` in `tests/Server.Tests/ApiPostLoadTests.fs`

## Outputs

[x] `src/Shared/ResidentProjection.fs`
[x] `src/Server/Api.fs`
[x] `src/Shared/ApiResponses.fs`
[x] `src/Shared/ApiResponseSerialization.fs`
[x] `tests/Shared.Tests/WantTests.fs` — the four Want tests named in Process step 5, with the two `wantAnswer` names updated to the return
[x] `tests/Shared.Tests/SerializationTests.fs` — change ``UniversalResponse round-trips nodes events and latestId``
[x] `tests/Server.Tests/LoadSaveCommandClientTests.fs` — `gitResponse` carries the child list
[x] `tests/Server.Tests/BrowserResidencyClientTests.fs` — change ``Poll answer chains the next Included residency Want``
[x] `tests/Shared.Tests/LoadCaptureTests.fs` — change ``wantAnswerForTargets returns the target Header and its child list`` and ``captureLoadResponse shares event id for Changes and Nodes``
[x] `tests/Server.Tests/ApiGetStateTests.fs` — change ``getState zoom outside ROOT adds owning Workspace`` and ``getState without zoom keeps nested Workspace Unloaded``
[x] `tests/Server.Tests/StateEndpointTests.fs` — change ``GET state default returns ROOT closure excluding nested workspace contents``. `readState` reads `?scope=full` so a stored Header stays visible.
[x] `tests/Server.Tests/ApiPostLoadTests.fs` — change ``postLoad Want answer when includeWorkspace true`` and ``postLoad shares one event id for Events and Nodes``

## Stop when

[ ] Record a new stop under `plan/client-never-fails/issues/06-server-sends-child-list/reports/`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[ ] Stop when a test must change for a reason other than this spec change.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``wantAnswer returns the requested Header and its child list``
  - `tests/Shared.Tests`: ``wantAnswer keeps a child id when that Header is absent``
  - `tests/Shared.Tests`: ``visibleClosureGraph loads reserved Children and Zoom ancestors``
  - `tests/Shared.Tests`: ``visibleClosureGraph bad Zoom stays inside reserved set``
  - `tests/Shared.Tests`: ``UniversalResponse round-trips nodes events and latestId``
  - `tests/Server.Tests`: ``Poll answer chains the next Included residency Want``
  - `tests/Shared.Tests`: ``wantAnswerForTargets returns the target Header and its child list``
  - `tests/Shared.Tests`: ``captureLoadResponse shares event id for Changes and Nodes``
  - `tests/Server.Tests`: ``getState zoom outside ROOT adds owning Workspace``
  - `tests/Server.Tests`: ``getState without zoom keeps nested Workspace Unloaded``
  - `tests/Server.Tests`: ``GET state default returns ROOT closure excluding nested workspace contents``
  - `tests/Server.Tests`: ``postLoad Want answer when includeWorkspace true``
  - `tests/Server.Tests`: ``postLoad shares one event id for Events and Nodes``
[x] All executed tests pass.
[x] Write the verdict to `plan/client-never-fails/reports/06-server-sends-child-list-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
