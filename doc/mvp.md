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
- [x] `FileAgent` per file — holds in-memory graph + revision, serialises access
- [x] `GET /gambol` serves `gambol.html` (client app entry point)
- [x] `GET /gambol/state` returns `{ revision, graph }` as JSON
- [x] `POST /gambol/changes` accepts `{ id, ops }`, returns `{ revision, graph }`
- [x] Snapshot written automatically to disk after each accepted change
- [x] Change log appended to `{file}.log` after each accepted change
- [x] On startup: load snapshot + replay log entries that follow it
- [x] Last-write-wins sync per [[sync-mvp]]

### Client (Fable → JS)
- [x] On load: `GET /gambol/state`, build local model
- [x] Render outline: recursive indented `<div>`s from graph
- [x] Click line → select it (highlight); two modes: selection and edit (like Excel)
- [x] Hidden `<input>` captures keystrokes in selection mode
- [x] Typing, F2, or Enter → edit mode; inline `<input>` in selected row
- [x] Enter in edit mode → split node at cursor (`SplitNode` → `NewNode` + `Replace` ± `SetText`)
- [x] Escape → cancel edit, return to selection mode
- [ ] Tab → `Replace` (reparent under previous sibling)
- [ ] Shift+Tab → `Replace` (reparent under grandparent)
- [x] After each structural/text change: apply locally, POST to server in background
- [x] On POST response: update local revision (optimistic — local graph is already correct)

### Persistence (minimal)
- [x] Snapshot written automatically after each accepted change (async, via `FileAgent`)
- [x] Snapshot = single text outline file (tabs for indentation)
- [x] Change log (`{file}.log`) appended after each change; entries prefixed with 8-digit change id
- [x] On server startup: load snapshot + replay any log entries with id ≥ snapshot revision

### Tests
- [x] Elements of the above that can be easily tested with XUnit shall have basic tests defined.

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
- [x] `GET /gambol` serves `gambol.html` (single entry point for the app)
- [x] `GET /gambol/state` returns graph + revision as JSON
- [x] `POST /gambol/changes` applies change, appends to log, writes snapshot, returns new graph + revision
- [x] On startup: load snapshot + replay log if present
- [x] Test with xunit HTTP tests

### Step 4: Client rendering
- [x] Fable compiles to JS, served via `GET /gambol`
- [x] On load: fetch `GET /gambol/state`, render outline as indented divs
- [x] No editing yet — read-only view

### Step 5: Client editing – text ✓
Two UI modes (like Excel): **selection mode** and **edit mode**.
See [[mvpstep5]] for detailed design.

**Selection mode** (default):
- [x] Click a row to select it (`.selected` highlight); at most one selected row
- [x] Hidden `<input>` (off-screen) retains keyboard focus to capture keystrokes
- [x] Typing a printable character → enter edit mode (input starts with that character, replacing)
- [x] F2 → enter edit mode (input starts with the row's current text, appending)

**Edit mode**:
- [x] Inline `<input>` appears inside the selected row (replaces `.text` content), receives focus
- [x] User edits text freely in the input
- [x] Enter → **split**: split node at cursor position; focus moves to start of the second node
- [x] Escape → **cancel**: revert to original text → return to selection mode
- [x] Click another row → commit current edit, select the clicked row

**Server sync** (optimistic post):
- [x] After each committed edit (text changed), POST change to server in background
- [x] On response: update local revision (local graph is already correct)
- [ ] On no response: repost with usual retry protocol (deferred — fire-and-forget for MVP)

### Step 6: Client editing – structure
- [x] Enter (in edit mode) → split node at cursor (`SplitNode` msg → `NewNode` + `Replace` ± `SetText`)
- [ ] Tab → indent (reparent) [ if this is first node in its parent, this is a NO-OP ]
- [ ] Shift+Tab → outdent (reparent) [ if this is a root node, this is a NO-OP.]
- [ ] Each structural edit = `NewNode` + `Replace` ops in a `Change`

## Success criteria

The MVP is complete when:

1. `dotnet run` starts the server
2. Opening `http://localhost:5115/gambol` shows an outline
3. Clicking a line selects it
4. Typing changes the selected line's text
5. Enter creates a new line
6. Tab/Shift+Tab changes indentation
7. Refreshing the page shows the current state
8. Restarting the server preserves the outline

## Architecture notes

### Server (`src/Server/Server.fs`)

- `namespace Gambol.Server` with `type Program = class end` marker for `WebApplicationFactory`
- `FileAgent` — one per file; owns in-memory graph + revision; serialises reads/writes; writes snapshot + log asynchronously
- `Main.resolveDataDir` reads `DataDir` from `IConfiguration`, defaults to `../../data` relative to content root
- `Api.getState` encodes state as JSON and returns `IResult`
- `Api.postChange` decodes `{ id, ops }`, applies via `Change.apply`, bumps revision, writes log + snapshot, returns `{ graph, revision }` or 400 on error
- `Main.main` — ASP.NET minimal API entry point, routes: `GET /gambol`, `GET /gambol/state`, `POST /gambol/changes`

### Client (`src/Client/Program.fs`)

- Fable-compiled to `src/Server/wwwroot/` via `dotnet fable src/Client/Gambol.Client.fsproj -o src/Server/wwwroot`
- **No MSBuild property for outDir** — Fable 5 alpha requires `-o` on the CLI
- Uses `Thoth.Json.JavaScript` (v0.4.1) as the Fable backend for `Thoth.Json.Core` decoders
- Renders outline: each node is a `.row` div containing N `.indent` divs (for depth) + a `.text` div
- CSS in separate `wwwroot/style.css`: `#ccc` global background, `#eee` row background, `0.2rem` gaps

### Tests (`tests/Server.Tests/StateEndpointTests.fs`)

- Each test creates a `WebApplicationFactory<Program>` with `DataDir` overridden to a fresh temp directory
- All tests use `testFile = "gambol"` → routes `GET /gambol/state`, `POST /gambol/changes`
- Helpers: `getStateJson`, `decodeRevision`, `decodeGraph`, `encodeChangeBody`, `postChange`, `addChild`
- `createClientForDir` allows persistence tests to inspect snapshot and log files in the temp directory

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
- **MVP response shape**: `POST /gambol/changes` returns `{ graph, revision }` — no `remoteChanges`, conflict detection. Grow the contract later.
- **Thread safety**: `FileAgent` mailbox serialises all state access per file. Sufficient for single-client MVP.
- **Persistence**: Snapshot and log written automatically after each accepted change. Log entries are prefixed with an 8-digit padded change id for replay ordering.
- **NewNode undo**: All three `Op` cases are reversible from their structure. `NewNode` undo removes the node from the graph's nodes map.
- **Data directory**: `data/` at repo root, gitignored. Path configurable via `appsettings.json` `DataDir` key. Tests override to temp dir.

## Key file paths

| What | Path |
|---|---|
| Server entry point | `src/Server/Server.fs` |
| Server config | `src/Server/appsettings.json` |
| Client entry point | `src/Client/Program.fs` |
| Client project | `src/Client/Gambol.Client.fsproj` |
| HTML | `src/Server/wwwroot/gambol.html` |
| CSS | `src/Server/wwwroot/style.css` |
| Compiled JS | `src/Server/wwwroot/Program.js` (Fable output) |
| Server tests | `tests/Server.Tests/StateEndpointTests.fs` |
| Snapshot data | `data/gambol` |
| Change log | `data/gambol.log` |
| Fable compile command | `dotnet fable src/Client --outDir src/Server/wwwroot --sourceMaps` |
