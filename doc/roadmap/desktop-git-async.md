# Desktop git commands — async client

Category: Client / Sync
Status: Planned
See also: [[git-sync-gateway]], [[workspace-scale-import-slice2-plan]], [[lazy-load]], [[cmd-last-result-format]], [[doc/arch]], [[src/Client/UpdateWorkspaceGit.fs]], [[src/Client/App.fs]], [[src/Shared/ViewModel.fs]], [[src/Shared/SyncPlanner.fs]], [[src/Client/UpdateHelpers.fs]], [[src/Server/FileAgent.fs]], [[src/Desktop/DesktopGitEndpoints.fs]], [[src/Shared/dotnet/DesktopGit.fs]]

G7 delivered desktop git commands over synchronous browser XHR ([[src/Client/JsInterop.fs]] `getJsonSync` / `postJsonSync` / `putJsonSync`). Each Pull, Push, Status, Connect, or Clone run blocks the Fable MVU thread until the desktop host finishes (including stock `git` subprocess work). This plan makes those commands non-blocking on the client, surfaces a visible pending indicator, and updates `#cmd-last-result` on completion — without changing git transport semantics ([[git-sync-gateway]]).

## Existing client→server command queue

Gambol already has a client→server command queue for graph edits. Git async should **plug into this established pipeline**, not add a parallel FIFO field on `VM`.

### Client-side queue and planner

| Piece | Location | Role |
| --- | --- | --- |
| Enqueue | [[src/Client/UpdateHelpers.fs]] `applyAndPost` | Appends a `Change` to `syncInfo.pendingChanges` |
| FIFO + serial gate | [[src/Shared/SyncPlanner.fs]] `tryStartSubmit` / `ackBatch` | One in-flight POST when idle; blocked while `Sending` / `Polling` |
| Dispatch effect | [[src/Shared/ViewModel.fs]] `Effect.SubmitPendingBatch` | Updater returns effect; does not block MVU |
| Async runner | [[src/Client/App.fs]] `runSubmitPendingBatch` | `postJson` to `/{file}/changes`; completion via `SysMsg` |
| Completion | [[src/Client/Update.fs]] `SubmitResponse` handler | `SyncPlanner.ackBatch` dequeues ack'd changes; starts next batch if queue non-empty |
| Persistence | [[src/Client/UpdateHelpers.fs]] `savePendingQueue` / `loadPendingQueue` | localStorage backup of pending queue |

### Server-side serialization

| Piece | Location | Role |
| --- | --- | --- |
| Route | [[src/Server/RouteRegistration.fs]] `POST /ambit/changes` | Receives change batches |
| Agent | [[src/Server/FileAgent.fs]] / [[src/Server/DbAgent.fs]] | `MailboxProcessor` serializes all reads/writes per document |

### Async dispatch shell (reuse for git)

All non-blocking remote work follows the same MVU shell:

```text
User command → Updater returns Effect list (immediate return)
  → App.fs runEffect issues async fetch
  → completion dispatches SysMsg
  → update applies result + may return next Effect
```

Precedents already in production:

- **Graph sync** — `SubmitPendingBatch` → `SubmitResponse` (queued, FIFO, serial)
- **File status** — `RequestDesktopFileStatus` / `RequestServerFileStatus` → `DesktopFileStatusReceived` (single in-flight indicator, no queue)
- **Git Save** — [[src/Client/UpdateSave.fs]] `gitSaveOp` uses async `postEmpty` to `/{file}/save` (fire-and-forget; no `lastCmdResult` yet)

Git ops are **not graph changes** and do not belong in `pendingChanges`. They should use the **same Effect → async HTTP → SysMsg pattern** and, for FIFO serial execution, a **SyncPlanner-shaped module** in Shared — not ad-hoc `gitCommandQueue` on `VM`.

## What it gives you

- Git Pull to Desktop (and later Push, Status, Connect, Clone, Parse/Upload push branch) return immediately from the command handler; the UI stays responsive while desktop and/or server do the work.
- A visible **pending** state in the sticky header while a git command is in flight.
- Completion still writes `lastCmdResult` with the command display name prefix ([[cmd-last-result-format]]): Pull and Push success both use `label → path` (e.g. `home → C:\dev\home`); structured errors (missing mapping, auth, non-FF, dirty server).
- Additional git commands while one is pending enqueue in FIFO order and run after the in-flight op completes (slice 1) — via a Shared planner on the existing Effect/SysMsg pipeline, not a separate VM queue field.
- One established MVU pattern reused across git ops, aligned with graph sync batch submit and desktop file-status polling.

