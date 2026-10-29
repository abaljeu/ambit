# 06 — Server sends each Node with its child list
**Status**: 
One job: The Server returns every NodeId with its Header and its child list of ids. `ChildrenStatus` stays, and tests that build Unloaded Nodes stay as they are. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason for the child list on each state return.

Type: coding
Blockers: [05 — Fill a placeholder](plan/client-never-fails/issues/05-fill-placeholder/05-fill-placeholder-ticket.md)

## Inputs

### Working

[ ] `ResidentProjection.wantAnswer` and `ResidentProjection.visibleClosureWantAnswer` in `src/Shared/ResidentProjection.fs` build the Poll, post, and boot answers.
[ ] `Api.nodesForRequest` in `src/Server/Api.fs` maps requested ids to resident Headers.
[ ] `UniversalResponse` in `src/Shared/ApiResponses.fs` is `{ nodes; events; latestId }` and carries no child list.
[ ] `encodeUniversalResponse` and `decodeUniversalResponseDecoder` in `src/Shared/ApiResponseSerialization.fs` round-trip that record.
[ ] `SyncResponse.childMap` in `src/Shared/ApiResponses.fs` is the child-list shape already used on Poll and post replies. The list is ids only.
[ ] ``wantAnswer omits an edge with an absent target Node`` in [[tests/Shared.Tests/WantTests.fs]] expects a child id with no Header to leave the edge.
[ ] ``UniversalResponse round-trips nodes events and latestId`` in [[tests/Shared.Tests/SerializationTests.fs]] round-trips the record without a child list.
[ ] [[src/Shared/ResidentProjection.fs]] — `wantAnswer` and `visibleClosureWantAnswer`.
[ ] [[src/Server/Api.fs]] — `nodesForRequest`.
[ ] [[src/Shared/ApiResponses.fs]] — `UniversalResponse` gains the child list.
[ ] [[src/Shared/ApiResponseSerialization.fs]] — encode and decode carry that list.

### Reference

[ ] Story order in [[plan/client-never-fails/arch.md]]. Paths 6.1 and 6.2 are this ticket. `ChildrenStatus` stays.

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: `type ChildrenStatus` and its `Loaded` and `Unloaded` cases in `src/Shared/Model.fs`.
[ ] Closed: the wire field `childrenStatus` in `src/Shared/Serialization.fs`.
[ ] Closed: tests that build Unloaded Nodes. Leave them as they are.
[ ] Closed: `includeWorkspace`. That delete is [07 — Delete includeWorkspace](plan/client-never-fails/issues/07-delete-include-workspace/07-delete-include-workspace-ticket.md).

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `wantAnswer` and `visibleClosureWantAnswer` so each returned Node includes its Header and its child list of ids. A child id with no Header stays in the list.
3. [ ] Expand `UniversalResponse` with that child list. Change encode and decode so the list round-trips.
4. [ ] Change `nodesForRequest` so each returned Node includes its Header and that list.
5. [ ] Change ``wantAnswer omits an edge with an absent target Node`` and ``UniversalResponse round-trips nodes events and latestId``.

## Outputs

[ ] `src/Shared/ResidentProjection.fs`
[ ] `src/Server/Api.fs`
[ ] `src/Shared/ApiResponses.fs`
[ ] `src/Shared/ApiResponseSerialization.fs`
[ ] `tests/Shared.Tests/WantTests.fs` — change ``wantAnswer omits an edge with an absent target Node``
[ ] `tests/Shared.Tests/SerializationTests.fs` — change ``UniversalResponse round-trips nodes events and latestId``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/06-server-sends-child-list-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[ ] Stop when a test that builds an Unloaded Node must change. Leave that test as it is and name it in the stop report.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``wantAnswer omits an edge with an absent target Node``
  - `tests/Shared.Tests`: ``UniversalResponse round-trips nodes events and latestId``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/06-server-sends-child-list-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
