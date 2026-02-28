# API Contract

HTTP API for current MVP client-server communication.

## Revision Tracking

- **Revision**: Monotonically increasing integer representing the state version
- Server maintains authoritative revision
- Each change increments revision
- Client tracks its local revision
- Client must send its revision with each request

## Transaction Log (History)

The server uses the shared `History` type (from `State`) as its transaction log — they are the same object. All applied changes are appended to `History.past`:
- Server processes changes in order; the history preserves the full linear record
- Enables: future undo/redo, load snapshot → replay, debugging
- In-memory only for MVP; disk persistence is a future enhancement

## Endpoints

### `GET /`
Returns HTML page with client application.

**Response**: `text/html`

---

### `GET /state`
Get current graph state and revision.

**Response** (JSON):
```json
{
  "graph": { ... },
  "revision": 42
}
```

**Fields**:
- `graph`: Full graph structure (see JSON encoding)
- `revision`: Current server revision

---

### `POST /submit`
Apply a change (batch of ops) to the graph.

**Request** (JSON):
```json
{
  "clientRevision": 40,
  "change": {
    "id": 0,
    "ops": [ ... ]
  }
}
```

**Fields**:
- `clientRevision`: Client's current revision (currently accepted as advisory in MVP)
- `change`: Change to apply (see Change JSON encoding)

**Response** (JSON):
```json
{
  "revision": 42,
  "graph": { ... }
}
```

**Fields**:
- `revision`: Current server revision after handling the request
- `graph`: Current authoritative graph

**Error Cases**:
- `400 Bad Request`: Invalid change (e.g., op validation failed)
  - Returns error message

**Client Behavior**:
1. Send change with current `clientRevision`
2. On success: update local revision from `revision`
3. Keep local graph (already optimistic) or replace with returned `graph`
4. If `400 Bad Request`: show error, do not retry

---

### `POST /undo`
Undo the most recent change.

**Request** (JSON):
```json
{
  "clientRevision": 42
}
```

**Status**: Deferred in current MVP (endpoint not implemented).

---

### `POST /redo`
Redo the most recent undone change.

**Request** (JSON):
```json
{
  "clientRevision": 42
}
```

**Status**: Deferred in current MVP (endpoint not implemented).

---

### `POST /save`
Write current graph state to snapshot file on disk.

**Request**: No body required.

**Response** (JSON):
```json
{
  "success": true,
  "snapshotFile": "gambol-snapshot.txt"
}
```

**Error Response** (500, JSON):
```json
{
  "success": false,
  "error": "Failed to write snapshot: ..."
}
```

**Notes**:
- This is the only way to persist state to disk. Edits are not auto-saved.
- Snapshot is a tab-indented text outline file (see Snapshot module).
- On server restart, only the last saved snapshot is restored. Unsaved edits are lost.

---

### `GET /ops?since={revision}`
Get all changes since a given revision.

**Status**: Deferred in current MVP (endpoint not implemented).

**Query Parameters**:
- `since`: Revision number to fetch changes after

**Response** (JSON):
```json
{
  "changes": [
    {
      "id": 14,
      "ops": [ ... ],
      "revision": 41
    },
    {
      "id": 15,
      "ops": [ ... ],
      "revision": 42
    }
  ],
  "latestRevision": 42
}
```

**Fields**:
- `changes`: Array of changes since `since` revision (may be empty)
- `latestRevision`: Current server revision

**Use Case** (future): Client polling for remote changes, or resync after conflict

---

## JSON Encoding

### NodeId
```json
"550e8400-e29b-41d4-a716-446655440000"
```
Guid as string.

### Op
```json
{
  "type": "NewNode",
  "nodeId": "550e8400-e29b-41d4-a716-446655440000",
  "text": "node text"
}
```

```json
{
  "type": "SetText",
  "nodeId": "550e8400-e29b-41d4-a716-446655440000",
  "oldText": "old",
  "newText": "new"
}
```

```json
{
  "type": "Replace",
  "parentId": "550e8400-e29b-41d4-a716-446655440000",
  "index": 0,
  "oldIds": ["id1", "id2"],
  "newIds": ["id3", "id4", "id5"]
}
```

### Change
```json
{
  "id": 15,
  "ops": [
    { "type": "NewNode", ... },
    { "type": "SetText", ... }
  ]
}
```

**Note**: Client-provided `id` is passed through in MVP; there is no server-side reassignment yet.

### Node
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "text": "node text",
  "children": ["id1", "id2", "id3"]
}
```

### Graph
```json
{
  "root": "550e8400-e29b-41d4-a716-446655440000",
  "nodes": {
    "550e8400-e29b-41d4-a716-446655440000": {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "text": "root",
      "children": ["id1", "id2"]
    },
    "id1": { ... },
    "id2": { ... }
  }
}
```

**Note**: `nodes` is an object/map keyed by NodeId string.

---

## Multi-Client Sync Protocol (Current MVP)

### Assumptions
- Maximum 5 concurrent clients (N<5)
- All clients operate on the same model
- Server is authoritative
- Changes are applied optimistically on client, then synced to server

### Client Sync Flow

1. **Initial Load**:
   - `GET /state` → get graph + current revision
   - Store locally as baseline

2. **Local Edit**:
   - User makes edit → create `Change` with ops
   - Apply optimistically to local state
   - Queue change for server sync

3. **Sync to Server**:
   - `POST /submit` with `clientRevision` and queued change
  - On success: update local revision from response
  - On `400`: surface error and keep local state unchanged

4. **Polling for Remote Changes**:
  - Not part of current MVP (no `GET /ops?since=` endpoint yet)

### Conflict Resolution

Advanced conflict handling is deferred for MVP. Current behavior is effectively last-write-wins by arrival order.

### Transaction Log Structure

The server's transaction log is the shared `History` type inside `State`. `History.past` is a list of `Change` records (newest first). Each `Change` has an `id` and a list of `Op`s.

The server additionally tracks `revision` (on `ServerState`) which is bumped with each applied change.

**Note**: Log is in-memory only for MVP. Disk persistence (for crash recovery
and load → replay) is a future enhancement.

---

## Error Codes

- `200 OK`: Success
- `400 Bad Request`: Invalid request (malformed JSON, invalid op, etc.)
- `500 Internal Server Error`: Server error

---

## Notes

- All timestamps in ISO 8601 format
- All GUIDs as lowercase strings without braces
- Server does not currently reassign change IDs
- Revision starts at 0
- Empty arrays should be `[]`, not omitted

