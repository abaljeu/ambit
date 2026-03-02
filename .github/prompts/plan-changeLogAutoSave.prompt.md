## Plan: Single-File Server with Change Log + Auto-Save

**TL;DR**: Refactor server to use `GET /{file}/state` / `POST /{file}/changes`, backed by a `MailboxProcessor` for strict in-order handling. Changes are appended to a binary-header indexed log file before responding to the client. Snapshots are written asynchronously with coalescing (write `{file}.tmp` → rename over `{file}`). Client derives its filename from `window.location.pathname` and drops the save button.

**Steps**

1. **Add `ChangeLog` module to Shared** ([Gambol.Shared.fsproj](src/Shared/Gambol.Shared.fsproj))
   - New file `ChangeLog.fs` in the Shared project
   - Log file format: each record is UTF. `[8-byte change id (LE int32)][UTF-8 JSON bytes]` — Each record is a single `Change`.  one newline per record.
   - On startup scan: read 8-byte header, skip `length` bytes, record the file offset of each entry into an `int64 ResizeArray` — O(N) once, then O(1) lookup by change number
   - Functions: `appendChange (stream: FileStream) (change: Change) -> unit` (serialize + write header + payload + flush), `readIndex (stream: FileStream) -> int64 ResizeArray` (startup scan building offset table), `readChangeAt (stream: FileStream) (offset: int64) -> Change` (seek + read one entry)
   - Serialization uses `Thoth.Json.Core` encoders from [Serialization.fs](src/Shared/Serialization.fs) — but the actual `Encode.toString` / `Decode.fromString` call lives *in the Server project* (Thoth.Json.Newtonsoft), so `ChangeLog.fs` defines the format/framing and the Server calls it with the concrete backend

2. **Split Server into multiple files** — compile order in `.fsproj`: `Types.fs → FileAgent.fs → Api.fs → Server.fs`

   **`Types.fs`** — shared server types
   - `Program` marker type (for `WebApplicationFactory<Program>` in tests)
   - Thoth `Encode`/`Decode` module aliases (`Thoth.Json.Newtonsoft`)

   **`FileAgent.fs`** — `MailboxProcessor`-based file concurrency
   - `FileAgentMsg` DU: `GetState of AsyncReplyChannel<string>`, `PostChange of string * AsyncReplyChannel<Result<string, string>>`, `SnapshotDone`
   - `FileAgent` class wrapping a `MailboxProcessor`:
     - Mutable state: `{ state: State; revision: Revision; logStream: FileStream; offsetIndex: int64 ResizeArray; snapshotInProgress: bool; snapshotNeeded: bool; dataDir: string; filename: string }`
   - **Change handling order** (inside mailbox loop):
     1. Parse + validate change (try-apply against current state)
     2. If invalid → reply error
     3. If valid → append to `{filename}.log` via `ChangeLog.appendChange`, flush
     4. Reply success with new revision + graph JSON
     5. Update in-memory state + revision
     6. Trigger snapshot if not already in progress
   - **Snapshot coalescing**: `startSnapshot` fires `Task.Run` that writes `{filename}.tmp` + `{filename}.meta.tmp` (containing the revision number), then atomically renames both (`.meta.tmp` → `.meta`, `.tmp` → `{filename}`). Posts `SnapshotDone` back to mailbox. On `SnapshotDone`: if `snapshotNeeded`, start another.
   - **Startup**: load `{filename}` snapshot (or empty graph), read revision N from `{filename}.meta` (default 0 if missing), open/create `{filename}.log`, replay log entries where `change.id >= N`, build offset index
   - **Dispose**: flush + close log stream

   **`Api.fs`** — HTTP route handler functions
   - `getState (agent: FileAgent)` → JSON response
   - `postChange (agent: FileAgent) (body: string)` → JSON response or 400
   - Encode/decode helpers (`encodeStateJson`, `submitDecoder`)

   **`Server.fs`** — entry point + wiring
   - `Main.main`: `WebApplication` builder, `resolveDataDir`, route registration
   - One agent at a time: mutable `currentAgent: (string * FileAgent) option`. If `{file}` differs, dispose old, create new.
   - **Routes**:
     - `GET /{file}/state` → `Api.getState agent`
     - `POST /{file}/changes` → `Api.postChange agent body`
     - `GET /{file}` → serve `index.html`
     - Remove `/state`, `/submit`, `/save`
   - **File creation**: `GET /{file}/state` for nonexistent file returns empty single-root graph (no file created). File + log are only created on first `POST /{file}/changes`.
   - Remove old `ServerState` record, `switchSnapshotFile`, lock-based concurrency

3. **Client URL-based file discovery** — [Program.fs](src/Client/Program.fs)
   - Extract filename from `window.location.pathname` (strip leading `/`)
   - Fetch from `/{file}/state`, post to `/{file}/changes`
   - Store `file: string` in model or just a module-level binding

4. **Simplify client POST body** — [Update.fs](src/Client/Update.fs)
   - `encodeSubmitBody` → just `Serialization.encodeChange change |> Encode.toString`  (drop `clientRevision` wrapper)
   - Rename `postJson "/submit"` calls to `postJson $"/{file}/changes"`
   - `decodeSubmitResponse` → decode `{ revision, graph }` (same shape as state response — reuse `decodeStateResponse`)

5. **Remove save button** — [Model.fs](src/Client/Model.fs), [View.fs](src/Client/View.fs), [index.html](src/Server/wwwroot/index.html)
   - Remove `SaveRequested` from `Msg` DU
   - Remove save button wiring in `render`
   - Remove `<button id="save-button">` from HTML
   - Remove `SaveRequested` handler in [Update.fs](src/Client/Update.fs)

6. **Update tests** — [StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs)
   - Change all `getStateJson` to `GET /{file}/state` (use a test filename like `"test"`)
   - Change `postSubmit` to `POST /{file}/changes` with plain `Change` body
   - Remove `postSave` and all save-specific tests
   - Add new tests: log file exists after first change, snapshot file written after change, startup replay (create agent → post change → dispose → create new agent with same dataDir → verify state)
   - Keep temp-dir isolation pattern

**Verification**
- `dotnet test --no-build --no-restore` — all existing tests pass with new endpoints
- Manual: navigate to `localhost:5000/test`, edit a node, verify `data/test.log` appears with binary-framed entries, verify `data/test` snapshot appears shortly after
- Manual: kill server, restart, navigate to same file — state is restored from snapshot + log replay

**Decisions**
- Single log file with UTF 8-byte headers .  Each line will be a numbered JSON Change object.In-memory offset index built on startup scan gives O(1) runtime lookup
- MailboxProcessor instead of lock — strict ordering, non-blocking snapshot via `Task.Run` + `SnapshotDone` postback
- `ChangeLog.fs` in Shared defines framing logic but delegates Thoth encode/decode to Server (respects the split Thoth backend architecture)
- No log truncation — log is permanent, will serve as undo history later
