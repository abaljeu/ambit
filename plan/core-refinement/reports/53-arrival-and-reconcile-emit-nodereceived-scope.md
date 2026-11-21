# Scope — [53 — Arrival and reconcile emit NodeReceived](../issues/53-arrival-and-reconcile-emit-nodereceived.md)

Scope mode is implement for the core-refinement arrival and reconcile `Notice.NodeReceived` expand slice.

## Signatures

[ ] `type Notice` in `src/Shared/History.fs` includes `| NodeReceived of nodeId: NodeId`.
[ ] `type EventBody` in `src/Shared/History.fs` includes `| Notice of Notice`.
[ ] `type Op` in `src/Shared/History.fs` includes `| SetDocumentState of nodeId: NodeId * oldState: DocumentStateEnum * newState: DocumentStateEnum`.
[ ] `WorkspaceUploadStructure.planServerFilePresentOps : Graph -> string -> string list -> Op list` in `src/Shared/WorkspaceUploadStructure.fs`.
[ ] `Api.postEvents : CoreChangesDoor -> int -> int -> string -> Async<IResult>` in `src/Server/Api.fs`.
[ ] `Api.inferredNodeEvents : Ev -> Notice list` is private in `src/Server/Api.fs`.
[ ] `CoreDoor.postEvents : CoreChangesDoor -> Ev list -> Async<Result<CoreChangesAccepted, string>>` in `src/Server/Core/CoreDoor.fs`.
[ ] `CoreMailboxApi.postNotice : MailboxHost -> Notice -> unit` is internal in `src/Server/Core/CoreMailboxApi.fs`.
[ ] `CoreMailboxLoop.applyNotice : PersistHandlers -> Notice -> Result<unit, string>` in `src/Server/Core/CoreMailboxLoop.fs`.
[ ] `CoreMailboxFilesAxis.writeReceived : PersistHandlers -> NodeId -> Result<unit, string>` in `src/Server/Core/CoreMailboxFilesAxis.fs`.
[ ] `GraphMutate.setParseState : NodeId -> ParseState -> Graph -> Result<Graph, string>` and `GraphMutate.setPersistState : NodeId -> PersistState -> Graph -> Result<Graph, string>` in `src/Shared/GraphMutate.fs`.
[ ] `LazyLoadReconciliationServer.reconcileReceivedFiles : WorkspaceReconcile -> LazyLoadReconciliation.ChangedPath list -> Async<Result<LazyLoadReconciliationReport.Failure list, string>>` in `src/Server/LazyLoadReconciliationServer.fs`.
[ ] `LazyLoadReconciliationServer.WorkspaceReconcile = { handle: CoreChangesDoor; dataDir: string; workspaceLabel: string }` in `src/Server/LazyLoadReconciliationServer.fs`.
[ ] `type CoreChangesDoor = { host: MailboxHost; caller: Caller; writable: bool }` in `src/Server/Core/CoreChangesDoor.fs`.
[ ] `CoreEventDispatch.logNotice` and `CoreEventDispatch.writeAxis` in `src/Server/Core/CoreEventDispatch.fs` log and apply axis writes for Notices.

## Match sites