## What it avoids for now

- A parallel `gitCommandQueue` list field on `VM` — FIFO belongs in a Shared planner + Effect dispatch, same shape as [[src/Shared/SyncPlanner.fs]].
- Desktop-side async `git` subprocess or job IDs on `/_desktop/git-*` — HTTP handlers may keep blocking until git finishes; only the **browser client** becomes async.
- Cancellation (`AbortController`, kill git child) — defer until after Pull/Push are stable async.
- Parallel git subprocesses — only one git remote op in flight at a time (slice 1); overlapping subprocess work is not started.
- Overloading `#sync-status` with git state — that pill stays graph HTTP sync only (`Saving…`, `Checking…`, stale alerts).
- Row-level or per-workspace git spinners in the outline.
- Disabling the command palette / keybindings while pending (optional polish later).
- Server **Save** (`gitSaveOp`) last-result wiring — already uses async `postEmpty` but only logs; out of scope unless touched incidentally.
- Lazy Load reactions after pull ([[lazy-load]]) — unchanged; Pull/Push success message format stays `label → path` (Push today is `pushed: <detail>`; align to Pull when wiring async Push).

## What async means here

**Client remains active.** Async does not mean background threads in the browser — it means the command handler returns immediately, dispatches through the existing Effect/SysMsg pipeline, and desktop and/or server execute blocking git subprocess work on their HTTP threads.

| Concern | Today | Target |
| --- | --- | --- |
| Browser main thread | Sync XHR in `UpdateWorkspaceGit` blocks until desktop responds | Updater returns `Effect`; `App.fs` async `fetch` + `SysMsg` callback |
| UI during run | Frozen until complete; no in-flight signal | Pending indicator in `#cmd-last-result` |
| Result bar | Set only after blocking returns | Set pending text on dispatch; replace with `Detail` / `Error` on completion |
| Where git runs | Desktop `/_desktop/git-*` handlers; server `GET /ambit/git-token` | Unchanged — client only stops blocking |
| Desktop HTTP | `task { }` handlers; git subprocess is sync `WaitForExit` | Unchanged for slice 1 |
| Auth prefetch | `getJsonSync "/ambit/git-token"` inside Pull/Push/Clone | Async fetch in effect runner before desktop POST |

Non-blocking UI does **not** require git progress percentages or cancellable operations in v1.

## What runs where

| Step | Runs on | Transport |
| --- | --- | --- |
| User triggers git command | Client MVU | Command registry → updater |
| Preflight validation (focus, mapping, capabilities) | Client MVU | Sync; errors go straight to `lastCmdResult` |
| Enqueue / start remote op | Client MVU | Shared git planner → `Effect` |
| Auth token fetch | Server | `GET /ambit/git-token` (async fetch from client) |
| Pull / Push / Status / Clone / git-remote | Desktop | `POST /_desktop/git-*` (async fetch from client) |
| Workspace mapping PUT | Desktop | `PUT /_desktop/workspace-mappings` |
| Folder picker | Desktop | `POST /_desktop/pick-folder` (native dialog; may stay sync XHR while open) |
| Git subprocess (`git pull`, etc.) | Desktop host process | Inside desktop HTTP handler |
| Graph change sync (orthogonal) | Server FileAgent/DbAgent | Existing `SubmitPendingBatch` queue |

## Command inventory

| Command | Registry name | Entry | Blocking today? | Auth? | Notes |
| --- | --- | --- | --- | --- | --- |
| Git Pull to Desktop | `Git Pull to Desktop` | `gitPullOp` | Yes — sync GET token + sync POST `/_desktop/git-pull` | Optional PAT | Success: `label → localPath` |
| Git Push to Server | `Git Push to Server` | `gitPushOp` | Yes | Optional PAT | Success target: `label → localPath` (same as Pull; today `pushed: <detail>`). Also via Parse/Upload on Workspace |
| Git status | `Git status` | `gitStatusOp` | Yes | No | Formatted ahead/behind/dirty |
| Git Push to New Remote | `Git Push to New Remote` | `gitConnectOp` | Yes — sync pick-folder + PUT mapping + POST git-remote | No | Multi-step chain |
| Git Clone workspace | `Git Clone workspace` | `gitCloneOp` | Yes — pick-folder + clone + mapping + remote | Optional PAT | Longest chain |
| Parse / Upload | `Parse / Upload` | `parseOrPushOp` | Parse: sync GET `/_desktop/file`; Push: same as Push | Push only | Two branches |
| Save | `Save` | `gitSaveOp` | No (async `postEmpty`) | N/A | No `lastCmdResult`; defer |

