# Minimum Viable Product

A tracer-bullet implementation: the thinnest slice that demonstrates
the full stack working end-to-end as an editable outliner.

## What the user can do

Open a browser, see an outline, edit it.  Specifically:

1. See a tree of text lines rendered as an indented outline.
2. Click a line to select it.
3. Type to edit the selected line's text.
4. Press Enter to create a new sibling node below the selected line.
5. Press Tab to indent (make child of previous sibling).
6. Press Shift+Tab to outdent (move up one level).
7. Edits persist across page reload.

That's it.  This is enough to demonstrate the full data path:
client → ops → server → persistence → reload.

## What is included

### Shared (already mostly done)
- [x] `Node`, `NodeId`, `Graph` types
- [x] `Op` (NewNode, SetText, Replace)
- [x] `Change`, `History`, `State`
- [x] `Op.apply`, `Op.undo`, `Change.apply`, `Change.undo`
- [x] `History.applyChange`, `History.undo`, `History.redo`
- [x] `Revision` type
- [x] JSON serialization (encode/decode for all shared types)

### Server
- [ ] In-memory `ServerState` (shared `State` with graph + history, plus revision)
- [ ] `GET /` serves HTML page + compiled client JS
- [ ] `GET /state` returns `{ revision, graph }` as JSON
- [ ] `POST /submit` accepts `{ clientRevision, change }`, returns `{ revision, graph }`
- [ ] `POST /save` writes snapshot to disk on explicit request
- [ ] History serves as transaction log (in-memory; enables future undo/redo and replay)
- [ ] Last-write-wins sync per [[sync-mvp]]

### Client (Fable → JS)
- [ ] On load: `GET /state`, build local model
- [ ] Render outline: recursive indented `<div>`s from graph
- [ ] Click line → select it (highlight); two modes: selection and edit (like Excel)
- [ ] Hidden `<input>` captures keystrokes in selection mode
- [ ] Typing or F2 → edit mode; inline `<input>` in selected row
- [ ] Enter commits edit (`SetText` op on selected node); Escape cancels
- [ ] Enter → `NewNode` + `Replace` (insert sibling)
- [ ] Tab → `Replace` (reparent under previous sibling)
- [ ] Shift+Tab → `Replace` (reparent under grandparent)
- [ ] After each edit: apply locally, POST to server in background
- [ ] On POST response: update local revision (optimistic — local graph is already correct)

### Persistence (minimal)
- [ ] Snapshot written only on explicit `POST /save` (not on every edit)
- [ ] Snapshot = single text outline file (tabs for indentation)
- [ ] On server startup: load snapshot file if present → rebuild graph
- [ ] History (in-memory, same as transaction log) records every applied change for future undo/redo and replay

### Tests
- [ ] Elements of the above that can be easily tested with XUnit shall have basic tests defined.

## What is excluded from MVP

| Excluded | Why |
|---|---|
| Multi-file persistence | Single file is enough to prove the path |
| `#label` / `[[wikilink]]` parsing | Not needed for basic editing |
| `name` field on nodes | Deferred; all MVP nodes are text-only |
| Undo/redo UI | Undo logic exists in Shared; no keybinding yet |
| Folding (collapse/expand) | All nodes shown expanded |
| Multiple files / pages | Single outline |
| Polling / real-time sync | Single client sufficient for demo |
| Conflict merging | Last-write-wins is enough |
| HTTPS | HTTP only for local dev |
| Styled UI | Functional appearance only |
| `GET /ops?since=` endpoint | Not needed without polling |
| Transaction log file persistence | In-memory log only; disk persistence is a future enhancement |

## Implementation order

Each step is a deliverable that can be reviewed and tested.

### Step 0: Establish shared data models ✓
- [x] `NodeId`, `Revision` (struct wrappers)
- [x] `Node`, `Graph`, `Graph` module (create, newNode, setText, replace)
- [x] `Op`, `Change`, `History`, `State`, `ApplyResult`
- [x] `Op.apply/undo`, `Change.apply/undo`
- [x] `History.applyChange/undo/redo`
- [x] `ModelBuilder` helpers + `createDag12` test fixture
- [x] Unit tests for Model, ModelBuilder, History

### Step 1: JSON serialization (Shared) ✓
- [x] Encode/decode `NodeId`, `Node`, `Graph`, `Op`, `Change`, `Revision`
- [x] Round-trip tests (9 tests via Thoth.Json.Newtonsoft)
- [x] Works in both .NET and Fable (Thoth.Json.Core in Shared)

### Step 2: Persistence ✓
- [x] `Snapshot.write`: Graph → tab-indented text outline
- [x] `Snapshot.read`: tab-indented text → new Graph (fresh NodeIds)
- [x] XUnit tests: format verification, round-trip consistency, file I/O (10 tests)

### Step 3: Server endpoints
- [x] `GET /` returns a static HTML page (hardcoded or from file)
- [x] `GET /state` returns graph + revision as JSON
- [x] `POST /submit` applies change, appends to transaction log, returns new graph + revision
- [x] `POST /save` writes snapshot to disk (explicit save, not on every edit)
- [x] On startup: load snapshot if present; transaction log starts empty
- [x] Test with xunit HTTP tests

### Step 4: Client rendering
- [x] Fable compiles to JS, served by `GET /`
- [x] On load: fetch state, render outline as indented divs
- [x] No editing yet — read-only view

### Step 5: Client editing – text
Two UI modes (like Excel): **selection mode** and **edit mode**.
See [[mvpstep5]] for detailed design.