[ ] `Notice.NodeReceived` case definition: `src/Shared/History.fs`, `type Notice`.
[ ] JSON encode match: `EventJson.encodeNotice`, `src/Shared/EventJson.fs`, case `Notice.NodeReceived nodeId` with tag `"nodeReceived"`.
[ ] JSON decode match: `EventJson.noticeFrom`, `src/Shared/EventJson.fs`, tag `"nodeReceived"`.
[ ] Apply match: `CoreMailboxLoop.applyNotice`, `src/Server/Core/CoreMailboxLoop.fs`, arm calls `CoreMailboxFilesAxis.writeReceived`.
[ ] Axis notice argument: `CoreMailboxFilesAxis.writeReceived`, `src/Server/Core/CoreMailboxFilesAxis.fs`, passes `Notice.NodeReceived nodeId` into `CoreEventDispatch.writeAxis`.
[ ] Database row match: `Database.writeNoticeRow`, `src/Server/Database.fs`, arm `Notice.NodeReceived nodeId` calls `writeReceivedNode`.
[ ] Arrival inference pick: `Api.inferredNodeEvents`, `src/Server/Api.fs`, `| id, PullingFile, Unparsed -> Some (Notice.NodeReceived id)`.
[ ] Queue dispatch match: `CoreMailboxLoop.dispatchItem`, `src/Server/Core/CoreMailboxLoop.fs`, `CoreQueue.Notice msg` calls `logNotice` then `applyNotice`.
[ ] Reconcile beside callback: `LazyLoadReconciliationServer.reconcileReceivedFiles`, `src/Server/LazyLoadReconciliationServer.fs`, `receivedFileIds` then `List.iter (CoreMailboxApi.postNotice workspace.handle.host << Notice.NodeReceived)`.
[ ] Reconcile id pick: private `receivedFileIds` in `src/Server/LazyLoadReconciliationServer.fs` chooses Modified files in `Current | PullingFile`, Added paths on `PullingFile` stubs, and Renamed files when reconcile ops mark Unparsed.
[ ] Test predicate match: `NodeEventExpandTests.received`, `tests/Server.Tests/NodeEventExpandTests.fs`, `| Notice.NodeReceived found -> found = id`.
[ ] Caller `Api.postEvents`: after `CoreDoor.postEvents` succeeds, `List.collect inferredNodeEvents` then `List.iter (CoreMailboxApi.postNotice handle.host)` in `src/Server/Api.fs`.
[ ] HTTP caller `RouteRegistration`: git pull and lazy-load routes call `LazyLoadReconciliationServer.reconcileReceivedFiles` in `src/Server/RouteRegistration.fs`.
[ ] Arrival ops producer: `WorkspaceUploadStructure.planServerFilePresentOps` emits `Op.SetDocumentState(fileId, PullingFile, Unparsed)` for matching File stubs in `src/Shared/WorkspaceUploadStructure.fs`.

## Existing tests

[ ] `NodeReceived applies Unparsed` in `tests/Server.Tests/CoreLoopNoticeTests.fs` calls `CoreMailboxLoop.applyNotice` with `Notice.NodeReceived`; asserts Unparsed, `PersistState.Persisted`, `pullingFile` false, and projection row words `unparsed` / `persisted`.
[ ] `incoming file posts NodeReceived beside SetDocumentState` in `tests/Server.Tests/NodeEventExpandTests.fs` calls `Api.postEvents` with ops from `WorkspaceUploadStructure.planServerFilePresentOps`; asserts `SetDocumentState` to Unparsed, logged `Notice.NodeReceived` for the arrived file, and graph Unparsed; does not assert Persisted or cleared PullingFile on that node.
[ ] `git changed file posts NodeReceived beside SetDocumentState` in `tests/Server.Tests/NodeEventExpandTests.fs` calls `LazyLoadReconciliationServer.reconcileReceivedFiles` with `LazyLoadReconciliation.Modified`; asserts reconcile wrote Unparsed, logged `Notice.NodeReceived`, and graph Unparsed.
[ ] `renamed file posts NodeReceived` in `tests/Server.Tests/NodeEventExpandTests.fs` calls `reconcileReceivedFiles` via `pullReceived` with `LazyLoadReconciliation.Renamed`; asserts logged `Notice.NodeReceived` and graph Unparsed.
[ ] `moved file posts NodeReceived` in `tests/Server.Tests/NodeEventExpandTests.fs` calls `reconcileReceivedFiles` with a cross-directory rename; asserts logged `Notice.NodeReceived` and graph Unparsed.
[ ] `renamed and modified file posts NodeReceived` in `tests/Server.Tests/NodeEventExpandTests.fs` calls `reconcileReceivedFiles` with rename plus disk content; asserts logged `Notice.NodeReceived` and graph Unparsed.
[ ] `NodeReceived writes the received node to the database` in `tests/Server.Tests/DbAgentNoticeTests.fs` applies `Notice.NodeReceived` through the database agent path; asserts Unparsed, Persisted, and update time change (not `Api.postEvents` or reconcile).
[ ] `Notice JSON round trip` in `tests/Shared.Tests/EventJsonTests.fs` includes `Notice.NodeReceived nodeId` in the Notice list.
[ ] `planServerFilePresentOps transitions only matching absent files` in `tests/Shared.Tests/WorkspaceUploadStructureTests.fs` applies ops locally; asserts PullingFile to Unparsed ops only (no server or Notice).
[ ] Lazy-load reconcile integration tests in `tests/Server.Tests/LazyLoadReconciliationServerTests.fs` call `reconcileReceivedFiles` for graph marks; they do not assert `EventBody.Notice` `Notice.NodeReceived`.

## Copy-from

