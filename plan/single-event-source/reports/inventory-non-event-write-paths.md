# Inventory non-Event write paths

Question: which remaining code paths mutate the Graph, persist EventLog, or apply Ops without an Event (`Ev`)? Source: `src/` and `tests/` F# only. CONTEXT: Change is `EventBody.Change` of an Op list, not a record. The leftover `{ id; submissionId; ops }` record is still in the code. Revision is a retired name for event id. EventLog persist of an Event is `appendEvent` on `Ev`. Graph apply of that same work still goes through the leftover Change record.

## 1. The Change record and module Change

The leftover record is `type Change = { id: int; submissionId: System.Guid; ops: Op list }` in [[src/Shared/History.fs]]. `Op.makeChange` in the same file builds that record. No other `src/` call site uses `makeChange`.

`module Change` in [[src/Shared/History.fs]] has `addOp`, `inverse`, `invert`, `apply`, and `undo`. `Change.inverse` writes `id = baseRevision.Value`. `Change.apply` folds `Op.apply`. `Change.undo` folds `Op.undo`. `ChangeValidation.applyChangeTrusted` calls `Change.apply`. `ChangeValidation.applyChange` then checks ownership. `PersistStamp.appendToChange` and `PersistStamp.appendToLast` append stamp Ops onto a Change list.

JSON of the leftover record is `Serialization.encodeChange` / `decodeChange` in [[src/Shared/Serialization.fs]]. Keys are `"id"`, `"submissionId"`, `"ops"`. `"id"` is the integer serial.

### 1.1 Shared apply and amend of Change lists

- [[src/Shared/ChangeAmendment.fs]] `ChangeAmendment.applyChange` takes a leftover Change, calls `ChangeValidation.applyChange`, and can return an amended Change.
- [[src/Shared/ResidentProjection.fs]] `applyChange` applies `change.ops` via `applyOps`.
- [[src/Shared/SyncPlanner.fs]] `restorePending.extractChange` builds `{ id = 0; ... }` from `Ev.ops` and calls `ChangeValidation.applyChange`.
- [[src/Shared/BootCache.fs]] stores a Change list (`changeStore = "changes"`). Helpers: `changesAfter`, `clientRevision`, `mergeConfirmed`.
- [[src/Shared/ImportText.fs]] `buildImportChange` and `buildDirectoryMergeChange` return leftover Change. `id` is the `revision: int` argument.
- [[src/Shared/ClientHistory.fs]] `record` takes leftover Change and copies Ops onto `EventBody.Change`. `undo` and `redo` return leftover Change via private `asChange`.
- [[src/Server/DatabaseProjection.fs]] `plan` takes `changes: Change list` and uses `change.ops` to choose node and child rows.

### 1.2 Client constructors of leftover Change

Each constructor sets `id = model.revision.Value` (or an int `revision` argument) and then calls `applyAndPost` or `applyAndPostSync`.

- [[src/Client/UpdateHelpers.fs]] `applyAndPost`, `commitTextEdit`, `splitNode`.
- [[src/Client/UpdatePaste.fs]] `newChange`, Paste and Cut.
- [[src/Client/UpdateEdit.fs]] edit commit.
- [[src/Client/UpdateMove.fs]] move.
- [[src/Client/UpdateOps.fs]] `DupNodes`, `deleteChildSpan`, `EditClasses`.
- [[src/Client/UpdateRename.fs]] Rename.
- [[src/Client/UpdateAmbleRun.fs]] Exec.
- [[src/Client/UpdateFileSearch.fs]] `applyOpsChange`.
- [[src/Client/UpdateWorkspaceDownload.fs]] download stamp Change.
- [[src/Client/UpdateWorkspaceSync.fs]] `applyAndPostSync`, workspace structure Change, `createWorkspaceOnServer`.
- [[src/Client/Program.fs]] `bootLog: Change list`.
- [[src/Client/BootCacheStore.fs]] `appendChanges`, IndexedDB encode via `Serialization.encodeChange`.

### 1.3 Server constructors and Change lists

