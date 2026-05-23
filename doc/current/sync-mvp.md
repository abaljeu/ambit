# Multi-Client Sync – MVP (implemented baseline)

Current baseline used by the running server/client. See the **Implemented API** section in [[doc/api.md]] for endpoint and JSON details. This document describes sync semantics; it can evolve toward the **Target API** in [[doc/api.md]] later.

## Principle

Last write wins by arrival order on the server. No client-side merging required.

## Protocol

1. Client sends a `ChangeBatch` to `POST /{pathname}/changes` (e.g. `POST /ambit/changes`).
2. Server applies each change in order against authoritative state (revision must match `Change.id`).
3. If changed, server increments `revision`, appends to durable log (file `.log` and/or PostgreSQL `changes`).
4. Server responds with `ChangeBatchAck` (`revision`, `ackedChangeIds`) — **not** the full graph.
5. Client polls `GET /{pathname}/poll?rev=N` (e.g. every 5s or after activity) for remote changes and build stamps.
6. When behind, poll returns a change tail in `c`; client applies it locally. Full graph via `GET /{pathname}/state` on initial load or resync.

## Why this is simpler

- No conflict resolution on client.
- No full graph on every submit (smaller responses; graph via state endpoint when needed).
- Poll carries incremental `changes` when the client revision lags.
- Undo/redo server endpoints are deferred; history is client-local with inverse ops in normal batches.

## Trade-offs accepted for MVP

- A concurrent edit from another client can be silently overwritten by later submits.
  Acceptable because N<5 and edits are infrequent.
- Revision mismatch returns `400` (client must catch up via poll or `GET /state`).
- Undo/redo are not separate HTTP endpoints; see [[doc/undo.md]].

## Server state

```
revision : int                   -- monotonically increasing
graph    : Graph                 -- current authoritative graph
history  : History               -- in-process change history (mirrors applied ops)
```

Durable history is **not** only in-memory: see **Message log** below.

## Endpoints (implemented)

Canonical reference: [[doc/api.md]].

| Method | Path | Role |
|--------|------|------|
| `GET` | `/ambit/state` | `{ revision, graph }` |
| `POST` | `/ambit/changes` | `ChangeBatch` → `{ revision, ackedChangeIds }` |
| `GET` | `/ambit/poll?rev=N` | `{ r, b, p, c }` — revision, build stamps, change tail |

There is **no** `POST /submit` (formerly returned full graph in the response).

### `POST /ambit/changes`

**Request** (example):

```json
{
  "changes": [
    {
      "id": 0,
      "changeId": "550e8400-e29b-41d4-a716-446655440000",
      "ops": [ … ]
    }
  ]
}
```

- `Change.id` must equal server revision at apply time.
- `changeId` enables idempotent retry (same id → same ack, no double-apply).

**Success** (200):

```json
{
  "revision": 1,
  "ackedChangeIds": [ "550e8400-e29b-41d4-a716-446655440000" ]
}
```

**Failure** (400): `{ "error": "…" }` (invalid op, revision mismatch, empty batch, etc.).

### Undo / redo

Undo/redo remain **client-local**. The client applies inverses locally and posts them in `ChangeBatch` like any other edit. Server `POST /undo` and `POST /redo` are not implemented.

## Message log

Append-only change log is **persisted**:

- **File mode**: `data/{doc}.log` (one JSON change per line); snapshot + `.meta` revision.
- **DB mode**: PostgreSQL `changes` table (same payload concept).

On startup the server replays from the log after the last snapshot checkpoint. In-process `History` mirrors applied changes for the running process but is not the durable store.

Persistence mode: `Persistence:Mode` (`db` default, `file` rollback). See [[doc/arch.md]] and [[doc/roadmap/persistence-vs-domain-model.md]].

There is **no** `POST /save`; snapshots are written asynchronously by agents after accepted changes.

## Migration path

When ready for the target contract in [[doc/api.md]]:

1. Add sequence-based concurrency and `409` stale responses.
2. Multi-document routes under `/documents/{docId}`.
3. Optional WebSocket push instead of or in addition to poll.
4. Server-side undo/redo endpoints with explicit conflict rules.