Desktop endpoints ([[src/Desktop/DesktopGitEndpoints.fs]]): `POST /_desktop/git-remote`, `git-pull`, `git-push`, `git-status`, `git-clone` — no API shape change expected for slice 1.

## Pending UI indicator

### Where it lives

Use **`#cmd-last-result`** in the sticky header ([[src/Server/wwwroot/gambol.template.html]]). It is already command-scoped, sits beside `#sync-status` / `#db-status`, and implements the `Commandname: …` format ([[src/Client/Controller.fs]] `setCmdLastResultDisplay`).

Do **not** repurpose `#sync-status` — that element owns graph change sync (`Sending`, `Polling`, `WaitingToRetry`, stale reload). Mixing git pull state there would conflate unrelated transports.

Do **not** use the row `.amb-file-indicator` (`CheckingFileStatus` → `…`) for git — that indicator is file-reference scoped ([[src/Shared/ViewModelRowState.fs]]), not workspace-git scoped.

### Visual treatment

While pending, render `#cmd-last-result` as:

`Git Pull to Desktop: pulling…`

- Reuse existing last-result element; add CSS class `amb-last-result-pending` (yellow background aligned with `.amb-sync-status.amb-syncing`) so pending is visible without reading the text.
- Set `aria-live="polite"` on `#cmd-last-result` if not already present, so screen readers announce start and completion.

### MVU state — git UX only, queue in planner

Do **not** add `gitCommandQueue` to `VM`. FIFO queuing lives in a Shared planner module (SyncPlanner shape). `VM` carries only what `#cmd-last-result` needs to render in-flight work:

```fsharp
type GitCommandPending = { commandName: string; phase: string }
// phase examples: "pulling…", "pushing…", "connecting…"
```

Field: `gitCommandPending: GitCommandPending option` — **display slot** for the one git remote op currently in flight (covers token prefetch + desktop POST span). Cleared on `DesktopGitOpDone`. Not a second queue.

Shared planner (new, modeled on [[src/Shared/SyncPlanner.fs]]):

```fsharp
type GitRemoteOp = Pull | Push | Status | ...
type GitRemoteState = { pending: (GitRemoteOp * commandName: string) list; inFlight: bool }

module GitRemotePlanner =
    let enqueue ...   // append tail; start effect if idle
    let onDone ...    // clear inFlight; dequeue head; return next effect if any
```

Tie-in with `lastCmdResult`:

| Phase | `gitCommandPending` | `lastCmdResult` | `#cmd-last-result` display |
| --- | --- | --- | --- |
| Idle | `None` | prior value or `None` | Last completed result (or empty) |
| Dispatched | `Some { commandName; phase }` | unchanged or cleared | `commandName: phase` via pending renderer |
| Success | `None` | `Detail (Some commandName, msg)` | `commandName: msg` |
| Error | `None` | `Error (Some commandName, msg)` | `commandName: msg` |
| Cancelled (deferred) | `None` | optional `Detail` / `Error` | cleared or short message |

`renderDiagnostics` ([[src/Client/View.fs]]) checks `gitCommandPending` first; if `Some`, format pending text and skip `lastCmdResult` until cleared. Pending text is driven by planner dispatch / completion, same as `#sync-status` reads `syncInfo.syncState` for graph sync.

### When it clears

- **Success** — desktop POST returns 2xx and payload decodes `ok: true` → clear pending, set `Detail`.
- **Error** — HTTP error, decode failure, desktop `error` field, or preflight validation (no workspace focus, auth 401) → clear pending, set `Error`. Preflight errors that happen before any effect may skip pending entirely.
- **Cancel** — not in slice 1; when added, clear pending and optionally set `Error (Some name, "cancelled")`.

## Client MVU pattern

Mirror graph sync and desktop file-status:

```text
User command → Updater calls GitRemotePlanner.enqueue
  → if started: gitCommandPending set + Effect RequestDesktopGitOp
  → App.fs runEffect issues async fetch (token GET then desktop POST as needed)
  → completion dispatches SysMsg DesktopGitOpDone
  → update calls GitRemotePlanner.onDone; sets lastCmdResult; clears gitCommandPending
  → if planner returns next op: set pending + return next RequestDesktopGitOp effect
```

