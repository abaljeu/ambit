# Change list construction sites

Date: 2026-09-16. Inventory only. No migration. Ticket context: [[../issues/44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId basis]] (Status done) left a Change-list consume/persist seam; this report maps every real `Change list` assembly for the next hop. Spec: [[../arch.md|Core creation architecture]] Story **Caller, persist, and Poll**. Field shapes: [[event-abstraction.md|Event abstraction]].

Working tree is mid-edit. Ev types (`EventId`, `Authority`, `ActorResult`, `ActorStart`, `EventBody`, `Ev`) sit in [[src/Shared/History.fs]]. The Ev module is copying into that same file after `module Change`; [[src/Shared/Event.fs]] still holds a second Ev module. Change record Guid field is mid-rename: committed name was `changeId`; History.fs currently has a corrupted name `submissionIdonId`. Call sites mix `changeId`, `submissionId`, and `submissionIdonId` on Change records. This inventory treats that Guid as the Change submission key. [[src/Shared/Serialization.fs]] `encodeChange` is similarly mid-rename. Do not treat this file as a compile-clean snapshot.

Count method: one site is one function or mutable cell that **builds or rebuilds** a `Change list` (literal, cons, append, filter/sort/map that yields Change, decode). Pass-through parameters that only receive an already-built list sit in section 1.2. Single Change records that tests later wrap as `[ change ]` sit in section 1.3.

## 1. Construction and assembly

### 1.1 Production sites (16)

| # | File | Symbol | How the list is made |
| --- | --- | --- | --- |
| 1 | [[src/Server/Core/CoreEventDispatch.fs]] | `persist` | Builds one Change from Ev ops + persist revision, then posts `[ change ]` to `PersistHandlers.postChange` or `postGraphOnlyChange`. Origin of the persist Change list. Not a downcast. |
| 2 | [[src/Server/Core/FileAgent.fs]] | `applyBatch` | Folds the inbound Change list. Dedup path builds a stored Change from EventLog Ev (`id` / submission Guid / `Ev.ops`). Fresh path conses the amended Change. Ends with `List.rev` of confirmations and fresh. |
| 3 | [[src/Server/Core/DbAgent.fs]] | `applyOneChange` / `applyBatch` | Same cons of stored-from-Ev or amended Change onto confirmations, then `List.rev`. |
| 4 | [[src/Server/Core/DbAgent.fs]] | `processPostChange` | After apply, `fresh` is `confirmations` filtered by submitted Guids. Assembly by filter, not a new record literal. |
| 5 | [[src/Shared/History.fs]] | `PersistStamp.appendToLast` | Rebuilds the list: reverse, `appendToChange` on last, reverse again. Returns the same list when stamp ops or the list are empty. |
| 6 | [[src/Server/Core/CoreMailboxBackend.fs]] | `overlayFresh` | Calls `appendToLast` on `fresh`, then `List.map` over `confirmations` to swap in stamped records. Returns `(stamped, confirmed)`. |
| 7 | [[src/Shared/BootCache.fs]] | `changesAfter` | `List.filter` + `List.sortBy` on `id`. |
| 8 | [[src/Shared/BootCache.fs]] | `acceptedForLog` | If confirmed is empty: `submitted \|> List.map (fun item -> item.change)` (PendingChange.change is `Ev.asChange`). Else returns `confirmed`. |
| 9 | [[src/Shared/BootCache.fs]] | `novelChanges` | Filters poll Changes whose `id` / Guid are not already in the log. |
| 10 | [[src/Shared/BootCache.fs]] | `decideBootPoll` | `poll.events \|> List.map Ev.asChange`, then `novelChanges`. Bridge map; see section 2. |
| 11 | [[src/Client/BootCacheStore.fs]] | `decodeCachePayload` | Decodes JSON string list, `List.choose` of `Serialization.decodeChange`. Empty `[]` on miss or length mismatch. |
| 12 | [[src/Client/Program.fs]] | `bootLog` | Mutable `Change list = []`. `finishPaint` assigns the decoded log. `applyBootNovel` does `bootLog @ novel`. |
| 13 | [[src/Client/App.fs]] | `SubmitChangeCallbacks.onPostOk` | `ack.events \|> List.map Ev.asChange` into `SystemMsg.SubmitResponse`. Bridge map. |
| 14 | [[src/Client/App.fs]] | `runPollServer` | `poll.events \|> List.map Ev.asChange` into `SystemMsg.PollDone`. Fail paths pass `[]`. Bridge map. |
| 15 | [[src/Shared/SyncLogic.fs]] | `applySyncResponse` | `response.events \|> List.map` into `{ id = 0; Guid; ops }` Change records, then `foldProjectedChanges`. Inline Ev→Change, same shape as `Ev.asChange` with `id` forced to 0. |
| 16 | [[src/Shared/SyncLogic.fs]] | `consumeCatchUpPoll` | Same inline Ev→Change `List.map` as site 15, then fold. |

