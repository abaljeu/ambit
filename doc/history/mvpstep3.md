# Step 3: Server endpoints

> **Historical.** Tracer-bullet notes from early MVP. For current behavior see [[doc/arch.md]] and the implemented section of [[doc/api.md]].

## Current status

### Done

- **`GET /state`** endpoint — returns `{ revision, graph }` as JSON (Thoth serializers)
- **`GET /`** — serves `index.html` + `style.css` + Fable-compiled `Program.js`
- **Server state** — `ServerState` record holding a shared `State` (graph + history) plus `revision`, initialized from snapshot or fresh graph. The `History` inside `State` serves as the transaction log.
- **Data directory** — `data/` at repo root, configured via `DataDir` in `appsettings.json` (relative to content root). 
- **Client rendering** — Fable client fetches `/state`, decodes graph with shared Thoth decoders (`Thoth.Json.JavaScript`), renders outline as CSS-classed divs
- **Integration tests** — `Server.Tests` project with `WebApplicationFactory<Program>`, isolated via temp data dir override
- **`POST /submit`** (was MVP name; now **`POST /ambit/changes`**) — accepts `ChangeBatch`, returns `{ revision, ackedChangeIds }` (no graph in response). Returns 400 for invalid JSON, revision mismatch, or failed ops.
  - A `Change` is `{ id: int, ops: Op list }`. The three `Op` cases:
    - **`NewNode(nodeId, text)`** — add a new node to the graph (no parent link yet)
    - **`SetText(nodeId, oldText, newText)`** — change a node's text (old-text guard)
    - **`Replace(parentId, index, oldIds, newIds)`** — splice a parent's children list (old-span guard)
  - Typical client edits combine these into a single `Change`:
    - *Edit text* — one `SetText` op
    - *Enter (new sibling)* — `NewNode` + `Replace` (insert into parent's children)
    - *Tab (indent)* — `Replace` on old parent (remove) + `Replace` on new parent (insert)
    - *Shift+Tab (outdent)* — `Replace` on old parent (remove) + `Replace` on grandparent (insert)
- **`POST /save`** — removed; snapshots are written automatically after accepted changes (see [[doc/api.md]]).
- **Configurable snapshot file** — `SnapshotFile` in config (defaults to `gambol-snapshot.txt`). `ServerState` tracks `dataDir` and `snapshotFile`.
- **All 48 tests pass** (36 shared + 12 server)

**Step 3 is complete.** All server endpoints are implemented and tested.