- [[src/Server/GraphOnlyChangePost.fs]] `postChunks` builds `{ id = revision.Value; submissionId = Guid.NewGuid(); ops = chunk }`.
- [[src/Server/Api.fs]] `applyParseFile` builds `{ id = state.revision.Value; ... }` and posts it as graph-only.
- [[src/Server/Core/FileAgent.fs]] `applyBatch` steps a Change list. Dedup rebuilds leftover Change from stored `Ev`. `processPostChange` takes `Change list`.
- [[src/Server/Core/DbAgent.fs]] `applyOneChange` / `applyBatch` / `processPostChange` same shape.
- [[src/Server/Core/CoreMailboxBackend.fs]] `overlayFresh` stamps a Change list via `PersistStamp.appendToLast`.
- [[src/Server/DatabaseSetup.fs]] `decodeChangePayload` still decodes leftover Change JSON. [[src/Server/Database.fs]] `loadPersistedState` takes `_decodeChange` but does not read Change rows. Schema `DROP TABLE IF EXISTS changes` is in the same file.

### 1.4 Tests that build leftover Change

Shared: [[tests/Shared.Tests/HistoryTests.fs]], [[tests/Shared.Tests/EventTests.fs]], [[tests/Shared.Tests/ChangeAmendmentTests.fs]], [[tests/Shared.Tests/ClientHistoryTests.fs]], [[tests/Shared.Tests/ClientHistoryRuntimeTests.fs]], [[tests/Shared.Tests/AckReconcileTests.fs]] `textChange`, [[tests/Shared.Tests/SyncLogicTests.fs]] `mkChange`, [[tests/Shared.Tests/SyncPlannerTests.fs]], [[tests/Shared.Tests/BootCacheTests.fs]], [[tests/Shared.Tests/BootCachePollTests.fs]], [[tests/Shared.Tests/SerializationTests.fs]], [[tests/Shared.Tests/DeleteOpsTests.fs]], [[tests/Shared.Tests/ImportDocumentTests.fs]], [[tests/Shared.Tests/LargeChangeApplyTests.fs]], [[tests/Shared.Tests/LoadCaptureTests.fs]], [[tests/Shared.Tests/GraphOnlyChangeChunksTests.fs]], [[tests/Shared.Tests/LazyLoadReconciliationTests.fs]], [[tests/Shared.Tests/WorkspaceUploadTests.fs]], [[tests/Shared.Tests/WorkspaceUploadStructureTests.fs]], [[tests/Shared.Tests/ViewModelTests.fs]], [[tests/Shared.Tests/ViewModelMoveOpsTests.fs]].

Server: [[tests/Server.Tests/TestBackend.fs]], [[tests/Server.Tests/TestActor.fs]], [[tests/Server.Tests/DbAgentTests.fs]], [[tests/Server.Tests/FileAgentFailureTests.fs]], [[tests/Server.Tests/GraphOnlyChangePostTests.fs]], [[tests/Server.Tests/LazyLoadReconciliationServerTests.fs]], [[tests/Server.Tests/CoreChangesTests.fs]], [[tests/Server.Tests/CoreMailboxDoorTests.fs]], [[tests/Server.Tests/Issue41CoreMailboxTests.fs]], [[tests/Server.Tests/Issue42PersistHandlersTests.fs]], [[tests/Server.Tests/PersistHandlersRestoreTests.fs]], [[tests/Server.Tests/CredentialedChangePostsTests.fs]], [[tests/Server.Tests/ActorCoreChangesDoorTests.fs]], [[tests/Server.Tests/CoreRuntimeTests.fs]], [[tests/Server.Tests/CoreActorPoolTests.fs]], [[tests/Server.Tests/CoreMsgActorCasesTests.fs]], [[tests/Server.Tests/StateEndpointTests.fs]], [[tests/Server.Tests/ChangeEndpointResilienceTests.fs]], [[tests/Server.Tests/DatabaseProjectionContractTests.fs]], [[tests/Server.Tests/IgnoredDestinationValidationTests.fs]], [[tests/Server.Tests/TestActorHelloTests.fs]], [[tests/Server.Tests/SavePrepTests.fs]].

## 2. Ev↔Change copy

There is no `PendingChange.ofChange`. [[src/Shared/ViewModelSync.fs]] has `PendingChange.ofEvent` and `PendingChange.workspaceSingleton`. Member `PendingChange.change` is `Ev.asChange this.event`.