FileAgent `preparePostChange` and DbAgent `preparePostChange` call site 6. They do not build a third independent list.

### 1.2 Typed seams that carry Change list (pass-through)

These annotate or transport a list. They do not assemble records.

1. [[src/Server/Core/CoreChanges.fs]] `CoreChanges.postChange: Change list -> ...`. Comment: CoreEventDispatch builds Changes from Ev ops and calls this door. `postGraphOnlyChange` is singular Change.
2. [[src/Server/Core/CoreMsg.fs]] `PersistHandlers.postChange` and `postGraphOnlyChange` are both `Change list -> Result<...>`.
3. [[src/Server/Core/FileAgent.fs]] `processPostChange`, persist handler lambdas. [[src/Server/Core/DbAgent.fs]] `processPostChange`, persist handler lambdas.
4. [[src/Server/Core/CoreMailbox.fs]] `postChange` — `first :: rest` splits an inbound list, `List.map eventFromChange` to Ev list, then merge-posts. Converts away from Change list.
5. [[src/Server/Core/CoreCredentials.fs]] `CoreAuth.post` — admit then enqueue the same list.
6. [[src/Server/Core/CoreRuntime.fs]] `rejectPost (_: Change list)`. [[src/Server/Core/CoreMailboxBackend.fs]] `failedPersist` `postChange = fun _ -> Error`.
7. [[src/Shared/ViewModel.fs]] `SystemMsg.SubmitResponse` field `confirmed: Change list`; `PollDone` of `Change list`.
8. [[src/Client/Update.fs]] `applySubmitResponse` receives confirmed, immediately `List.map (Ev.ofChange "")` back to Ev. Poll consume maps the same way before `applyServerTail` / `consumeCatchUpPoll`.
9. [[src/Shared/SyncLogic.fs]] `foldProjectedChanges` folds an inbound Change list through `ResidentProjection.applyChange`.
10. [[src/Shared/ResidentProjection.fs]] `captureLoadResponse` takes Change list, maps `Ev.ofChange ""` onto LoadResponse.events (Ev list).
11. [[src/Server/DatabaseProjection.fs]] `plan` / `distinctIds` / `nodeRows` / `childReplacements` collect ops from an inbound Change list.
12. [[src/Client/UpdateWorkspaceDownload.fs]] `accumulateAutoDownloadFromChanges` collects ops (`List.collect`).
13. [[src/Client/BootCacheStore.fs]] `appendChanges` encodes an inbound list to IndexedDB. `readCache` callback type is `... -> Change list -> unit`.
14. [[src/Shared/BootCache.fs]] `foldLog`, `clientRevision`, `decideBootRead`, `decideBootReadWait`, `BootPoll.ApplyNovel` carry or consume the log / novel list.
15. [[src/Client/Program.fs]] `applyBootNovel`, `finishPaint` parameters.

No type alias `type X = Change list`. Pending queue is `PendingChange list` (wraps Ev), not Change list. [[src/Shared/Serialization.fs]] `EventBatch` is `{ events: Ev list }`. There is no remaining `ChangeBatch` type.

### 1.3 Single Change constructors (not lists)

These build one Change. Lists form only when a caller writes `[ change ]` or conses during applyBatch.

1. Client commands: [[src/Client/UpdateHelpers.fs]] `applyAndPost`; [[src/Client/UpdatePaste.fs]] `newChange`; [[src/Client/UpdateOps.fs]], [[src/Client/UpdateEdit.fs]], [[src/Client/UpdateMove.fs]], [[src/Client/UpdateRename.fs]], [[src/Client/UpdateFileSearch.fs]], [[src/Client/UpdateAmbleRun.fs]], [[src/Client/UpdateWorkspaceSync.fs]], [[src/Client/UpdateWorkspaceDownload.fs]] — record literals, then enqueue PendingChange.
2. [[src/Shared/ImportText.fs]] `buildImportChange` / `buildDirectoryMergeChange`.
3. [[src/Server/GraphOnlyChangePost.fs]] `postChunks` — one Change per Op chunk; `CoreChanges.postGraphOnlyChange` is singular.
4. [[src/Server/Api.fs]] `applyParseFile` — one Change, `postGraphOnlyChange`.
5. [[src/Shared/History.fs]] `Change.inverse` / `Change.invert`; [[src/Shared/ChangeAmendment.fs]] `applyChange` returns one amended Change (FileAgent/DbAgent then cons it).
6. [[src/Shared/ClientHistory.fs]] private `asChange` — one Change from an undo/redo Ev.
7. [[src/Shared/SyncLogic.fs]] `undoPendingGraph` and `projectSuffixes` — one Change per step, not a list.
8. [[src/Shared/SyncPlanner.fs]] `extractChange` — one Change from Ev for `ChangeValidation.applyChange`. Rebuilds pending as `PendingChange list`.

