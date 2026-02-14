# Multi-Client Sync – MVP (last-write-wins)

Interim design for faster MVP.  Replaces the conflict/merge protocol in [[api]] for now.

## Principle

Last write wins per version.  No client-side merging required.

## Protocol

1. Client sends `(version, change)` to server.
2. Server checks its current version:
   - If `version` matches: apply change, increment version, respond with new version.
   - If server already has change(s) from this version (another client got there first):
     discard the previous change(s) at that version, apply the new change instead,
     respond with new version.
   - If `version` is behind by more than one: reject (client must resync).
3. Server always responds with `(newVersion, currentGraph)` so the client
   can replace its local state entirely.

## Why this is simpler

- No conflict resolution on client.
- No `remoteChanges` array in responses.
- No client-side rebase/retry loop.
- Client receives full graph on every successful POST, so it never drifts.
- Server message log is append-only but may contain overwritten entries
  (marked as superseded).

## Trade-offs accepted for MVP

- A concurrent edit from another client can be silently lost.
  Acceptable because N<5 and edits are infrequent.
- Full graph in every response is wasteful at scale.
  Acceptable because graphs are small during MVP.
- Undo history on server may become inconsistent when a change is discarded.
  Acceptable: undo is per-client in MVP; server undo can be deferred.

## Server State

```
version : int                    -- monotonically increasing
graph   : Graph                  -- current authoritative graph
log     : (version * Change) list  -- append-only, may have superseded entries
```

## Endpoint Changes (vs [[api]])

### `POST /submit`

**Request**:
```json
{
  "version": 5,
  "change": { "id": 0, "ops": [ ... ] }
}
```

**Success Response** (200):
```json
{
  "version": 6,
  "graph": { ... }
}
```

**Stale Response** (409 – client too far behind):
```json
{
  "error": "stale",
  "version": 8,
  "graph": { ... }
}
```

Client behavior on 409: replace local state with returned graph + version,
discard pending local changes.

### `GET /state`

Unchanged.  Returns `{ "version": N, "graph": { ... } }`.

### Undo / Redo

Undo/redo remain client-local for MVP.  Client keeps its own history stack and
sends the resulting inverse change via `POST /submit` like any other edit.

## Message Log Format

Each entry:
```
{ "timestamp": "...", "version": 5, "change": {...}, "superseded": false }
```

When a change is replaced at the same version, the old entry is marked
`"superseded": true` and the new entry is appended.

## Migration Path

When ready for proper merge-based sync ([[api]]):
1. Add `remoteChanges` to response instead of full graph.
2. Client applies remote changes and rebases pending ops.
3. Remove the discard/supersede logic on server.
4. Message log entries are never superseded.

