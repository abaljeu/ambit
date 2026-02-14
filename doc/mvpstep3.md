# Step 3: Server endpoints

## Codebase context

Shared library (already complete):
- `Model.fs` — `NodeId`, `Revision`, `Node`, `Graph` types and operations
- `History.fs` — `Op`, `Change`, `History`, `State`, `ApplyResult` plus apply/undo logic
- `Serialization.fs` — Thoth.Json encoders/decoders for all types
- `Snapshot.fs` — `Snapshot.write` (graph → tab-indented text), `Snapshot.read` (text → graph)
- `ModelBuilder.fs` — test helpers, `createDag12` fixture

Server (skeleton only):
- `Program.fs` — bare ASP.NET Core minimal API, serves static files, placeholder `/api/hello`
- `wwwroot/index.html` — minimal HTML shell loading `Program.js`

## What Step 3 delivers

### Endpoints

- **`GET /`** — already works (serves `index.html` via `UseDefaultFiles`/`UseStaticFiles`). No change needed.
- **`GET /state`** — returns `{ graph, revision }` as JSON using Thoth serializers.
- **`POST /op/apply`** — accepts `{ clientRevision, change }`, applies the change, bumps revision, returns `{ graph, revision }`. MVP: single client, last-write-wins, no conflict detection.
- **`POST /save`** — writes current graph to snapshot file on disk. Returns success/failure.

### Server state

In-memory `ServerState` holding:
- `graph: Graph` — current graph
- `revision: Revision` — monotonically increasing version
- `transactionLog: Change list` — append-only log of all applied changes

Initialized on startup from snapshot file if present, otherwise a fresh empty graph.
Protected by a simple lock (single-client MVP, no agents/channels needed).

### Transaction log

Each `POST /op/apply` appends the `Change` to an in-memory append-only transaction log.
This is **separate from** the `History` undo/redo stack — it records every change the
server has ever applied (since last startup), in order.

Purpose:
- Foundation for future server-side undo/redo (replay/reverse transactions)
- Foundation for "load snapshot → replay transactions" crash recovery
- Debugging and audit trail

For MVP the log is in-memory only — it is lost on server restart.
Persisting the log to disk is a future enhancement.

### Persistence

- **Not** on every edit. Snapshot written only on explicit `POST /save` request.
- On startup: load snapshot file if present → rebuild graph. Transaction log starts empty.
- Snapshot path: `gambol-snapshot.txt` in working directory (configurable later).
- After restart, any edits not explicitly saved are lost. Acceptable for MVP.

### Tests

A new `Server.Tests` xunit project with HTTP integration tests using `WebApplicationFactory`:
- `GET /state` returns valid graph JSON
- `POST /op/apply` modifies state and bumps revision
- `POST /save` writes snapshot to disk
- Startup with existing snapshot restores state
- Startup without snapshot starts fresh

## Design decisions

- **JSON on the server**: Shared uses `Thoth.Json.Core` (abstract). Server needs `Thoth.Json.Newtonsoft` to produce JSON strings. Add this dependency to `Gambol.Server.fsproj`.
- **MVP response shape**: `POST /op/apply` returns `{ graph, revision }` — no `remoteChanges`, `changeId`, or conflict detection. Grow the contract later.
- **Thread safety**: Simple lock around mutable state. Sufficient for single-client MVP.

## Implementation steps

1. Add `Thoth.Json.Newtonsoft` to `Gambol.Server.fsproj`
2. Rewrite `src/Server/Program.fs` with `GET /state`, `POST /op/apply`, `POST /save`, snapshot load on startup, transaction log
3. Create `tests/Server.Tests/` project with integration tests
4. Add the test project to the solution
5. Build and run tests