### 1.4 Test assembly

Test code builds Change lists in two patterns: a helper that is typed `Change list`, and `[ change ]` (or `[ c1; c2 ]`) at `postChange` / `plan` / BootCache calls.

**Helpers typed as Change list**

1. [[tests/Server.Tests/DbAgentTests.fs]] `encodeChangeBatch` — identity on `Change list`. `emptyChange` returns a one-element list. Call sites pass `encodeChangeBatch [ change ]` into persist `postChange`.
2. [[tests/Server.Tests/StateEndpointTests.fs]] `encodeEventBatchBody` / `postChanges` — take `Change list`, map `eventFromChange`, encode EventBatch. `postChange` wraps `[ change ]`. One test posts `[ change; undo; redo ]`.
3. [[tests/Server.Tests/DatabaseProjectionContractTests.fs]] `encodeBatch` — identity. `persistPatch` / `core.postChange` get `[ change ... ]`.
4. [[tests/Shared.Tests/BootCachePollTests.fs]] `mkPoll` — takes `Change list`, maps `Ev.ofChange ""` onto `ChangeSuccessResponse.events`.
5. [[tests/Server.Tests/IgnoredDestinationValidationTests.fs]] `encodeChange` — returns `[ { id = 0; Guid; ops } ]`.

**Multi-element literals (not only `[ change ]`)**

1. [[tests/Server.Tests/FileAgentFailureTests.fs]] persist `postChange [ second; first ]` (dedup batch).
2. [[tests/Server.Tests/DatabaseProjectionTests.fs]] `let changes = [ change op1; change op2 ]` into `DatabaseProjection.plan`.
3. [[tests/Shared.Tests/SyncLogicTests.fs]] `let changes = [ mkChange 5; mkChange 6; mkChange 7 ]`.
4. [[tests/Server.Tests/StateEndpointTests.fs]] `[ change; undo; redo ]`, `[ change1; change2 ]`, `[ change1; bad ]`.
5. [[tests/Shared.Tests/SyncPlannerTests.fs]] `[ c1; c2; c3 ]` then `List.map (Ev.ofChange "")`.
6. [[tests/Shared.Tests/BootCacheTests.fs]] `[ mkChange n ]` / `[ change ]` into `changesAfter` / `foldLog` / `acceptedForLog`.
7. [[tests/Shared.Tests/LoadCaptureTests.fs]] `[ change ]` into `captureLoadResponse`.
8. [[tests/Server.Tests/CoreCredentialsTests.fs]] `ResizeArray<Change list>()`; asserts `Assert.Equal<Change list>([ change ], ...)`.

**`[ change ]` at CoreChanges / CoreMailbox.postChange** (one Change, list wrapper): [[tests/Server.Tests/CoreChangesTests.fs]], [[tests/Server.Tests/CoreRuntimeTests.fs]], [[tests/Server.Tests/CoreActorPoolTests.fs]], [[tests/Server.Tests/ActorCoreChangesDoorTests.fs]], [[tests/Server.Tests/CredentialedChangePostsTests.fs]], [[tests/Server.Tests/GraphOnlyChangePostTests.fs]], [[tests/Server.Tests/LazyLoadReconciliationServerTests.fs]], [[tests/Server.Tests/Issue41CoreMailboxTests.fs]], [[tests/Server.Tests/PersistHandlersRestoreTests.fs]], [[tests/Server.Tests/Issue42PersistHandlersTests.fs]], [[tests/Server.Tests/CoreMailboxDoorTests.fs]] (includes an inline one-element record list), [[tests/Server.Tests/CoreMsgActorCasesTests.fs]], [[tests/Server.Tests/TestActor.fs]], [[tests/Server.Tests/FileAgentFailureTests.fs]], [[tests/Server.Tests/DbAgentTests.fs]].

## 2. Bridge conversions that feed lists

These are record copies (`id` / Guid / ops). They are not downcasts.

