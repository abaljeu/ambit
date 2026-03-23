---
name: Robust Client-Server Sync
overview: Add automatic exponential-backoff retry, localStorage queue persistence, multi-client conflict resolution via UUID-tagged changes and 409 responses, and a rebase placeholder for salvaging stashed changes after a conflict.
todos:
  - id: interop
    content: Add setTimeout and localStorage JS interop emits to Update.fs
    status: completed
  - id: save-load
    content: Add savePendingQueue / loadPendingQueue helpers to Update.fs
    status: completed
  - id: call-save
    content: Call savePendingQueue wherever pendingChanges is mutated (applyAndPost, undoOp, redoOp, SubmitResponse handler)
    status: completed
  - id: auto-retry
    content: Add mutable retryCount to App.fs; schedule setTimeout retry in SubmitFailed dispatch arm; reset on SubmitResponse
    status: completed
  - id: restore-startup
    content: Restore pending queue from localStorage in StateLoaded handler in App.fs, filtering by serverRev and re-applying ops to reconstruct local graph
    status: completed
  - id: poll-stale-bar
    content: Client polls GET /state (interval + focus); if server revision > client, show blue Stale bar "Refresh the view"; click reloads
    status: completed
  - id: uuid-change
    content: Add changeId (Guid) field to the Change record in Model/History; update Serialization, ChangeLog, all Change construction sites
    status: in_progress
  - id: server-idempotency
    content: Add seen-UUID ring buffer to FileAgent; return OK for known UUID (duplicate retry), 409 for unknown UUID with id < revision (conflict)
    status: pending
  - id: poll-endpoint
    content: Add GET /ambit/changes-since/{revision} endpoint on server, reading from the change log
    status: pending
  - id: poll-client
    content: Add client polling (on focus + interval) that calls changes-since and applies remote changes to local graph
    status: pending
  - id: rebase-placeholder
    content: Add attemptRebase function in Update.fs (stub that tries ops as-is, returns Discarded on failure) and wire 409 handler to stash/rebase/re-queue loop
    status: pending
  - id: deferred-changes
    content: Add deferredChanges field to VM; implement dirty-edit poll logic (apply/update/defer based on whether remote change touches edited node and whether edit is dirty); flush deferredChanges on Editing exit
    status: pending
isProject: false
---

# Robust Client-Server Sync

Two orthogonal improvements:

- **Auto-retry**: when a POST fails, schedule retries with exponential backoff instead of waiting for a user click.
- **Queue persistence**: save the pending queue to `localStorage` so changes survive a tab close or browser refresh.

---

## Key files

- `[src/Shared/ViewModel.fs](src/Shared/ViewModel.fs)` — `SyncState` type, `VM` record
- `[src/Client/Update.fs](src/Client/Update.fs)` — `applyAndPost`, `fireNextPending`, `update` handler for `SubmitFailed`/`SubmitResponse`
- `[src/Client/App.fs](src/Client/App.fs)` — `dispatch` function, `StateLoaded` handler
- `[[src/Client/View.fs]]` — `renderStatus`

---

## Part 1 — Auto-retry with exponential backoff

**Where the work lives: `App.fs` (infrastructure side-effect, not model logic)**

Add a mutable `retryCount` in `App.fs`. In the `dispatch` match arm that already handles `System SubmitFailed`:

```fsharp
let mutable retryCount = 0

// in dispatch:
| System SubmitFailed ->
    retryCount <- retryCount + 1
    let delaySec = min 60 (1 <<< retryCount)  // 2 4 8 16 32 60 60 ...
    setTimeout (fun () -> applyOp retryPendingOp) (delaySec * 1000) |> ignore
    View.renderStatus currentModel

| System (SubmitResponse _) ->
    retryCount <- 0
    consecutiveConflicts <- 0
    View.renderStatus currentModel

| System ConflictResponse ->
    // Rebase cycle: reset network-backoff counter (this is a fresh attempt, not a retry)
    retryCount <- 0
    consecutiveConflicts <- consecutiveConflicts + 1
    // ... stash / fetch / rebase / re-queue (see Part 3c)
    // If another 409 arrives after the rebase, this same arm runs again — no special case needed.
    // Warn the user after several consecutive conflicts (another device is actively editing).
    if consecutiveConflicts >= 5 then
        View.renderConflictWarning currentModel
    View.renderStatus currentModel
```

`consecutiveConflicts` is a second mutable alongside `retryCount`. It resets to 0 on any successful `SubmitResponse`.

Add the `setTimeout` JS interop in `Update.fs` (near the existing `postJson` emit):

```fsharp
[<Emit("window.setTimeout($0, $1)")>]
let setTimeout (f: unit -> unit) (ms: int) : unit = jsNative
```