### 2.1 Shared copy functions

- [[src/Shared/History.fs]] `Ev.asChange` copies `event.id.Value`, `submissionId`, and `Ev.ops` (empty list if the body is not an Action) into leftover Change.
- [[src/Shared/History.fs]] `Ev.ofChange` copies leftover Change into `Ev` with `id = EventId change.id`, `authority = Authority "Browser"`, and `body = EventBody.Change change.ops`.
- [[src/Shared/History.fs]] `Ev.inverseOps` builds a dummy leftover Change `{ id = 0; ... }`, calls `Change.inverse Revision.Zero`, and returns `inverse.ops`.
- [[src/Shared/History.fs]] `Ev.apply` wraps `Ev.ops` in leftover Change `{ id = 0; ... }` and calls `Change.apply`.
- [[src/Shared/ClientHistory.fs]] private `asChange` copies `Ev` Ops onto leftover Change with `id = rev` from `Revision`.
- [[src/Shared/ClientHistory.fs]] `record` copies leftover Change Ops onto `EventBody.Change`.
- [[src/Shared/SyncLogic.fs]] `applyLocalChange` applies leftover Change, then builds `Ev` with `EventBody.Change change.ops`. `undoPendingGraph` builds leftover Change from `item.event` then `Change.inverse`. `applyInverse` uses leftover Change from `ClientHistory.undo` / `redo`. `projectSuffixes` builds leftover Change from Op suffixes. `PendingChange.change` is used in confirmation checks.
- [[src/Shared/SyncPlanner.fs]] `extractChange` copies `Ev` to leftover Change `{ id = 0; ... }`.
- [[src/Shared/SyncBatch.fs]] `toWireBatch` maps `PendingChange` to `Ev list` (no leftover Change on the wire).

### 2.2 Server copy functions

- [[src/Server/Core/CoreMailbox.fs]] private `eventFromChange` builds `Ev` with `EventId 0`, empty Authority and commandName, `EventBody.Change change.ops`. `postChange` maps the Change list through `eventFromChange` then posts each as `PostEvent`.
- [[src/Server/Core/CoreMailboxBackend.fs]] `dispatchPostGraphOnlyChange` copies leftover Change to `Ev` with `EventId.zero` then calls `CoreEventDispatch.postEvent`.
- [[src/Server/Core/CoreEventDispatch.fs]] `persist` copies completed `Ev` Ops back into leftover Change `{ id = revision.Value; ... }` then calls `persist.postChange` or `persist.postGraphOnlyChange`.
- [[src/Server/Core/FileAgent.fs]] `accepted` maps confirmed leftover Change list with `Ev.ofChange ""`. Dedup in `applyBatch` rebuilds leftover Change from stored `Ev`.
- [[src/Server/Core/DbAgent.fs]] `accepted` maps confirmed leftover Change list with `Ev.ofChange ""`. `applyOneChange` rebuilds leftover Change from stored `Ev`.
- [[tests/Server.Tests/TestBackend.fs]] `eventFromChange` matches the CoreMailbox copy.

### 2.3 Client copy functions

- [[src/Client/Program.fs]] `novelChanges = novel |> List.map Ev.asChange` for boot cache.
- [[src/Client/App.fs]] Poll confirmed Events become leftover Change via `List.map Ev.asChange`.

Pending queue JSON is Event, not leftover Change: [[src/Shared/EventJson.fs]] `encodePendingChange` / `decodePendingChange` encode `item.event`. [[src/Client/UpdateHelpers.fs]] `savePendingQueue` / `loadPendingQueue` use that codec.

### 2.4 Tests that call Ev.ofChange / Ev.asChange / eventFromChange

`Ev.ofChange`: [[tests/Shared.Tests/AckReconcileTests.fs]], [[tests/Shared.Tests/SerializationTests.fs]], [[tests/Shared.Tests/LoadCaptureTests.fs]], [[tests/Shared.Tests/BootCachePollTests.fs]], [[tests/Shared.Tests/BootCacheTests.fs]], [[tests/Shared.Tests/SyncLogicTests.fs]], [[tests/Shared.Tests/SyncPlannerTests.fs]], [[tests/Shared.Tests/WorkspaceUploadTests.fs]], [[tests/Shared.Tests/ClientHistoryRuntimeTests.fs]].

