# 07 — Send child list on command response
**Status**: 
One job: A Command response carries each Node's Header and its child list of ids. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [06 — Send child list on want answer](plan/client-never-fails/issues/06-send-child-list-on-want-answer/06-send-child-list-on-want-answer-ticket.md)

## Inputs

### Working

[ ] ``Api.nodesForRequest`` in `src/Server/Api.fs` maps ids to resident Headers.
[ ] `UniversalResponse` is `{ nodes; events; latestId }` in `src/Shared/ApiResponses.fs`. It has no child list today.
[ ] `encodeUniversalResponse` and `decodeUniversalResponseDecoder` in `src/Shared/ApiResponseSerialization.fs`.
[ ] `nodesForRequest` reads Headers only.
[ ] Universal response encode uses `Serialization.encodeNode` and omits a child list.
[ ] ``UniversalResponse round-trips nodes events and latestId`` in [[tests/Shared.Tests/SerializationTests.fs]] round-trips the record without a child list.
[ ] `SyncResponse.childMap` in `src/Shared/ApiResponses.fs` is the child-list field already used on Poll and post replies.
[ ] [[src/Server/Api.fs]] — `nodesForRequest` includes each Node's child list of ids.
[ ] [[src/Shared/ApiResponses.fs]] — `UniversalResponse` carries that list.
[ ] [[src/Shared/ApiResponseSerialization.fs]] — encode and decode carry it.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/07-send-child-list-on-command-response-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: the wire field `childrenStatus`. That delete is 13 — Delete the childrenStatus wire.
[ ] Closed: `LoadTarget.includeWorkspace`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand `UniversalResponse` with the child list of ids, beside the current fields.
3. [ ] Change encode and decode so the list round-trips.
4. [ ] Change `nodesForRequest` so each returned Node includes its Header and that list. A child id with no Header stays in the list.
5. [ ] Change the round-trip fact and add the server fact named in Outputs.

## Outputs

[ ] `src/Server/Api.fs`
[ ] `src/Shared/ApiResponses.fs`
[ ] `src/Shared/ApiResponseSerialization.fs`
[ ] `tests/Shared.Tests/SerializationTests.fs` — change ``UniversalResponse round-trips nodes events and latestId``
[ ] `tests/Server.Tests/ApiGetStateTests.fs` — add ``nodesForRequest returns Header and child list``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/07-send-child-list-on-command-response-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[ ] Stop when an existing exact-JSON fact fails and fixing it would change a fifth test.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``UniversalResponse round-trips nodes events and latestId``
  - `tests/Server.Tests`: ``nodesForRequest returns Header and child list``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/07-send-child-list-on-command-response-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
