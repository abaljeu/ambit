# API Contract

HTTP API for client-server communication with multi-client sync support (N<5 clients).

## Revision Tracking

- **Revision**: Monotonically increasing integer representing the state version
- Server maintains authoritative revision
- Each change increments revision
- Client tracks its local revision
- Client must send its revision with each request

## Message Log

All messages sent to server are appended to an append-only message log:
- Each message includes: client version, timestamp, operation
- Server processes messages in order
- Log enables replay and debugging

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

### `POST /op/apply`
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
- `clientRevision`: Client's current revision (must match server or be behind)
- `change`: Change to apply (see Change JSON encoding)

**Response** (JSON):
```json
{
  "success": true,
  "newRevision": 42,
  "changeId": 15,
  "remoteChanges": [
    {
      "id": 14,
      "ops": [ ... ],
      "revision": 41
    }
  ]
}
```

**Fields**:
- `success`: Whether the change was accepted
- `newRevision`: New server revision after applying change
- `changeId`: Server-assigned change ID
- `remoteChanges`: Array of changes from other clients that occurred since `clientRevision` (may be empty)

**Error Response** (409 Conflict, JSON):
```json
{
  "success": false,
  "error": "revision_mismatch",
  "serverRevision": 45,
  "remoteChanges": [
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
  ]
}
```

**Error Cases**:
- `409 Conflict`: Client revision is behind server (server has newer changes)
  - Server returns all changes since client's revision
  - Client must merge these changes locally, then retry
- `400 Bad Request`: Invalid change (e.g., op validation failed)
  - Returns error message, no remote changes

**Client Behavior**:
1. Send change with current `clientRevision`
2. If `success: true`: update local revision to `newRevision`, apply any `remoteChanges`
3. If `409 Conflict`: apply `remoteChanges` locally, update revision, retry original change
4. If `400 Bad Request`: show error, do not retry

---

### `POST /op/undo`
Undo the most recent change.

**Request** (JSON):
```json
{
  "clientRevision": 42
}
```

**Response**: Same as `POST /op/apply` (undo is treated as a change)

---

### `POST /op/redo`
Redo the most recent undone change.

**Request** (JSON):
```json
{
  "clientRevision": 42
}
```

**Response**: Same as `POST /op/apply` (redo is treated as a change)

---

### `GET /ops?since={revision}`
Get all changes since a given revision.

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

**Use Case**: Client polling for remote changes, or resync after conflict

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

**Note**: Client sends `id: 0` for new changes; server assigns actual ID.

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

## Multi-Client Sync Protocol

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
   - `POST /op/apply` with `clientRevision` and queued change
   - If `success: true`:
     - Update local revision to `newRevision`
     - Apply any `remoteChanges` from response
     - Remove change from queue
   - If `409 Conflict`:
     - Apply all `remoteChanges` to local state
     - Update local revision to `serverRevision`
     - Retry queued change with new revision
     - If retry fails again, may need full resync

4. **Polling for Remote Changes** (optional, for real-time sync):
   - Periodically `GET /ops?since={localRevision}`
   - Apply received changes to local state
   - Update local revision

### Conflict Resolution

When client receives `409 Conflict`:
1. Apply all `remoteChanges` in order to local state
2. Update local revision to `serverRevision`
3. Re-apply any pending local changes (rebased on new state)
4. Retry original request

**Note**: Since ops are designed to be mergeable (e.g., `SetText` with old/new validation), conflicts should be rare. If a change cannot be applied after merging remote changes, client should show error to user.

### Message Log Structure

Server maintains append-only log:
```
[
  { "timestamp": "...", "clientId": "...", "revision": 1, "change": {...} },
  { "timestamp": "...", "clientId": "...", "revision": 2, "change": {...} },
  ...
]
```

Each entry:
- `timestamp`: When message was received
- `clientId`: Optional client identifier (for debugging)
- `revision`: Revision after applying this change
- `change`: The change that was applied

---

## Error Codes

- `200 OK`: Success
- `400 Bad Request`: Invalid request (malformed JSON, invalid op, etc.)
- `409 Conflict`: Revision mismatch (client behind server)
- `500 Internal Server Error`: Server error

---

## Notes

- All timestamps in ISO 8601 format
- All GUIDs as lowercase strings without braces
- Server assigns change IDs sequentially
- Revision starts at 0 or 1 (TBD)
- Empty arrays should be `[]`, not omitted

