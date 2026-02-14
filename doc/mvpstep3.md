# Step 3: Server endpoints

## Current status

### Done

- **`GET /state`** endpoint — returns `{ revision, graph }` as JSON (Thoth serializers)
- **`GET /`** — serves `index.html` + `style.css` + Fable-compiled `Program.js`
- **Server state** — `ServerState` record holding a shared `State` (graph + history) plus `revision`, initialized from snapshot or fresh graph. The `History` inside `State` serves as the transaction log.
- **Data directory** — `data/` at repo root, configured via `DataDir` in `appsettings.json` (relative to content root). Gitignored.
- **Client rendering** — Fable client fetches `/state`, decodes graph with shared Thoth decoders (`Thoth.Json.JavaScript`), renders outline as CSS-classed divs
- **Integration tests** — `Server.Tests` project with `WebApplicationFactory<Program>`, isolated via temp data dir override
- **All 38 tests pass** (36 shared + 2 server)

### Remaining

- **`POST /submit`** — accept `{ clientRevision, change }`, apply change, bump revision, append to history (which is the transaction log), return `{ graph, revision }`
  - A `Change` is `{ id: int, ops: Op list }`. The three `Op` cases:
    - **`NewNode(nodeId, text)`** — add a new node to the graph (no parent link yet)
    - **`SetText(nodeId, oldText, newText)`** — change a node's text (old-text guard)
    - **`Replace(parentId, index, oldIds, newIds)`** — splice a parent's children list (old-span guard)
  - Typical client edits combine these into a single `Change`:
    - *Edit text* — one `SetText` op
    - *Enter (new sibling)* — `NewNode` + `Replace` (insert into parent's children)
    - *Tab (indent)* — `Replace` on old parent (remove) + `Replace` on new parent (insert)
    - *Shift+Tab (outdent)* — `Replace` on old parent (remove) + `Replace` on grandparent (insert)
- **`POST /save`** — write snapshot to disk
- **Tests** for the above endpoints

## Architecture notes

### Server (`src/Server/Program.fs`)

- `namespace Gambol.Server` with `type Program = class end` marker for `WebApplicationFactory`
- `ServerState` — mutable record holding shared `State` (graph + history) and `revision`, behind `ServerState.withLock`
- `ServerState.resolveDataDir` reads `DataDir` from `IConfiguration`, defaults to `../../data` relative to content root
- `ServerState.create` loads `gambol-snapshot.txt` from data dir if present
- `Api.getState` encodes state via `Thoth.Json.Newtonsoft.Encode` + `Thoth.Json.Core.Encode`
- `Main.main` — ASP.NET minimal API entry point

### Client (`src/Client/Program.fs`)

- Fable-compiled to `src/Server/wwwroot/` via `dotnet fable src/Client/Gambol.Client.fsproj -o src/Server/wwwroot`
- **No MSBuild property for outDir** — Fable 5 alpha requires `-o` on the CLI
- Uses `Thoth.Json.JavaScript` (v0.4.1) as the Fable backend for `Thoth.Json.Core` decoders
- Renders outline: each node is a `.row` div containing N `.indent` divs (for depth) + a `.text` div
- CSS in separate `wwwroot/style.css`: `#ccc` global background, `#eee` row background, `0.2rem` gaps

### Tests (`tests/Server.Tests/StateEndpointTests.fs`)

- Each test creates a `WebApplicationFactory<Program>` with `DataDir` overridden to an empty temp directory (no snapshot interference)
- `getStateJson()` helper — does GET, asserts 200 + content-type, returns body
- `decode` helper — wraps `Thoth.Json.Newtonsoft.Decode.fromString` with failwith on error

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
| Larger project context | 'doc/mpv.md' |
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
