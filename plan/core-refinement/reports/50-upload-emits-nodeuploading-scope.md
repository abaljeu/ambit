# Scope — [50 — Upload emits NodeUploading](../issues/50-upload-emits-nodeuploading.md)

Scope mode is implement for the core-refinement upload Notice expand slice.

## Signatures

[ ] `type Notice` in `src/Shared/History.fs` includes `| NodeUploading of nodeId: NodeId`.
[ ] `type EventBody` in `src/Shared/History.fs` includes `| Notice of Notice`.
[ ] `type Op` in `src/Shared/History.fs` includes `| SetDocumentState of nodeId: NodeId * oldState: DocumentStateEnum * newState: DocumentStateEnum`.
[ ] `WorkspaceUploadStructure.planStubOps : Graph -> string -> InventoryItem list -> Result<Op list, string>` in `src/Shared/WorkspaceUploadStructure.fs`.
[ ] `Api.postEvents : CoreChangesDoor -> int -> int -> string -> Async<IResult>` in `src/Server/Api.fs`.
[ ] `Api.inferredNodeEvents : Ev -> Notice list` is private in `src/Server/Api.fs`.
[ ] `CoreDoor.postEvents : CoreChangesDoor -> Ev list -> Async<Result<CoreChangesAccepted, string>>` in `src/Server/Core/CoreDoor.fs`.
[ ] `CoreMailboxApi.postEvents : MailboxHost -> Caller -> Ev list -> Async<Result<CoreChangesAccepted, string>>` in `src/Server/Core/CoreMailboxApi.fs`.
[ ] `CoreMailboxApi.postNotice : MailboxHost -> Notice -> unit` is internal in `src/Server/Core/CoreMailboxApi.fs`.
[ ] `CoreMailboxLoop.applyNotice : PersistHandlers -> Notice -> Result<unit, string>` in `src/Server/Core/CoreMailboxLoop.fs`.
[ ] `CoreMailboxFilesAxis.writePullingFileUnpersisted : PersistHandlers -> NodeId -> Result<unit, string>` in `src/Server/Core/CoreMailboxFilesAxis.fs`.
[ ] `CoreEventDispatch.logNotice : Context -> Notice -> Result<Ev, string>` in `src/Server/Core/CoreEventDispatch.fs`.
[ ] `CoreEventDispatch.writeAxis : PersistHandlers -> Notice -> (Graph -> Result<Graph, string>) -> Result<unit, string>` in `src/Server/Core/CoreEventDispatch.fs`.
[ ] `type CoreChangesDoor = { host: MailboxHost; caller: Caller; writable: bool }` in `src/Server/Core/CoreChangesDoor.fs`.
[ ] `Node.withDocumentState : DocumentStateEnum -> Node -> Node` and `GraphMutate.setPersistState : NodeId -> PersistState -> Graph -> Result<Graph, string>` are used from `CoreMailboxFilesAxis.writePullingFileUnpersisted`.

## Match sites

[ ] `Notice.NodeUploading` case definition: `src/Shared/History.fs`, `type Notice`.
[ ] JSON encode match: `EventJson.encodeNotice`, `src/Shared/EventJson.fs`, case `Notice.NodeUploading nodeId`.
[ ] JSON decode match: `EventJson.decodeNotice`, `src/Shared/EventJson.fs`, tag `"nodeUploading"`.
[ ] Apply match: `CoreMailboxLoop.applyNotice`, `src/Server/Core/CoreMailboxLoop.fs`, arm calls `CoreMailboxFilesAxis.writePullingFileUnpersisted`.
[ ] Axis notice argument: `CoreMailboxFilesAxis.writePullingFileUnpersisted`, `src/Server/Core/CoreMailboxFilesAxis.fs`, passes `Notice.NodeUploading nodeId` into `CoreEventDispatch.writeAxis`.
[ ] Database projection match: `Database.projectNotice`, `src/Server/Database.fs`, arm `Notice.NodeUploading _` is a no-op task.
[ ] Inference pick: `Api.inferredNodeEvents`, `src/Server/Api.fs`, `| id, _, PullingFile -> Some (Notice.NodeUploading id)`.
[ ] Queue dispatch match: `CoreMailboxLoop.dispatchItem`, `src/Server/Core/CoreMailboxLoop.fs`, `CoreQueue.Notice msg` calls `logNotice` then `applyNotice`.
[ ] Test predicate match: `NodeEventExpandTests.uploading`, `tests/Server.Tests/NodeEventExpandTests.fs`, `| Notice.NodeUploading found -> found = id`.
[ ] Caller `Api.postEvents`: after `CoreDoor.postEvents` succeeds, `List.collect inferredNodeEvents` then `List.iter (CoreMailboxApi.postNotice handle.host)` in `src/Server/Api.fs`.
[ ] Caller `CoreDoor.postEvents`: `callIfReady CoreMailboxApi.postEvents` in `src/Server/Core/CoreDoor.fs`.
[ ] HTTP caller `RouteRegistration`: `POST /ambit/changes` and `POST /ambit/events` invoke `Api.postEvents` in `src/Server/RouteRegistration.fs`.
[ ] Upload Change ops producer: `WorkspaceUploadStructure.markNewStubsUnparsed` calls private `markPullingFile` via `foldStateOps` for new File stubs in `src/Shared/WorkspaceUploadStructure.fs`.
[ ] Parallel expand caller (NodeReceived, not Upload): `LazyLoadReconciliationServer.reconcileReceivedFiles` `beside` callback posts `Notice.NodeReceived` through `CoreMailboxApi.postNotice` in `src/Server/LazyLoadReconciliationServer.fs`.