`retryPendingOp` already exists and does the right thing — it re-fires the head of the queue and sets `syncState = Syncing`.

The UI text in `View.renderStatus` can stay unchanged or gain a "(auto-retrying)" note — the existing "click to retry" affordance still works as an impatient override.

---

## Part 2 — localStorage persistence

**Where the work lives: `Update.fs` (save/load helpers) + `App.fs` (restore on startup)**

### 2a. Save queue on every change

Add localStorage interop and a serialise/deserialise pair in `Update.fs`:

```fsharp
[<Emit("localStorage.setItem($0,$1)")>] let lsSet (k:string) (v:string) : unit = jsNative
[<Emit("localStorage.getItem($0)")>]    let lsGet (k:string) : string = jsNative
[<Emit("localStorage.removeItem($0)")>] let lsDel (k:string) : unit = jsNative

let private pendingKey = "gambol-pending-v1"

let savePendingQueue (changes: Change list) =
    if changes.IsEmpty then lsDel pendingKey
    else
        let arr = changes |> List.map Serialization.encodeChange
        lsSet pendingKey (Thoth.Json.JavaScript.Encode.toString 0 (Encode.list arr))

let loadPendingQueue () : Change list =
    let json = lsGet pendingKey
    if isNull json || json = "" then []
    else
        match Thoth.Json.JavaScript.Decode.fromString (Decode.list Serialization.decodeChange) json with
        | Ok cs -> cs | Error _ -> []
```

Call `savePendingQueue` in every place `pendingChanges` changes:

- `applyAndPost` — after building `pending`
- `undoOp` / `redoOp` — after building `pending`
- `update` → `SubmitResponse` — after removing the head (or clearing when empty)

### 2b. Restore queue on startup

In `App.fs`, after `StateLoaded` creates the fresh VM from the server state:

```fsharp
| System (StateLoaded _) ->
    currentModel <- restoreSessionState currentModel
    // -- restore pending queue --
    let saved = loadPendingQueue ()
    let serverRev = currentModel.revision.Value
    // keep only changes the server hasn't confirmed yet
    let pending = saved |> List.filter (fun c -> c.id >= serverRev)
    if not pending.IsEmpty then
        // re-apply pending ops on top of the server graph so the UI
        // shows the user's uncommitted edits immediately
        let localGraph =
            pending |> List.fold (fun g c ->
                match Change.apply c { graph = g; history = History.empty; revision = Revision 0 } with
                | ApplyResult.Changed s -> s.graph
                | _ -> g) currentModel.graph
        currentModel <- { currentModel with
                            graph      = localGraph
                            pendingChanges = pending
                            syncState  = Syncing }
        fireNextPending pending dispatch
    // -- end restore --
    elementCache <- render currentModel applyOp
    View.renderUndoStatus currentModel
```

The filter `c.id >= serverRev` works because `change.id` is always set to `model.revision.Value` at creation time, which equals the server revision the change was based on. Changes with `id < serverRev` were already applied by the server.

---

## Part 3 (initial) — Stale detection via polling

**Status: DONE**

Simplest version: client polls the existing `GET /{file}/state` endpoint. If server revision > client revision, the view is stale — show a blue bar prompting the user to refresh.

- **SyncState**: add `Stale` variant
- **SystemMsg**: add `ServerAhead of Revision`
- **Polling**: `setInterval` 15s + `window.focus` event; on each poll, fetch state and dispatch `ServerAhead` when `serverRev > model.revision`
- **UI**: blue `.amb-stale` bar with "Refresh the view"; click reloads the page
- **Update**: handle `ServerAhead` → set `syncState = Stale`

No new server endpoint; reuses `GET /state` and compares revision numbers.

---

## Part 3 — UUID-tagged changes and multi-client conflict resolution

### 3a. Add `changeId: Guid` to `Change`

`**src/Shared/History.fs**` — add the field:

```fsharp
type Change =
    { id: int          // base revision (server revision this change was built on)
      changeId: Guid   // unique identity for deduplication across clients
      ops: Op list }
```

Update `Serialization.fs`, `ChangeLog.fs`, and every `Change` construction site (all `applyAndPost` / `undoOp` / `redoOp` call sites in `Update.fs`) to populate `changeId = Guid.NewGuid()`. On rebase, the UUID is **reused** (the server has never seen it, since the change was never successfully applied).

### 3b. Server seen-UUID set + 409 response

`**src/Server/FileAgent.fs`** — replace the simple `< revision` guard with UUID-aware logic:

```fsharp
// Ring buffer of the last ~50 applied change UUIDs
let seenIds = System.Collections.Generic.Queue<Guid>()
let maxSeen = 50

let handlePostChange body reply inbox =
    match decode body with
    | Error err -> reply.Reply(Error err)
    | Ok change ->
        if change.id < state.Value.revision.Value then
            if seenIds.Contains(change.changeId) then
                reply.Reply(Ok (encodeStateJson ()))       // duplicate retry → OK
            else
                reply.Reply(Error "conflict")              // different client → 409
        else
            match History.applyChange change state.Value with
            | ApplyResult.Changed newState ->
                if seenIds.Count >= maxSeen then seenIds.Dequeue() |> ignore
                seenIds.Enqueue(change.changeId)
                // ... store, update revision, reply OK
```

The server returns a distinct HTTP status — **409 Conflict** rather than 400 — so the client can distinguish a conflict from a malformed request.

### 3c. Client stash / rebase / re-queue on 409

Add `ConflictResponse` to `SystemMsg` and extend `postJson` to map HTTP 409 to `ConflictResponse`.

On `ConflictResponse` (this arm may fire repeatedly if the remote client is still active — it is self-contained and re-entrant by design):

1. Stash `model.pendingChanges`
2. Fetch latest state (`GET /ambit/state`)
3. Apply server graph to model (this becomes the new rebase base)
4. Run **sequential rebase**: each stashed change is attempted on the graph produced by the previous successful rebase, not all on the same base — so a discarded change does not corrupt the base for subsequent ones:

```fsharp
let rebaseAll (serverGraph: Graph) (serverRev: int) (stashed: Change list) =
    stashed |> List.fold (fun (graph, rev, acc) change ->
        match attemptRebase graph rev change with
        | Rebased c ->
            let graph' = Change.apply c ... // advance the accumulated graph
            (graph', rev + 1, acc @ [c])
        | Discarded ->
            (graph, rev, acc)   // skip; do NOT advance rev
    ) (serverGraph, serverRev, [])
```

1. Re-queue the successfully rebased changes (UUIDs reused, `id` updated to new base), fire first POST
2. If *that* POST also gets 409 (another device moved the server again), the same arm runs again from step 1 — no special-case needed. `consecutiveConflicts` increments each time; a warning is shown after 5.

### 3d. Polling for remote changes + `window.online`

**New server endpoint** `GET /ambit/changes-since/{revision}` reads entries from the change log starting at the given revision index and returns them as a JSON array.

**Client** polls this on:

- `window.focus` event (immediate catch-up when switching back to the tab)
- `window.online` event (immediate retry when network connectivity is restored — avoids waiting up to 60s for the next backoff tick)
- A ~15-second interval while the tab is visible

**Applying incoming remote changes while in `Editing` mode:**

The app is almost always in `Editing` mode (a node is always selected and shown in the edit input), so naively deferring all polls during editing would disable the feature entirely. The discriminator is whether the edit is **dirty**:

```
dirty = readEditInputValue() != originalText   // user has actually typed something
```

- **Remote change does not touch the edited node**: apply all remote changes immediately; edit input and `originalText` are untouched.
- **Remote change touches the edited node, edit is clean**: apply; silently update `originalText` and the edit input to the incoming server text — no data lost, user sees the remote text appear in the input.
- **Remote change touches the edited node, edit is dirty**: defer the conflicting changes into `model.deferredChanges`; apply the remainder immediately.

On any transition out of `Editing` (commit or cancel), flush `model.deferredChanges` through the normal sequential rebase path immediately before the next render.

`deferredChanges: Change list` is added as a new field on `VM` (empty by default). It is also added to `savePendingQueue` / `loadPendingQueue` so deferred changes survive a page reload.

---

## Part 4 — localStorage persistence

*(unchanged from original plan — see above)*

---

## Part 5 — Rebase placeholder

`**src/Client/Update.fs`**

```fsharp
type RebaseResult =
    | Rebased of Change   // ops applied cleanly; change ready to re-queue (id updated)
    | Discarded           // ops could not be applied; data could not be salvaged

/// Attempt to apply a stashed change on top of a new server graph.
/// Currently: tries ops as-is; returns Discarded on any Invalid result.
///
/// TODO: smart merge — e.g. if a NewNode's intended parent was deleted,
/// walk up to the nearest surviving ancestor and re-attach there rather
/// than discarding the node entirely.
let attemptRebase (newGraph: Graph) (newBaseRevision: int) (stashed: Change) : RebaseResult =
    let fakeState = { graph = newGraph; history = History.empty; revision = Revision newBaseRevision }
    match Change.apply stashed fakeState with
    | ApplyResult.Changed _ -> Rebased { stashed with id = newBaseRevision }
    | _                     -> Discarded
```

The `TODO` comment is the explicit placeholder. When the smart merge logic is built out, it replaces the `Discarded` branch with orphan-rescue logic (e.g. re-attaching children of a deleted parent to the nearest surviving ancestor).