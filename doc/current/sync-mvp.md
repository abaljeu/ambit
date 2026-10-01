# Multi-client sync

Category: Capability

See Also

[Server](server.md)

[Persistence model](persistence-model.md)

[HTTP contract](doc/api.md)

Endpoint and JSON detail for the implemented API.

[Undo](doc/undo.md)

Multi-client sync is last-write-wins by arrival order on the server, and the running server and client use this baseline.

## Job

[x] The server applies the last write by arrival order.
[x] The client does not merge.
[x] A later submit can overwrite a concurrent edit from another client.
[ ] Graph-to-Browser residency: want-driven.

## Protocol

[x] The client sends a `ChangeBatch` to `POST /{pathname}/changes`, for example `POST /ambit/changes`.
[x] The server applies each change in order against authoritative state. The revision must match `Change.id`.
[x] When the graph changes, the server increments `revision`, appends a row to the PostgreSQL `changes` table, and auto-persists affected document artifacts under `DataDir`.
[x] The server responds with the complete `ChangeSuccessResponse`: revision, real deploy and page stamps, readiness, `externalChanges = false`, durable confirmation Changes in `c`, and an optional persistence message. The response does not include the full Graph.
[x] The Browser uses the Post response only for confirmation reconciliation. The Browser does not apply `c` as a Poll tail.
[x] The Browser polls `GET /{pathname}/poll?rev=N`, for example every 5 seconds or after activity, for remote Changes and build stamps.
[x] When the client is behind, Poll returns the same response type with its Change tail in `c`. The Browser applies that list locally. The full Graph comes from `GET /{pathname}/state` on initial Load or on resync.
[ ] Multi-document routes live under `/documents/{docId}`.
[ ] The server can push on a WebSocket instead of poll, or in addition to poll.

## Endpoints

[x] `GET /ambit/state` returns `{ revision, graph }`.
[x] `POST /ambit/changes` accepts a `ChangeBatch` and returns the complete Change success envelope with confirmation Changes.
[x] `GET /ambit/poll?rev=N` returns the same Change success envelope with a Poll tail.
[x] `Change.id` equals the server revision at apply time.
[x] `changeId` makes retry idempotent. The same id returns the same ack and does not apply the change twice.
[x] Success: HTTP 200. Body fields: `r`, `b`, `p`, `ready`, `externalChanges`, and `c`.
[x] Post uses `c` as confirmation data only. Poll uses `c` as the remote Change tail. Both channels use [ApiResponseSerialization.fs](src/Shared/ApiResponseSerialization.fs). The two channels remain separate Browser paths.
[x] Failure: HTTP 400 with `{ "error": "…" }` for an invalid op, a revision mismatch, an empty batch, or a similar error.
[x] A revision mismatch returns 400. The client catches up through poll or `GET /state`.
[ ] Sync uses sequence-based concurrency and returns 409 for a stale response.
[x] Undo and redo stay on the client. The client applies inverses locally and posts them in a `ChangeBatch` like any other edit.
[ ] The server exposes undo and redo endpoints with explicit conflict rules.

## State

[x] `revision`: an int and increases monotonically.
[x] `graph`: the current authoritative Graph.
[x] `history`: the in-process change history. It mirrors applied ops.

## Message log

[x] The append-only change log is persisted in the PostgreSQL `changes` table. `payload`: the full change JSON for each accepted batch.
[x] On startup the server replays from the log after the stored revision checkpoint.
[x] In-process `History` mirrors applied changes for the running process. `History` is not the durable store.
[o] After each accepted change, the server commits to the database and auto-persists correlated document artifacts under `DataDir`.
[x] Persistence runs automatically after an accepted change.

## Explanation

A submit omits the full graph, so the response stays small. Undo and redo server endpoints are deferred, so history stays on the client and inverse ops travel in a normal batch. Fewer than five clients edit, and edits are infrequent, so the MVP accepts that overwrite.