`Ev.asChange`: [[tests/Server.Tests/FileAgentFailureTests.fs]], [[tests/Server.Tests/StateEndpointTests.fs]], plus client [[src/Client/App.fs]] and [[src/Client/Program.fs]].

`eventFromChange`: [[tests/Server.Tests/TestBackend.fs]], [[tests/Server.Tests/CoreChangesTests.fs]], [[tests/Server.Tests/CoreCredentialsTests.fs]], [[tests/Server.Tests/CoreRuntimeTests.fs]], [[tests/Server.Tests/StateEndpointTests.fs]], [[tests/Server.Tests/ChangeEndpointResilienceTests.fs]], [[tests/Server.Tests/LazyLoadReconciliationServerTests.fs]].

## 3. type Revision, EventId.ofRevision / toRevision, revision fields and JSON keys, Change.id as serial

### 3.1 Type and converters

`type Revision = | Revision of int` with `Revision.Zero` is in [[src/Shared/Model.fs]]. It is not EventId.

[[src/Shared/History.fs]] `module EventId` has `ofRevision` (`EventId rev.Value`) and `toRevision` (`Revision id.Value`). [[src/Shared/EventLog.fs]] comment: "EventId retires Revision".

### 3.2 Fields named revision

- [[src/Shared/History.fs]] `ActorStart.revision: EventId`. `State.revision: Revision`.
- [[src/Shared/ViewModel.fs]] `VM.revision: Revision`. `SystemMsg.SubmitResponse` carries `revision: Revision`.
- [[src/Shared/ViewModelSync.fs]] `CatchUpBaseline.revision: EventId`. `SyncState.WaitingToRetry` has `baseRevision: int`. `Effect.SubmitPendingBatch` has `baseRevision: int`. `Effect.PollServer` / `LoadServer` have `revision: int`.
- [[src/Shared/ApiResponses.fs]] `StateResponse.revision`, `ChangeSuccessResponse.revision`, `LoadRequest.revision`, `LoadResponse.revision` are `EventId`.
- [[src/Shared/SyncLogic.fs]] `ClientSyncState.revision: EventId`. Comment still says "Browser graph, Revision, and ClientHistory".
- [[src/Shared/BootCache.fs]] `SnapshotRecord.revision: int`.
- [[src/Server/Core/CoreChanges.fs]] `CoreChangesAccepted.revision: Revision`. `getRevision` on the handle returns `EventId`.
- [[src/Server/Core/CoreMsg.fs]] `GetRevision` replies `Result<Revision, string>`. `PersistHandlers.getRevision` returns `Result<Revision, string>`.
- [[src/Server/Core/CoreMailbox.fs]] `getRevision` unwraps `Revision` and returns `EventId`.
- [[src/Server/Database.fs]] table `graph` column `revision INT`. Row type `GraphSingletonRow.revision: int`. `replaceGraphProjectionWithTx` writes that column. `loadPersistedState` sets `State.revision = Revision revision`.
- [[src/Server/DatabaseProjection.fs]] `plan` takes `revision: int`. Patch graph row field `revision`.
- [[src/Server/Bookkeeping.fs]] `readRevision` returns `Revision`. `writeRevision` writes an int to `SYSTEM/gambol.meta`.
- [[src/Server/DocumentLoader.fs]] `stateFromGraph` sets `revision = Bookkeeping.readRevision`.
- [[src/Shared/WorkspaceUploadStructure.fs]] and [[src/Shared/dotnet/LazyLoadReconciliationApply.fs]] seed `State` with `revision = Revision.Zero` for local `Op.apply`.
- [[src/Shared/ModelBuilder.fs]] same `Revision.Zero` seed.
- [[src/Client/Program.fs]] initial VM `revision = Revision.Zero`.

### 3.3 JSON keys named revision

