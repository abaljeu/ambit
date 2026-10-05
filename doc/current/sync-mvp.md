# Multi-client sync

Category: Capability

See Also

[Server](server.md)

[Persistence model](persistence-model.md)

[HTTP contract](http-contract.md)

Endpoint and JSON detail for the implemented API.

[Undo](doc/undo.md)

Multi-client sync is last-write-wins by arrival order on the server, and the running server and client use this baseline.

## Job

[x] The server applies the last write by arrival order.
[x] The client does not merge.
[x] A later submit can overwrite a concurrent edit from another client.
[ ] Graph-to-Browser residency: want-driven.

## Protocol

[x] The client posts events and a want list to `POST /ambit/changes`. Wire: [HTTP contract](http-contract.md).
[x] The server applies each new event in order. The same `submissionId` returns the stored event.
[x] When the graph changes, the server appends the event and auto-persists affected document artifacts under `DataDir`. Detail: [Persistence model](persistence-model.md).
[x] The Browser uses the changes response and the poll response on separate paths.
[x] The Browser polls `POST /ambit/poll` on a 5 second interval, on window focus, and when activity wakes an inactive poll. Wire: [HTTP contract](http-contract.md).
[x] When the client is behind, poll returns the event tail. The Browser applies that tail. The graph comes from `GET /ambit/state` on initial load or on resync.
[ ] Multi-document routes live under `/documents/{docId}`.
[ ] The server can push on a WebSocket instead of poll, or in addition to poll.

## Endpoints

[x] State, poll, changes, load, and the change-success JSON: [HTTP contract](http-contract.md).
[ ] Sync uses sequence-based concurrency and returns 409 for a stale response.
[x] Undo and redo stay on the client. The client posts undo and redo events.
[ ] The server exposes undo and redo endpoints with explicit conflict rules.

## State

[x] The server cursor is the event id. The HTTP names are on [HTTP contract](http-contract.md).
[x] `graph`: the current authoritative Graph.
[x] `history`: the in-process change history. It mirrors applied ops.

## Message log

[x] The append-only change log is persisted in the PostgreSQL `changes` table. `payload`: the full change JSON for each accepted batch.
[x] On startup the server replays from the log after the stored revision checkpoint.
[x] In-process `History` mirrors applied changes for the running process. `History` is not the durable store.
[o] After each accepted change, the server commits to the database and auto-persists correlated document artifacts under `DataDir`.
[x] Persistence runs automatically after an accepted change.

## Explanation

A changes response carries the want answer, so the body stays smaller than a full graph. Undo and redo server endpoints are deferred, so history stays on the client and inverse ops travel in a normal event. Fewer than five clients edit, and edits are infrequent, so the MVP accepts that overwrite.
