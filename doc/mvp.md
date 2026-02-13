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
- [ ] JSON serialization (encode/decode for all shared types)

### Server
- [ ] In-memory `ServerState` (graph + version)
- [ ] `GET /` serves HTML page + compiled client JS
- [ ] `GET /state` returns `{ version, graph }` as JSON
- [ ] `POST /op/apply` accepts `{ version, change }`, returns `{ version, graph }`
- [ ] Append-only message log (in-memory list for MVP, no file persistence yet)
- [ ] Last-write-wins sync per [[sync-mvp]]

### Client (Fable → JS)
- [ ] On load: `GET /state`, build local model
- [ ] Render outline: recursive indented `<div>`s from graph
- [ ] Click line → select it (highlight, set cursor)
- [ ] Hidden `<input>` captures keystrokes
- [ ] Typing → `SetText` op on selected node
- [ ] Enter → `NewNode` + `Replace` (insert sibling)
- [ ] Tab → `Replace` (reparent under previous sibling)
- [ ] Shift+Tab → `Replace` (reparent under grandparent)
- [ ] After each edit: apply locally, POST to server in background
- [ ] On POST response: replace local graph + version from server

### Persistence (minimal)
- [ ] On server shutdown / periodic: write snapshot to disk
- [ ] Snapshot = single text outline file (tabs for indentation)
- [ ] On server startup: load snapshot file if present → rebuild graph

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
| Ops log file persistence | In-memory log only; snapshot is sufficient |

## Implementation order

Each step is a deliverable that can be reviewed and tested.

### Step 1: JSON serialization (Shared)
- Encode/decode `NodeId`, `Node`, `Graph`, `Op`, `Change`, `Revision`
- Round-trip tests
- Must work in both .NET and Fable

### Step 2: Server endpoints
- `GET /` returns a static HTML page (hardcoded or from file)
- `GET /state` returns graph + version as JSON
- `POST /op/apply` applies change, returns new graph + version
- In-memory server state, no persistence yet
- Test with curl / HTTP tests

### Step 3: Client rendering
- Fable compiles to JS, served by `GET /`
- On load: fetch state, render outline as indented divs
- Click to select a line (visual highlight)
- No editing yet — read-only view

### Step 4: Client editing – text
- Hidden `<input>` element captures typing
- On input → `SetText` op → apply locally → re-render line
- POST change to server in background

### Step 5: Client editing – structure
- Enter → create new node, insert as sibling
- Tab → indent (reparent)
- Shift+Tab → outdent (reparent)
- Each structural edit = `NewNode` + `Replace` ops in a `Change`

### Step 6: Persistence
- Server writes snapshot file on shutdown (or periodic timer)
- Server loads snapshot file on startup
- Text outline format with tab indentation
- Test: edit → restart server → edits survive

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