- [[src/Shared/EventJson.fs]] ActorStart body field `"revision"` encodes EventId.
- [[src/Shared/ApiResponseSerialization.fs]] StateResponse `"revision"` is EventId. LoadRequest `"revision"` is int mapped to EventId. ChangeSuccessResponse uses short key `"r"` for EventId, not `"revision"`.
- [[src/Shared/BootCache.fs]] and [[src/Client/BootCacheStore.fs]] snapshot JSON `"revision"` is int.
- [[src/Server/LazyLoadReconciliationServer.fs]] log text `"revision"`.
- [[src/Server/DatabaseProjection.fs]] projection parameter name `"revision"`.

### 3.4 Change.id used as serial

Leftover Change `id` is int. Call sites copy Revision or event id into it:

- Client: `id = model.revision.Value` in UpdateHelpers, UpdatePaste, UpdateEdit, UpdateMove, UpdateOps, UpdateRename, UpdateAmbleRun, UpdateFileSearch, UpdateWorkspaceDownload, UpdateWorkspaceSync.
- Shared: `Change.inverse` `id = baseRevision.Value`. `Ev.asChange` `id = event.id.Value`. `Ev.ofChange` `EventId change.id`. `SyncLogic.projectSuffixes` `id = state.revision.Value`. `ImportText` `id = revision`.
- Server: `GraphOnlyChangePost.postChunks` `id = revision.Value`. `Api.applyParseFile` `id = state.revision.Value`. `CoreEventDispatch.persist` `id = revision.Value`. FileAgent and DbAgent dedup `id = s.revision.Value`.
- [[src/Shared/SyncBatch.fs]] rewrites `Ev.id` from `baseRevision + idx` (event id serial on Ev, not leftover Change.id).

Converters `EventId.ofRevision` / `toRevision` sit on the Browser Sync path: [[src/Client/Update.fs]], [[src/Client/UpdateHelpers.fs]], [[src/Client/UpdateWorkspaceSync.fs]], [[src/Client/Program.fs]], [[src/Client/App.fs]], [[src/Shared/SyncLogic.fs]], [[src/Shared/BootCache.fs]].

Tests seed `State` / VM with `Revision.Zero` in many files (HistoryTests, EventTests, ImportDocumentTests, VmTestHelpers, WorkspaceOpsTests, and others listed in section 1.4).

## 4. Dual doors: postChange / postGraphOnlyChange taking Change vs postEvents taking Ev

### 4.1 Core Changes handle

[[src/Server/Core/CoreChanges.fs]] `CoreChanges` has three write doors: `postChange: Change list -> ...`, `postEvents: Ev list -> ...`, `postGraphOnlyChange: Change -> ...`. Comment: HTTP uses `postEvents`. `postChange` is graph-apply only. That comment is stale relative to mailbox: mailbox `postChange` converts Change to Ev first (section 2.2). Persist handlers still apply leftover Change lists (section 4.3).

[[src/Server/Core/CoreRuntime.fs]] reject stubs: `rejectPost` takes `Change list`, `rejectEvents` takes `Ev list`, `rejectGraph` takes leftover `Change`.

[[src/Server/Core/CoreCredentials.fs]] `CoreAuth.post` still wraps `Change list -> ...`.

### 4.2 Mailbox

[[src/Server/Core/CoreMailbox.fs]] `postChange` (Change list → `eventFromChange` → `PostEvent`). `postEvents` (Ev list → `PostEvent`). `postGraphOnlyChange` (one leftover Change → `PostGraphOnlyChange`). `postEvent` posts one Ev.

[[src/Server/Core/CoreMsg.fs]] `PostEvent` carries `Ev`. `PostGraphOnlyChange` carries leftover `Change`. There is no `PostChange` case. PersistHandlers have `postChange: Change list` and `postGraphOnlyChange: Change list` and `appendEvent: Ev`. PersistHandlers have no `postEvents`.

[[src/Server/Core/CoreMailboxBackend.fs]] `dispatchPostEvent` calls `CoreEventDispatch.postEvent` with `graphOnly = false`. `dispatchPostGraphOnlyChange` copies Change to Ev then `postEvent` with `graphOnly = true`.

[[src/Server/Core/CoreEventDispatch.fs]] `postEvent` is the Event admit path. After completeAction it calls `persist`, which builds leftover Change and calls `context.persist.postChange` or `postGraphOnlyChange`. Then `commit` calls `persist.appendEvent` on Ev. One Event post therefore: admit Ev, apply leftover Change, append Ev to EventLog.