## Existing tests

[ ] `NodeUploading applies PullingFile` in `tests/Server.Tests/CoreLoopNoticeTests.fs` calls `CoreMailboxLoop.applyNotice` with `Notice.NodeUploading`; asserts PullingFile, `PersistState.Unpersisted`, stored row `document_state` is `unparsed`, and node count unchanged.
[ ] `NodeUploading leaves the Edit's changedVersion` in `tests/Server.Tests/CoreLoopNoticeTests.fs` calls `CoreMailboxLoop.applyNotice` with `Notice.NodeUploading`; asserts PullingFile, Unpersisted, and `changedVersion` unchanged.
[ ] `upload posts NodeUploading beside SetDocumentState` in `tests/Server.Tests/NodeEventExpandTests.fs` calls `Api.postEvents` with ops from `WorkspaceUploadStructure.planStubOps`; asserts `Op.SetDocumentState` to PullingFile on the Change, a logged `EventBody.Notice` `Notice.NodeUploading` for the File Node, and graph `PullingFile` via `assertBeside`.
[ ] `markPullingFile sets PullingFile` in `tests/Shared.Tests/WorkspaceUploadStructureTests.fs` applies `planStubOps` ops locally; asserts File Node PullingFile after op apply only (no server or Notice).
[ ] `upload structure persistence preserves existing file mtime` in `tests/Server.Tests/DocumentPersistenceTests.fs` applies upload structure ops; asserts stamped graph File Node is PullingFile and disk file absent (persist path, not Notice expand).
[ ] `NodeUploading` round-trip in `tests/Shared.Tests/EventJsonTests.fs` encodes and decodes `Notice.NodeUploading nodeId`.

## Copy-from

[ ] `Api.inferredNodeEvents` in `src/Server/Api.fs` already maps `Op.SetDocumentState(_, _, PullingFile)` to `Notice.NodeUploading` and is invoked from `Api.postEvents` through `CoreMailboxApi.postNotice`.
[ ] `LazyLoadReconciliationServer.reconcileReceivedFiles` in `src/Server/LazyLoadReconciliationServer.fs` posts `Notice.NodeReceived` beside reconcile ops through `CoreMailboxApi.postNotice` (same expand pattern as Upload for [53 — Arrival and reconcile emit NodeReceived](../issues/53-arrival-and-reconcile-emit-nodereceived.md)).
[ ] `NodeEventExpandTests.postChange` in `tests/Server.Tests/NodeEventExpandTests.fs` builds a Change request body and calls `Api.postEvents` on a `CoreChangesDoor` (harness for an integration test).
[ ] `mailbox queue applies NodePersisted off CoreMsg` in `tests/Server.Tests/CoreLoopNoticeTests.fs` posts a Notice through `CoreMailboxApi.postNotice` then reads graph state with `CoreMailboxApi.getState` (pattern for observing handler effects off the mailbox queue).

## implement

[ ] Ticket Context names `CoreMailboxLoad.applyNotice`; the `Notice.NodeUploading` apply arm is `CoreMailboxLoop.applyNotice` in `src/Server/Core/CoreMailboxLoop.fs` ([44 — Apply document state from Node events](../issues/44-apply-document-state-from-file-events.md) finished the handler there).
[ ] Ticket story **Upload post** is unchecked; `src/Server/Api.fs` already runs `CoreMailboxApi.postNotice` for each `Notice.NodeUploading` from `inferredNodeEvents` after a successful upload Change post through `CoreDoor.postEvents`.
[ ] Ticket acceptance names test `upload emits NodeUploading`; no `[<Fact>]` with that name exists in the repo (nearest name is `upload posts NodeUploading beside SetDocumentState`).
[ ] Ticket acceptance requires the new test to assert `PersistState.Unpersisted` on the File Node after `Api.postEvents`; `upload posts NodeUploading beside SetDocumentState` asserts PullingFile on the graph and Notice on the EventLog but does not assert Unpersisted.
[ ] [57 — Upload Actor posts Node notices](../issues/57-upload-actor-posts-document-state.md) will remove `inferredNodeEvents`; [50 — Upload emits NodeUploading](../issues/50-upload-emits-nodeuploading.md) scope stays the interim `Api.postEvents` expand path until that Actor exists.
[ ] Named new test `upload emits NodeUploading` should enter through public seam `Api.postEvents` with a `CoreChangesDoor` and encoded Change whose ops come from `WorkspaceUploadStructure.planStubOps` (upload Change), matching `NodeEventExpandTests.postChange`.
[ ] The new test should observe `Notice.NodeUploading` on the EventLog via `CoreMailboxApi.eventsSince` or equivalent `EventBody.Notice` scan (same public read path as `NodeEventExpandTests.hasCase`).
[ ] The new test should observe handler effect on Graph State via `CoreMailboxApi.getState`: File Node `documentState.pullingFile` true, `DocumentStateEnum.PullingFile`, and `documentState.persistState = PersistState.Unpersisted` after `Api.postEvents` completes.