**Selection mode** (default):
- [ ] Click a row to select it (`.selected` highlight); at most one selected row
- [ ] Hidden `<input>` (off-screen) retains keyboard focus to capture keystrokes
- [ ] Typing a printable character → enter edit mode (input starts with that character, replacing)
- [ ] F2 → enter edit mode (input starts with the row's current text, appending)

**Edit mode**:
- [ ] Inline `<input>` appears inside the selected row (replaces `.text` content), receives focus
- [ ] User edits text freely in the input
- [ ] Enter → **commit**: if text changed, create `SetText` op → apply locally → re-render → return to selection mode
- [ ] Escape → **cancel**: revert to original text → return to selection mode
- [ ] Click another row → commit current edit, select the clicked row

**Server sync** (optimistic post):
- [ ] After each committed edit (text changed), POST change to server in background
- [ ] On response: update local revision (local graph is already correct)
- [ ] On no response: repost with usual retry protocol

### Step 6: Client editing – structure
- Enter → create new node, insert as sibling
- Tab → indent (reparent)
- Shift+Tab → outdent (reparent)
- Each structural edit = `NewNode` + `Replace` ops in a `Change`

## Success criteria

The MVP is complete when:

1. `dotnet run` starts the server
2. Opening `http://localhost:5115` shows an outline
3. Clicking a line selects it
4. Typing changes the selected line's text
5. Enter creates a new line
6. Tab/Shift+Tab changes indentation
7. Refreshing the page shows the current state
8. Restarting the server preserves the outline

## Architecture notes

### Server (`src/Server/Program.fs`)

- `namespace Gambol.Server` with `type Program = class end` marker for `WebApplicationFactory`
- `ServerState` — mutable record holding shared `State` (graph + history) and `revision`, behind `ServerState.withLock`
- `ServerState.resolveDataDir` reads `DataDir` from `IConfiguration`, defaults to `../../data` relative to content root
- `ServerState.create` takes `dataDir` and `snapshotFile`, loads snapshot if present
- `Api.getState` encodes state as JSON and returns `IResult`
- `Api.submit` decodes `{ clientRevision, change }`, applies via `History.applyChange`, bumps revision, returns `{ graph, revision }` or 400 on error
- `Api.save` writes snapshot to disk, accepts optional `{ "filename": "..." }`, returns `{ success, snapshotFile }` or 500 on error
- `Main.main` — ASP.NET minimal API entry point, routes: `GET /state`, `POST /submit`, `POST /save`

### Client (`src/Client/Program.fs`)

- Fable-compiled to `src/Server/wwwroot/` via `dotnet fable src/Client/Gambol.Client.fsproj -o src/Server/wwwroot`
- **No MSBuild property for outDir** — Fable 5 alpha requires `-o` on the CLI
- Uses `Thoth.Json.JavaScript` (v0.4.1) as the Fable backend for `Thoth.Json.Core` decoders
- Renders outline: each node is a `.row` div containing N `.indent` divs (for depth) + a `.text` div
- CSS in separate `wwwroot/style.css`: `#ccc` global background, `#eee` row background, `0.2rem` gaps

### Tests (`tests/Server.Tests/StateEndpointTests.fs`)

- Each test creates a `WebApplicationFactory<Program>` with `DataDir` overridden to an empty temp directory (no snapshot interference)
- Helpers: `getStateJson`, `decodeRevision`, `decodeGraph`, `encodeSubmitBody`, `postSubmit`, `postSave`, `addChild`
- `createClientForDir` allows save tests to verify files in the temp directory

### Thoth.Json backend split

- **Shared** (`Thoth.Json.Core` v0.7.1) — abstract encoders/decoders, compiles on both .NET and Fable
- **Server** (`Thoth.Json.Newtonsoft` v0.3.3) — .NET concrete backend
- **Client** (`Thoth.Json.JavaScript` v0.4.1) — Fable/JS concrete backend
- **Tests** (`Thoth.Json.Newtonsoft` v0.3.3) — same as server

### Process management

- **Don't run the server from agent commands.** Tests use `WebApplicationFactory` (in-process, no port). Manual browser testing is done by the user in their own terminal.
- The server locks DLLs while running — stop it (Ctrl+C) before building or running tests.

## Design decisions

- **JSON on the server**: Shared uses `Thoth.Json.Core` (abstract). Server needs `Thoth.Json.Newtonsoft` to produce JSON strings.
- **MVP response shape**: `POST /submit` returns `{ graph, revision }` — no `remoteChanges`, `changeId`, or conflict detection. Grow the contract later.
- **Thread safety**: Simple lock around mutable state. Sufficient for single-client MVP.
- **Persistence**: Snapshot only on explicit `POST /save`. History (which is the transaction log) is in-memory only. Disk persistence of the log is a future enhancement.
- **NewNode undo**: All three `Op` cases are reversible from their structure. `NewNode` undo removes the node from the graph's nodes map.
- **Data directory**: `data/` at repo root, gitignored. Path configurable via `appsettings.json` `DataDir` key. Tests override to temp dir.

## Key file paths

| What | Path |
|---|---|
| Server entry point | `src/Server/Program.fs` |
| Server config | `src/Server/appsettings.json` |
| Client entry point | `src/Client/Program.fs` |
| Client project | `src/Client/Gambol.Client.fsproj` |
| HTML | `src/Server/wwwroot/index.html` |
| CSS | `src/Server/wwwroot/style.css` |
| Compiled JS | `src/Server/wwwroot/Program.js` (Fable output) |
| Server tests | `tests/Server.Tests/StateEndpointTests.fs` |
| Snapshot data | `data/gambol-snapshot.txt` |
| Fable compile command | `dotnet fable src/Client/Gambol.Client.fsproj -o src/Server/wwwroot` |