### New types (Shared)

```fsharp
type DesktopGitOp =
    | Pull of workspace: string * auth: (string * string) option
    | Push of workspace: string * auth: (string * string) option
    | Status of workspace: string
    // Connect / Clone added in later slices

type Effect =
    | RequestDesktopGitOp of op: DesktopGitOp * commandName: string
    // existing effects unchanged

type SystemMsg =
    | DesktopGitOpDone of
        commandName: string *
        result: Result<string, string>  // Ok detail message or Error message
    // existing sys msgs unchanged
```

`commandName` is the registry display string (e.g. `"Git Pull to Desktop"`) so completion does not depend on `CommandId` enum in async callbacks.

### Updater shape (Pull example)

1. Gate: `canDesktopGit`, `requireNamedWorkspace` — sync validation; on error → `fail` with `Error`, no pending, no enqueue.
2. Enqueue via `GitRemotePlanner.enqueue state (Pull (workspace, auth), "Git Pull to Desktop")` — returns updated planner state, optional `gitCommandPending`, and effects (empty when queued behind in-flight op).
3. Effect runner ([[src/Client/App.fs]]): async `fetch` `/_desktop/git-pull` with encoded body; on 2xx decode `DesktopGitOk`; dispatch `DesktopGitOpDone`.
4. `update` handler on `DesktopGitOpDone`: map result to `lastCmdResult`; call `GitRemotePlanner.onDone`; if next op dequeued, set pending and return `[ RequestDesktopGitOp (op, name) ]`.

