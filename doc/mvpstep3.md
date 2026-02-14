# Step 3: Server endpoints

## Current status

### Done

- **`GET /state`** endpoint — returns `{ revision, graph }` as JSON (Thoth serializers)
- **`GET /`** — serves `index.html` + `style.css` + Fable-compiled `Program.js`
- **Server state** — `ServerState` record holding a shared `State` (graph + history) plus `revision`, initialized from snapshot or fresh graph. The `History` inside `State` serves as the transaction log.
- **Data directory** — `data/` at repo root, configured via `DataDir` in `appsettings.json` (relative to content root). 
- **Client rendering** — Fable client fetches `/state`, decodes graph with shared Thoth decoders (`Thoth.Json.JavaScript`), renders outline as CSS-classed divs
- **Integration tests** — `Server.Tests` project with `WebApplicationFactory<Program>`, isolated via temp data dir override
- **`POST /submit`** — accepts `{ clientRevision, change }`, applies change via `History.applyChange`, bumps revision, returns `{ graph, revision }`. Returns 400 for invalid JSON or failed ops.
  - A `Change` is `{ id: int, ops: Op list }`. The three `Op` cases:
    - **`NewNode(nodeId, text)`** — add a new node to the graph (no parent link yet)
    - **`SetText(nodeId, oldText, newText)`** — change a node's text (old-text guard)
    - **`Replace(parentId, index, oldIds, newIds)`** — splice a parent's children list (old-span guard)
  - Typical client edits combine these into a single `Change`:
    - *Edit text* — one `SetText` op
    - *Enter (new sibling)* — `NewNode` + `Replace` (insert into parent's children)
    - *Tab (indent)* — `Replace` on old parent (remove) + `Replace` on new parent (insert)
    - *Shift+Tab (outdent)* — `Replace` on old parent (remove) + `Replace` on grandparent (insert)
- **`POST /save`** — writes snapshot to disk. Optional `{ "filename": "..." }` body to save to a different file. Defaults to the file loaded at startup.
- **Configurable snapshot file** — `SnapshotFile` in config (defaults to `gambol-snapshot.txt`). `ServerState` tracks `dataDir` and `snapshotFile`.
- **All 48 tests pass** (36 shared + 12 server)

**Step 3 is complete.** All server endpoints are implemented and tested.