| File | Symbol | Direction | Feeds a Change list? |
| --- | --- | --- | --- |
| [[src/Shared/Event.fs]] and duplicate in [[src/Shared/History.fs]] | `Ev.asChange` | Ev → Change | Yes when mapped: App POST ack, App Poll, BootCache.decideBootPoll. PendingChange.change member. FileAgentFailureTests / StateEndpointTests ack asserts. |
| same | `Ev.ofChange` | Change → Ev | Inverse: Update.fs submit/poll consume, Program.fs `applyBootNovel`, FileAgent/DbAgent `accepted`, ResidentProjection.captureLoadResponse, SyncBatch tests, BootCachePollTests.mkPoll. |
| [[src/Shared/ViewModelSync.fs]] | `PendingChange.change` | Ev → Change | `Ev.asChange this.event`. BootCache.acceptedForLog maps `item.change`. |
| [[src/Shared/ViewModelSync.fs]] | `PendingChange.ofChange` | Change → PendingChange | `Ev.ofChange ""` then wrap. Tests (SyncPlanner, AckReconcile, SyncLogic, BootCache, WorkspaceUpload). |
| [[src/Shared/ClientHistory.fs]] | private `asChange` | Ev → Change | One Change for undo/redo apply. Not a list. |
| [[src/Shared/SyncLogic.fs]] | `applySyncResponse` / `consumeCatchUpPoll` | Ev list → Change list | Inline copy, `id = 0`. Same as `Ev.asChange` except id. |
| [[src/Server/Core/FileAgent.fs]] / [[src/Server/Core/DbAgent.fs]] | stored Change in apply | Ev → Change | Field copy from EventLog Ev during dedup. Cons onto confirmations. |
| [[src/Server/Core/CoreMailbox.fs]] | `eventFromChange` | Change → Ev | Batch door maps the whole inbound Change list to Ev before `postEvent`. |
| [[src/Server/Core/CoreEventDispatch.fs]] | `persist` | Ev → `[ change ]` | Builds Change from `Ev.ops` + revision. |
| [[src/Shared/ApiResponses.fs]] | `ChangeSuccessResponse.changes` / `LoadResponse.changes` / `SyncResponse.changes` | member = `this.events` | **Ev list**, not Change list. Do not migrate these as Change lists. StateEndpointTests `post.changes \|> List.map Ev.asChange` is Ev→Change for asserts. |

No `Ev list -> Change list` function besides `List.map Ev.asChange` and the two SyncLogic inline maps.

## 3. Downcast verdict

**No.** No true downcast from Ev (or former Event) to Change.

Searched `src/` and `tests/` for `:? Change`, `:?> Change`, `unbox<Change`, `box` of Change/Ev, and `:?>` involving those types. Hits are Npgsql / DOM / test `box result` / `unbox<'a>` on SQL rows — not Change/Ev.

Ev→Change is always a new record (`Ev.asChange`, ClientHistory.asChange, FileAgent/DbAgent stored copy, SyncLogic inline map, CoreEventDispatch.persist). Change→Ev is `Ev.ofChange` or `eventFromChange`.

`ChangeSuccessResponse.changes` is a member alias onto `events: Ev list`. That is a name alias, not a type coerce.

## 4. Not Change lists (exclusion)

1. `PendingChange list` — pending stack, SyncPlanner, SyncBatch, ViewModelSync `WaitingToRetry` / `SubmitPendingBatch` / `SavePendingQueue`.
2. `Ev list` — EventBatch, EventLog, CoreChangesAccepted.events, ChangeSuccessResponse.events, LoadResponse.events, `postEvents`, `getEventsSince`, `previewEvents`.
3. `EventBody.Change of Op list` — ops payload, not a Change list.
4. [[src/Shared/dotnet/LazyLoadReconciliation.fs]] `ChangedPath list`.
5. Wire POST body is EventBatch (Ev list) via [[src/Client/UpdateCodec.fs]] `encodePendingBatchBody`. Decode of poll/POST success yields Ev list, then sites 10, 13–16 map to Change list.

## 5. Suggested next-seam order

Replace Change list with Ev list from the edges that already hold Ev, then the persist algorithm that still needs Change.

1. Client consume bridges (sites 13–16 and 10) — App Poll/POST, SyncLogic fold/catch-up, BootCache.decideBootPoll. Input is already Ev list. [[src/Shared/ViewModel.fs]] `SubmitResponse` / `PollDone` change together.
2. Boot IndexedDB log (sites 7–9, 11–12) — BootCache + BootCacheStore + Program.fs `bootLog`. Codec is `Serialization.encodeChange` / `decodeChange`.
3. Server apply/stamp (sites 2–6) — FileAgent/DbAgent applyBatch, PersistStamp.appendToLast, overlayFresh, DatabaseProjection.plan. This is the last Change-list algorithm (dedup, amend, stamp last).
4. Persist door (site 1 plus section 1.2 items 1–6) — CoreEventDispatch `[ change ]`, PersistHandlers, CoreChanges.postChange, CoreMailbox.postChange, CoreAuth.post. After step 3, post Ev (or ops) instead of wrapping `[ change ]`.
5. Tests — helpers in section 1.4 and `[ change ]` wrappers follow the door they hit.

Keep `Change` as the command-builder product (single record) until a later contract hop. Do not wait on test `[ change ]` wrappers to start steps 1–2.

## 6. Site count

1. Production construction/assembly: **16** (section 1.1).
2. Test files that assemble Change lists: **26** (section 1.4).
3. Downcasts: **none**.