Auth for Pull/Push/Clone: effect runner performs `GET /ambit/git-token` via async fetch first (same semantics as today's `fetchGitAuth`), then desktop POST — still one pending span covering both hops.

### JsInterop

Prefer existing async `postJson` ([[src/Client/JsInterop.fs]]) for desktop POSTs. Add async `getJson` / `putJson` only if needed for token GET and mapping PUT in later slices. Remove sync calls from git updaters as each slice lands.

### Desktop API

No endpoint changes for slice 1. JSON request/response shapes stay as implemented in G5/G6. Optional later: `RequestAborted` support via `context.RequestAborted` when client adds cancel.

## UX summary

| Event | User sees |
| --- | --- |
| Pull started | `#cmd-last-result`: yellow pending pill `Git Pull to Desktop: pulling…` |
| Pull succeeded | `Git Pull to Desktop: home → C:\dev\home` (`label → path`, [[tests/Shared.Tests/ViewModelCmdLastResultTests.fs]]) |
| Pull failed (no mapping) | `Git Pull to Desktop: no local mapping for workspace 'home'` |
| Pull failed (auth) | `Git Pull to Desktop: login required for git` |
| Pull failed (git) | `Git Pull to Desktop: <filtered stderr from DesktopGit>` |
| Push started | `#cmd-last-result`: yellow pending pill `Git Push to Server: pushing…` |
| Push succeeded | `Git Push to Server: home → C:\dev\home` (same `label → path` detail as Pull; not today's `pushed: <detail>`) |
| Push failed | `Git Push to Server: <structured or filtered error>` (mapping/auth/git; same error shapes as Pull where applicable) |
| Second Pull while pending | Queued in planner; `#cmd-last-result` stays on the in-flight op until it completes, then the queued Pull runs |

Palette / keybinding: commands remain available while pending; duplicate runs enqueue (FIFO). Optional polish later: show queue depth or disable-on-pending.

## Minimal slices

### Slice 1 — Async Pull + pending indicator + FIFO via planner

1. Add `GitRemotePlanner`, `DesktopGitOp`, `RequestDesktopGitOp`, `DesktopGitOpDone`, `gitCommandPending` (display only).
2. Refactor `gitPullOp` through planner enqueue; move HTTP to `App.fs` runner with async fetch + token prefetch.
3. On `DesktopGitOpDone`, planner dequeues and returns next effect when queue non-empty.
4. Update `renderDiagnostics` to render pending over `lastCmdResult`; add `amb-last-result-pending` CSS.
5. Verify: trigger Pull on a mapped workspace — UI responsive during pull; pending then success/error in bar.
6. Verify: trigger Pull twice quickly — second runs after first completes; both update `lastCmdResult` in order.

Success criteria: no sync XHR in Pull path; pending visible for entire token+pull duration; completion updates `lastCmdResult` with command name; queued Pull runs after in-flight Pull finishes; no `gitCommandQueue` on `VM`.

### Slice 2 — Push + Status

1. Extend `DesktopGitOp` and runner for Push and Status.
2. Phases: `pushing…`, `checking…`.
3. Wire `parseOrPushOp` push branch through planner and runner (same serial slot + FIFO queue).
4. Format Push success like Pull: `Detail` message is `label → localPath` (client composes from workspace label + desktop-resolved path, same as `gitPullOp` today). Drop the current `pushed: <detail>` wording.
5. Verify: Push and Status show pending then completion strings; Push success displays `label → path`.

### Slice 3 — Connect + Clone (multi-step)

1. **Folder picker** — `POST /_desktop/pick-folder` may remain sync XHR while the native dialog is open (desktop thread blocks on dialog anyway). Set pending only after a path is chosen, before mapping + git-remote / clone chain.
2. Phases: `connecting…`, `cloning…`; possibly sub-phase text (`cloning…` covers clone + mapping + remote).
3. Async PUT `/_desktop/workspace-mappings` and sequential desktop git POSTs via effect chain or single orchestrating effect with internal steps (still one in-flight slot; other user commands queue behind the chain in planner).

### Slice 4 — Parse (file read)

1. Async `GET /_desktop/file?path=…` for Parse branch of Parse/Upload.
2. Phase: `parsing…`; pending uses `Parse / Upload` command name.
3. Keep graph `applyAndPost` on completion unchanged ([[src/Client/UpdateImport.fs]]).

## Tests

| Area | What to prove |
| --- | --- |
| [[tests/Shared.Tests/ViewModelCmdLastResultTests.fs]] | Pending display: `formatGitPending` or `renderCmdResult` yields `Git Pull to Desktop: pulling…`; pending takes precedence over stale `lastCmdResult` |
| New Shared planner tests (or extend existing sync planner tests) | Enqueue while in-flight appends tail; `onDone` with non-empty queue dequeues head and returns next effect |
| [[tests/Shared.Tests/ViewModelCmdLastResultTests.fs]] | Existing pull success/error display cases unchanged after completion; add Push success case `Git Push to Server: home → C:\dev\home` |
| [[tests/Shared.Tests/DesktopGitTests.fs]] | No change expected — desktop git subprocess logic untouched |
| Manual / desktop | Pull while editing — caret and selection still work; pending clears on success and on auth/mapping errors |

No Server.Tests unless a shared decode helper moves to Shared. Client Fable modules stay thin; prefer pure Shared helpers for pending display and planner logic.

## Assumptions

- Desktop LocalProxy remains colocated; async fetch to `/_desktop/*` keeps working with current CORS/proxy setup.
- Serial git execution with a FIFO planner is enough for desktop UX (single user, single focus workspace); parallel subprocesses are not required.
- Git remote ops reuse the **Effect → SysMsg** dispatch shell already used by graph sync and file status; they do not reuse `pendingChanges` (different payload type).
- `withDiagnostic` ([[src/Client/Controller.fs]]) still stamps command names on ops that complete synchronously; async git ops set `commandName` explicitly in `DesktopGitOpDone` handler instead of relying on `withDiagnostic` for the completion message.
- Recent G7 behavior is preserved: server branch via symref ([[src/Shared/dotnet/DesktopGit.fs]] `remoteHeadBranch`), Ambit auth injection via `http.extraHeader`, Pull/Push success detail `label → path` (Push aligns to Pull; pending phase text stays verb-only, e.g. `pulling…` / `pushing…`, without path).

## Open questions (resolve in slice 1 implementation)

- Clear previous `lastCmdResult` on pending start vs keep underneath — recommend **keep underneath** but display pending text until done (user still sees activity).
- Surface queue depth in pending UI (e.g. `pulling… (2 queued)`) — defer; slice 1 shows in-flight op only.
- Where planner state lives on `VM` — recommend a single `gitRemoteState: GitRemoteState` field (pending list + inFlight flag), not a bare queue list at VM root.

## Review checkpoints

Stop for review if:

- Pending moves to `#sync-status` or row indicators (scope creep).
- A separate `gitCommandQueue` field reappears on `VM` instead of Shared planner state.
- Desktop endpoints gain job polling or WebSocket progress.
- Slice 3 tries to async the native folder dialog itself.
