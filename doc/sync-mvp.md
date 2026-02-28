# Multi-Client Sync – MVP (implemented baseline)

Current baseline used by the running server/client.  This keeps sync simple for MVP and can evolve toward [[api]] later.

## Principle

Last write wins by arrival order on the server.  No client-side merging required.

## Protocol

1. Client sends `(clientRevision, change)` to server.
2. Server applies the change against current authoritative state.
3. If changed, server increments `revision`.
4. Server responds with `(revision, currentGraph)` so the client
   can replace its local state entirely.

## Why this is simpler

- No conflict resolution on client.
- No `remoteChanges` array in responses.
- No client-side rebase/retry loop in MVP.
- Client receives full graph in each submit response.

## Trade-offs accepted for MVP

- A concurrent edit from another client can be silently overwritten by later submits.
  Acceptable because N<5 and edits are infrequent.
- Full graph in every response is wasteful at scale.
  Acceptable because graphs are small during MVP.
- Undo/redo endpoints are deferred; history is in-memory only for now.

## Server State

```
revision : int                   -- monotonically increasing
graph   : Graph                  -- current authoritative graph
history : History                -- in-memory change history in State
```

## Endpoint Changes (vs [[api]])

### `POST /submit`

**Request**:
```json
{
  "clientRevision": 5,
  "change": { "id": 0, "ops": [ ... ] }
}
```

**Success Response** (200):
```json
{
  "revision": 6,
  "graph": { ... }
}
```

No stale/409 branch is currently implemented in MVP.

### `GET /state`

Unchanged.  Returns `{ "revision": N, "graph": { ... } }`.

### Undo / Redo

Undo/redo remain client-local for MVP.  Client keeps its own history stack and
sends the resulting inverse change via `POST /submit` like any other edit.

## Message Log

Durable message-log persistence is deferred in MVP.

## Migration Path

When ready for proper merge-based sync ([[api]]):
1. Add stale detection and explicit client retry/rebase semantics.
2. Add optional `remoteChanges` response shape (or keep full graph by policy).
3. Add durable append-only transaction log persistence.
4. Add undo/redo endpoints with server-side conflict rules.