### 4.3 Persist fillings apply leftover Change, not Ev

[[src/Server/Core/FileAgent.fs]] `handlers.postChange` / `postGraphOnlyChange` call `processPostChange` on a Change list. `appendEvent` writes Ev through [[src/Server/EventLogFile.fs]] `appendEvent`. Direct `handlers.postChange` skips the mailbox Event door. [[tests/Server.Tests/FileAgentFailureTests.fs]] states that: EventLog then has only the mailbox-admitted first Change.

[[src/Server/Core/DbAgent.fs]] same split: `processPostChange` on Change list; `appendPersistedEvent` writes Ev via [[src/Server/Database.fs]] `appendEvent`.

### 4.4 HTTP Adapter

[[src/Server/Api.fs]] `postEvents` decodes `EventJson.decodeEventBatch` (`{ events: Ev list }`) and calls `handle.postEvents`. `applyParseFile` calls `handle.postGraphOnlyChange` with leftover Change.

[[src/Server/RouteRegistration.fs]] `POST /ambit/changes` and `POST /ambit/events` both call `Api.postEvents`. The `/changes` URL is a name only. The body is an Ev batch.

[[src/Client/App.fs]] `runSubmitPendingBatch` posts `SyncBatch.toWireBatch` (`Ev list`) to `/{file}/changes`. [[src/Client/UpdateWorkspaceSync.fs]] `applyAndPostSync` posts the same Ev batch to `/{file}/changes`.

[[src/Shared/GraphOnlyChangeChunks.fs]] splits Op lists for `postGraphOnlyChange`. Callers: [[src/Server/LazyLoadReconciliationServer.fs]] `GraphOnlyChangePost.postChunks handle.postGraphOnlyChange`.

Actors in tests call `coreChanges.postChange [ change ]` ([[tests/Server.Tests/TestActor.fs]], [[tests/Server.Tests/ActorCoreChangesDoorTests.fs]]).

## 5. Leftover [[src/Shared/EventId.fs]]

[[src/Shared/EventId.fs]] defines `type EventId = EventId of int` and `module EventId` with `zero`, `next`, `max`. It does not define `ofRevision` / `toRevision`.

[[src/Shared/Gambol.Shared.fsproj]] does not compile `EventId.fs`. Live EventId is in [[src/Shared/History.fs]]. [[src/Shared/dotnet/Gambol.Shared.DotNet.fsproj]] does not compile `EventId.fs` either.

No test references `EventId.fs`.

## 6. Other Graph mutate, persist, or apply paths that are not Event

EventLog persist of a stored record is Ev: [[src/Server/EventLogFile.fs]] `appendEvent`, [[src/Server/Database.fs]] `appendEvent` into table `events`, [[src/Server/Core/CoreEventDispatch.fs]] `commit`, FileAgent and DbAgent `appendEvent` handlers. There is no remaining EventLog write of leftover Change JSON. Startup restore is Ev: FileAgent `EventLog.restorePersisted`, DbAgent `loadRestoredEventLog`.

Graph apply of accepted work is not Ev. Persist fillings apply leftover Change then append Ev (section 4.3).

### 6.1 Apply Ops without wrapping as Event

- [[src/Shared/History.fs]] `Op.apply` / `Op.undo` mutate Graph through GraphMutate. `Change.apply` / `Change.undo` fold those. `ChangeValidation.applyChange` is the ownership-checked leftover-Change apply.
- [[src/Shared/ResidentProjection.fs]] `applyOp` / `applyOps` / `applyChange` call `Op.apply` under Loaded rules.
- [[src/Shared/ChangeAmendment.fs]] `buildAmendedOps` calls `Op.apply` to probe recoverable CAS.
- [[src/Shared/WorkspaceUploadStructure.fs]] private `applyOps` folds `Op.apply` on `State` with `Revision.Zero`.
- [[src/Shared/dotnet/LazyLoadReconciliationApply.fs]] `applyOps` folds `Op.apply` the same way. [[src/Shared/dotnet/LazyLoadReconciliation.fs]] uses that apply while it plans Ops. Planned Ops then go to `postGraphOnlyChange` as leftover Change (section 4.4).
- [[src/Shared/DocumentPathMove.fs]] `planRenameNode` calls `Graph.setName` to validate, then returns `Op.SetName`. That Graph result is discarded. The live write is the Op.