[ ] ``upload emits NodeUploading`` in `tests/Server.Tests/NodeEventExpandTests.fs` calls `Api.postEvents` through `postChange`, reads the EventLog for the Notice, and reads `CoreMailboxApi.getState` for PullingFile and Unpersisted ([50 — Upload emits NodeUploading](../issues/50-upload-emits-nodeuploading.md) acceptance pattern).
[ ] `Api.inferredNodeEvents` in `src/Server/Api.fs` already maps `Op.SetDocumentState(id, PullingFile, Unparsed)` to `Notice.NodeReceived` and `Api.postEvents` posts each result through `CoreMailboxApi.postNotice`.
[ ] `LazyLoadReconciliationServer.reconcileReceivedFiles` in `src/Server/LazyLoadReconciliationServer.fs` already posts `Notice.NodeReceived` beside reconcile ops through `CoreMailboxApi.postNotice` (pull request 263 path named in the ticket).
[ ] `NodeEventExpandTests.postChange` and `NodeEventExpandTests.pullReceived` in `tests/Server.Tests/NodeEventExpandTests.fs` are the HTTP-level and reconcile entry harnesses on a `CoreChangesDoor`.
[ ] `NodeEventExpandTests.received` and `NodeEventExpandTests.hasCase` in `tests/Server.Tests/NodeEventExpandTests.fs` scan `CoreMailboxApi.eventsSince` for `EventBody.Notice` cases (public EventLog read seam).

## implement

[ ] Ticket Context names `CoreMailboxLoad.applyNotice`; the `Notice.NodeReceived` apply arm is `CoreMailboxLoop.applyNotice` in `src/Server/Core/CoreMailboxLoop.fs` ([44 — Apply document state from Node events](../issues/44-apply-document-state-from-file-events.md) finished the handler there).
[ ] Ticket story **Arrival post** is unchecked; `src/Server/Api.fs` already runs `CoreMailboxApi.postNotice` for each `Notice.NodeReceived` from `inferredNodeEvents` after a successful Change post whose ops include `SetDocumentState(_, PullingFile, Unparsed)`.
[ ] Ticket reconcile path is already live: `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `Notice.NodeReceived` through `CoreMailboxApi.postNotice` for ids from `receivedFileIds`; no new producer code is required for reconcile emit unless a gap appears in tests.
[ ] Ticket acceptance names test `incoming file emits NodeReceived`; no `[<Fact>]` with that name exists (nearest name is `incoming file posts NodeReceived beside SetDocumentState`).
[ ] Ticket acceptance names test `reconcile emits NodeReceived`; no `[<Fact>]` with that name exists (nearest names are `git changed file posts NodeReceived beside SetDocumentState` and related rename tests).
[ ] Ticket acceptance requires both new tests to assert the `Notice.NodeReceived` handler sets Unparsed and Persisted and clears PullingFile; existing expand tests assert Unparsed and EventLog Notice but not `PersistState.Persisted` or `documentState.pullingFile` false after the public entry call.
[ ] [57 — Upload Actor posts Node notices](../issues/57-upload-actor-posts-document-state.md) will remove `inferredNodeEvents`; [53 — Arrival and reconcile emit NodeReceived](../issues/53-arrival-and-reconcile-emit-nodereceived.md) scope stays the interim `Api.postEvents` expand path for arrival until that Actor exists.
[ ] Named new test `incoming file emits NodeReceived` should enter through public seam `Api.postEvents` with a `CoreChangesDoor` and encoded Change whose ops come from `WorkspaceUploadStructure.planServerFilePresentOps` for a File stub in PullingFile, matching `NodeEventExpandTests.postChange`.
[ ] That test should observe `Notice.NodeReceived` on the EventLog via `CoreMailboxApi.eventsSince` or `NodeEventExpandTests.received` after `Api.postEvents` completes.
[ ] That test should observe handler effect on graph state via `CoreMailboxApi.getState`: File Node `DocumentStateEnum.Unparsed`, `documentState.persistState = PersistState.Persisted`, and `documentState.pullingFile` false.
[ ] Named new test `reconcile emits NodeReceived` should enter through public seam `LazyLoadReconciliationServer.reconcileReceivedFiles` with a `WorkspaceReconcile` record and a `LazyLoadReconciliation.ChangedPath` that `receivedFileIds` treats as a changed File Node (for example `Modified` on a PullingFile stub or `Added` on a PullingFile path), matching `NodeEventExpandTests.pullReceived`.
[ ] That test should observe `Notice.NodeReceived` on the EventLog via the same `eventsSince` scan after `reconcileReceivedFiles` completes.
[ ] That test should observe handler effect via `CoreMailboxApi.getState`: target File Node Unparsed, Persisted, and PullingFile clear, matching the assertions in `NodeReceived applies Unparsed` but driven from reconcile rather than direct `applyNotice`.
