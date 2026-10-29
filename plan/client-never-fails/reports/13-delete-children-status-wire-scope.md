# 13 — Delete the childrenStatus wire scope

Slice: Delete the field childrenStatus and the values loaded and unloaded from the Node wire.

## Signatures

[ ] `Serialization.encodeChildrenStatus` and `Serialization.decodeChildrenStatus` in `src/Shared/Serialization.fs`.
[ ] `decodeNodeWire` reads optional `childrenStatus` and defaults a missing field to Loaded. Unloaded with a non-empty `children` list is `Error` of a string.
[ ] `childMapFromWires` drops Unloaded nodes from `childMap`.

## Match sites

[ ] Modern `encodeGraph` already writes a top-level `childMap` and omits per-node `childrenStatus` on that path.
[ ] Legacy package decode still reads the field.

## Existing tests

[ ] ``Graph childMap Unloaded is absent after round-trip`` in [[tests/Shared.Tests/SerializationTests.fs]] expects Unloaded after a graph round-trip.
[ ] ``legacy Graph decode without childrenStatus defaults to Loaded`` in `tests/Shared.Tests/SerializationTests.fs` expects Loaded when the field is missing.
[ ] ``legacy package decode rejects Unloaded with non-empty children`` in `tests/Shared.Tests/SerializationTests.fs` expects `Error` of a string that mentions Unloaded.
[ ] ``StateResponse decode accepts Alan sample node shapes`` in `tests/Shared.Tests/SerializationTests.fs` expects Loaded on a legacy special Node.

## Copy-from

[ ] Top-level `childMap` encode is the form the decoder keeps.

## write-ticket

### Source files

[ ] [[src/Shared/Serialization.fs]] — delete the field and the two wire values.

### Tests

[ ] Change the four facts so they use the child list and leave `childrenStatus`, `loaded`, and `unloaded` unread.

## Size

[ ] Source files: 1. Under the limit of 8.
[ ] Tests added or changed: 4. At the limit of 4.