### 6.2 Graph mutate that is not Op and not Event

[[src/Shared/GraphMutate.fs]] is the Graph write primitive (`setText`, `setClasses`, `setName`, `setDocumentState`, `replace`). [[src/Shared/GraphOps.fs]] exposes those as `Graph.*`. Production callers besides `Op.apply` / `Op.undo`:

- [[src/Shared/ModelBuilder.fs]] `Graph.newNode` and `Graph.replace` build fixture Graphs.
- [[src/Shared/documents/DocumentColdParse.fs]] `Graph.create` / `Graph.fromNodes` for paste cold plan.
- [[src/Shared/dotnet/ImportDocument.fs]] and [[src/Shared/dotnet/DocumentAssembly.fs]] `Graph.fromNodes` while they parse documents into Graph.
- [[src/Shared/ResidentProjection.fs]] `installPackages` merges package Nodes via `Graph.fromNodes`. [[src/Shared/SyncLogic.fs]] calls `installPackages` on Poll/Load packages.
- [[src/Shared/GraphProjection.fs]] rebuilds Graph from projection rows.
- [[src/Server/DatabaseProjection.fs]] `Graph.fromNodes` when it trims deleted ids. `persistWithTx` writes `graph` / `nodes` / `node_children` with a `revision` int. That is Graph persist, not EventLog.
- [[src/Server/Database.fs]] `replaceGraphProjectionWithTx`, `tryLoadGraphFromProjection`, `rebuildFromDocumentFiles`.
- [[src/Server/DocumentLoader.fs]] loads Graph from documents and stamps `State.revision` from bookkeeping, not from EventLog.
- [[src/Server/DocumentPersistence.fs]] `persistGraphOps` / `persistGraphChange` write document files from a Graph delta and Op list. FileAgent and DbAgent call this after leftover-Change apply. This is file persist, not EventLog.
- [[src/Server/Bookkeeping.fs]] `writeRevision` persists the retired Revision int beside EventLog.
- [[src/Server/Core/CoreActorPool.fs]] `Graph.fromNodes` builds an Actor subgraph (read overlay, not EventLog).
- [[src/Server/Core/CoreMailboxBackend.fs]] `GraphSpan.withLockPresent` overlays lock-present on `getState` Graph. That is a read overlay, not persist.
- [[src/Client/Program.fs]] `Graph.create` for the empty Browser Graph before Load.

### 6.3 Dual serial persist

File mode: leftover Change apply updates in-memory `State.revision` (`Revision (n+1)` in FileAgent `applyBatch`). EventLog next id is `EventLog.nextId` on Ev. Bookkeeping `gambol.meta` stores the Revision int. Three serials can diverge if `handlers.postChange` runs without `appendEvent`.

Db mode: leftover Change apply updates `State.revision`. `DatabaseProjection.plan` writes `graph.revision`. `Database.appendEvent` writes `events.event_id` from `Ev.id`. `loadPersistedState` still accepts a leftover-Change decoder it does not use.

### 6.4 Alias that still says Change

[[src/Shared/ApiResponses.fs]] `ChangeSuccessResponse.changes`, `LoadResponse.changes`, `SyncResponse.changes` are members that return `this.events`. Wire key for the Ev list on ACK is `"c"` in [[src/Shared/ApiResponseSerialization.fs]].

[[src/Server/EventLogFile.fs]] module comment still says "Events and Changes".

## Answer

Every Graph-mutating unit that Sync, Parse, and Actor posts still has a leftover Change record `{ id; submissionId; ops }`. HTTP POST `/ambit/events` and `/ambit/changes` already send Ev. The mailbox Event door copies that leftover record to Ev, then copies Ev back to leftover Change for FileAgent/DbAgent apply, then appends Ev to EventLog. `postGraphOnlyChange` still takes leftover Change. `type Revision` and `Change.id` as int serial remain beside `EventId`. [[src/Shared/EventId.fs]] is unused. Document load, package install, projection SQL, and file persist mutate or store Graph without an Event.
