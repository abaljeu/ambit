# 07 — Send child list on command response scope

Slice: Api.nodesForRequest sends each Node's Header and its child list. UniversalResponse carries that list. Encode and decode carry it.

## Signatures

[ ] ``Api.nodesForRequest`` in `src/Server/Api.fs` maps ids to resident Headers.
[ ] `UniversalResponse` is `{ nodes; events; latestId }` in `src/Shared/ApiResponses.fs`. It has no child list today.
[ ] `encodeUniversalResponse` and `decodeUniversalResponseDecoder` in `src/Shared/ApiResponseSerialization.fs`.

## Match sites

[ ] `nodesForRequest` reads Headers only.
[ ] Universal response encode uses `Serialization.encodeNode` and omits a child list.

## Existing tests

[ ] ``UniversalResponse round-trips nodes events and latestId`` in [[tests/Shared.Tests/SerializationTests.fs]] round-trips the record without a child list.

## Copy-from

[ ] `SyncResponse.childMap` in `src/Shared/ApiResponses.fs` is the child-list field already used on Poll and post replies.

## write-ticket

### Source files

[ ] [[src/Server/Api.fs]] — `nodesForRequest` includes each Node's child list of ids.
[ ] [[src/Shared/ApiResponses.fs]] — `UniversalResponse` carries that list.
[ ] [[src/Shared/ApiResponseSerialization.fs]] — encode and decode carry it.

### Tests

[ ] Change ``UniversalResponse round-trips nodes events and latestId`` so the child list round-trips.
[ ] Add ``nodesForRequest returns Header and child list`` in `tests/Server.Tests/ApiGetStateTests.fs`.

## Size

[ ] Source files: 3. Under the limit of 8.
[ ] Tests added or changed: 2. Under the limit of 4.
