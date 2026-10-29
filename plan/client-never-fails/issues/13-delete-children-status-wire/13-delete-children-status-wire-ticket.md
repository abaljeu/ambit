# 13 — Delete the childrenStatus wire
**Status**: 
One job: The wire for a Node carries its child list and omits `childrenStatus`, `loaded`, and `unloaded`. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [12 — Drop the residency children line](plan/client-never-fails/issues/12-drop-residency-children-line/12-drop-residency-children-line-ticket.md)

## Inputs

### Working

[ ] `Serialization.encodeChildrenStatus` and `Serialization.decodeChildrenStatus` in `src/Shared/Serialization.fs`.
[ ] `decodeNodeWire` reads optional `childrenStatus` and defaults a missing field to Loaded. Unloaded with a non-empty `children` list is `Error` of a string.
[ ] `childMapFromWires` drops Unloaded nodes from `childMap`.
[ ] Modern `encodeGraph` already writes a top-level `childMap` and omits per-node `childrenStatus` on that path.
[ ] Legacy package decode still reads the field.
[ ] ``Graph childMap Unloaded is absent after round-trip`` in [[tests/Shared.Tests/SerializationTests.fs]] expects Unloaded after a graph round-trip.
[ ] ``legacy Graph decode without childrenStatus defaults to Loaded`` in `tests/Shared.Tests/SerializationTests.fs` expects Loaded when the field is missing.
[ ] ``legacy package decode rejects Unloaded with non-empty children`` in `tests/Shared.Tests/SerializationTests.fs` expects `Error` of a string that mentions Unloaded.
[ ] ``StateResponse decode accepts Alan sample node shapes`` in `tests/Shared.Tests/SerializationTests.fs` expects Loaded on a legacy special Node.
[ ] Top-level `childMap` encode is the form the decoder keeps.
[ ] [[src/Shared/Serialization.fs]] — delete the field and the two wire values.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/13-delete-children-status-wire-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: the `ChildrenStatus` type in `src/Shared/Model.fs`. That delete is 25 — Delete ChildrenStatus.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Contract `src/Shared/Serialization.fs`: delete `childrenStatus`, `loaded`, and `unloaded` from encode and decode.
3. [ ] Keep a child id list on the wire. A child id with no Header stays available to the Graph.
4. [ ] Change the four facts named in Outputs.

## Outputs

[ ] `src/Shared/Serialization.fs`
[ ] `tests/Shared.Tests/SerializationTests.fs` — change the four facts

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/13-delete-children-status-wire-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[ ] Stop when another Serialization fact compares exact JSON that still contains `childrenStatus` and fixing it would change a fifth test.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``Graph childMap Unloaded is absent after round-trip``
  - `tests/Shared.Tests`: ``legacy Graph decode without childrenStatus defaults to Loaded``
  - `tests/Shared.Tests`: ``legacy package decode rejects Unloaded with non-empty children``
  - `tests/Shared.Tests`: ``StateResponse decode accepts Alan sample node shapes``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/13-delete-children-status-wire-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
