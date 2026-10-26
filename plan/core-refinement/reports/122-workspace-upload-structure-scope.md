# 122 — Document state moves by Notice — Workspace upload structure

Ticket: [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md).

Slice: in `src/Shared/WorkspaceUploadStructure.fs`, each `Op.SetDocumentState` at lines 143, 145, 151, 153, 160, and 319 also sends an equivalent Notice through Mailbox; keep `Op.SetDocumentState`; no new test.

## 1. Signatures

[ ] `type DocumentStateEnum = | Current | Unparsed | PullingFile` in `src/Shared/Model.fs`.
[ ] `Op.SetDocumentState of nodeId: NodeId * oldState: DocumentStateEnum * newState: DocumentStateEnum` on `type Op` in `src/Shared/History.fs`.
[ ] `type Notice` cases used by this slice: `NodeUploading`, `NodeReceived`, `MarkUnparsed`, and `NodeParsed` of `NodeId` in `src/Shared/History.fs`.
[ ] `Ev.documentStateFromNotice : Notice -> (NodeId * DocumentStateEnum) option` in `src/Shared/History.fs` (enum a Notice writes; same target as `Op.SetDocumentState` apply).
[ ] `GraphMutate.setDocumentState : NodeId -> DocumentStateEnum -> DocumentStateEnum -> Graph -> Result<Graph, string>` in `src/Shared/GraphMutate.fs` (no-op when the node already holds `newState`).
[ ] `DocumentOpImpact.noticesBeside : Op list -> Notice list` in `src/Shared/DocumentOpImpact.fs`.
[ ] `ResidentProjection.applyDocumentNotice : Notice -> State -> ApplyResult` (internal) in `src/Shared/ResidentProjection.fs`.
[ ] `CoreMailboxApi.postNotice : MailboxHost -> Notice -> unit` (internal) in `src/Server/Core/CoreMailboxApi.fs`.
[ ] `WorkspaceUploadStructure.InventoryItem` record and `StructureCap = FullPaths | TopLevelOnly` in `src/Shared/WorkspaceUploadStructure.fs`.
[ ] `WorkspaceUploadStructure.planStubOps : Graph -> string -> InventoryItem list -> Result<Op list, string>` (public) in `src/Shared/WorkspaceUploadStructure.fs`.
[ ] `WorkspaceUploadStructure.planServerFilePresentOps : Graph -> string -> string list -> Op list` (public) in `src/Shared/WorkspaceUploadStructure.fs`.
[ ] `WorkspaceUploadStructure.markUnparsed` (private): `Graph -> NodeId -> Op list` in `src/Shared/WorkspaceUploadStructure.fs`.
[ ] `WorkspaceUploadStructure.markPullingFile` (private): `Graph -> NodeId -> Op list` in `src/Shared/WorkspaceUploadStructure.fs`.
[ ] `WorkspaceUploadStructure.markParentCurrent` (private): `Graph -> NodeId -> Op list` in `src/Shared/WorkspaceUploadStructure.fs`.
[ ] `WorkspaceUploadStructure.foldStateOps` (private): `(Graph -> NodeId -> Op list) -> Graph -> NodeId list -> Result<Graph * Op list, string>` in `src/Shared/WorkspaceUploadStructure.fs`.
[ ] `WorkspaceUploadStructure.markNewStubsUnparsed` (private): `Graph -> Op list -> Result<Op list, string>` in `src/Shared/WorkspaceUploadStructure.fs`.
[ ] `Api.inferredNodeEvents : Ev -> Notice list` (private) in `src/Server/Api.fs` delegates to `DocumentOpImpact.noticesBeside` on `EventBody.Change` ops.

## 2. Match sites

[ ] `Op.SetDocumentState(nodeId, Current, Unparsed)` constructor in `WorkspaceUploadStructure.markUnparsed` when `Node.documentStateEnum` is `Current` (`src/Shared/WorkspaceUploadStructure.fs`).
[ ] `Op.SetDocumentState(nodeId, PullingFile, Unparsed)` constructor in `WorkspaceUploadStructure.markUnparsed` when state is `PullingFile` (`src/Shared/WorkspaceUploadStructure.fs`).
[ ] `Op.SetDocumentState(nodeId, Current, PullingFile)` constructor in `WorkspaceUploadStructure.markPullingFile` when state is `Current` (`src/Shared/WorkspaceUploadStructure.fs`).
[ ] `Op.SetDocumentState(nodeId, Unparsed, PullingFile)` constructor in `WorkspaceUploadStructure.markPullingFile` when state is `Unparsed` (`src/Shared/WorkspaceUploadStructure.fs`).
[ ] `Op.SetDocumentState(nodeId, Unparsed, Current)` constructor in `WorkspaceUploadStructure.markParentCurrent` for `Special (Directory | Workspace)` with legacy `Unparsed` (`src/Shared/WorkspaceUploadStructure.fs`).
[ ] `Op.SetDocumentState(fileId, PullingFile, Unparsed)` constructor in `WorkspaceUploadStructure.planServerFilePresentOps` when the resolved File is `PullingFile` (`src/Shared/WorkspaceUploadStructure.fs`).
[ ] `Node.documentStateEnum` match in `markUnparsed`, `markPullingFile`, and `markParentCurrent` (`src/Shared/WorkspaceUploadStructure.fs`).
[ ] `Op.SetDocumentState` match arm in `DocumentOpImpact.documentStateTransition` (`src/Shared/DocumentOpImpact.fs`).
[ ] `Op.SetDocumentState` match arm in `ResidentProjection.applyOpAllowing` (`src/Shared/ResidentProjection.fs`).
[ ] Caller `foldStateOps markUnparsed` from `markNewStubsUnparsed` for new Directory stub ids (`src/Shared/WorkspaceUploadStructure.fs`).
[ ] Caller `foldStateOps markPullingFile` from `markNewStubsUnparsed` for new File stub ids (`src/Shared/WorkspaceUploadStructure.fs`).
[ ] Caller `foldStateOps markParentCurrent` from `markNewStubsUnparsed` for distinct owner ids of new stubs (`src/Shared/WorkspaceUploadStructure.fs`).
[ ] Caller `foldStateOps markParentCurrent` from `promoteUnparsedDirsWithMembers` for Unparsed Directory or Workspace nodes that already have an owned member (`src/Shared/WorkspaceUploadStructure.fs`).
[ ] Caller `markNewStubsUnparsed` from `planStubOps` after create ops are folded (`src/Shared/WorkspaceUploadStructure.fs`).
[ ] Production caller `WorkspaceCloudUpload.postStubs` calls `WorkspaceUploadStructure.planStubOps` then `AmbitSession.postOps` (`src/Shared/dotnet/WorkspaceCloudUpload.fs`).
[ ] Production caller `WorkspaceCloudUpload.markBodiesPresent` calls `WorkspaceUploadStructure.planServerFilePresentOps` then `AmbitSession.postOps` (`src/Shared/dotnet/WorkspaceCloudUpload.fs`).
[ ] Production caller `UpdateWorkspaceSync.completeUploadInventoryWith` calls `planStubOps` then `applyStructureLocally` (`src/Client/UpdateWorkspaceSync.fs`).
[ ] Production caller `UpdateWorkspaceSync.markServerFilesPresent` calls `planServerFilePresentOps` then `applyAndPostSync` (`src/Client/UpdateWorkspaceSync.fs`).
[ ] Server Mailbox caller `Api.postEvents` runs `List.collect inferredNodeEvents` then `List.iter (CoreMailboxApi.postNotice handle.host)` after `CoreDoor.postEvents` succeeds (`src/Server/Api.fs`).
[ ] Server Mailbox caller `FileAgent.postGraphOnlyNotices` posts `DocumentOpImpact.noticesBeside` for graph-only Changes (`src/Server/Core/FileAgent.fs`).
[ ] Parallel beside pattern (not in `WorkspaceUploadStructure.fs`): `LazyLoadReconciliationServer.reconcileReceivedFiles` `beside` callback posts `Notice.NodeReceived` per id from `receivedFileIds` after reconcile ops (`src/Server/LazyLoadReconciliationServer.fs`).

## 3. Existing tests

[ ] ``markPullingFile sets PullingFile`` in `tests/Shared.Tests/WorkspaceUploadStructureTests.fs` applies `planStubOps` ops; collects `Op.SetDocumentState(_, _, PullingFile)` ids; asserts File PullingFile and Directory Current on the graph after local `Op.apply` only.
[ ] ``new file stubs start PullingFile; directory parent stays Current`` in `tests/Shared.Tests/WorkspaceUploadStructureTests.fs` applies `planStubOps`; asserts parent Current and file PullingFile on the graph (no Notice).
[ ] ``reused Unparsed directory with owned child becomes Current`` in `tests/Shared.Tests/WorkspaceUploadStructureTests.fs` applies `planStubOps`; asserts Directory moves to Current (exercises `markParentCurrent` path).
[ ] ``plan promotes ancestor directories when nested file stub added`` in `tests/Shared.Tests/WorkspaceUploadStructureTests.fs` applies `planStubOps`; asserts ancestor Directory nodes Current.
[ ] ``planServerFilePresentOps transitions only matching absent files`` in `tests/Shared.Tests/WorkspaceUploadStructureTests.fs` applies `planServerFilePresentOps`; asserts PullingFile file becomes Unparsed and sibling stays PullingFile.
[ ] ``upload posts NodeUploading beside SetDocumentState`` in `tests/Server.Tests/NodeEventExpandTests.fs` posts `planStubOps` ops through `Api.postEvents`; asserts Change contains PullingFile `SetDocumentState`, EventLog `Notice.NodeUploading`, and graph PullingFile via `assertBeside`.
[ ] ``incoming file posts NodeReceived beside SetDocumentState`` in `tests/Server.Tests/NodeEventExpandTests.fs` posts `planServerFilePresentOps` through `Api.postEvents`; asserts Unparsed `SetDocumentState`, logged `Notice.NodeReceived`, and graph Unparsed for the present file only.
[ ] ``upload emits NodeUploading`` and ``incoming file emits NodeReceived`` in `tests/Server.Tests/NodeEventExpandTests.fs` post the same planner ops; assert Notice on the host and graph document-state fields including persist axis (integration, not inside `WorkspaceUploadStructure.fs`).
[ ] ``documentRootsAffectedByGraphChange upload sibling does not dirty existing file root`` in `tests/Shared.Tests/DocumentPartitionTests.fs` folds `planStubOps` ops with `Op.apply`; checks `DocumentPartition.documentRootsAffectedByGraphChange` scope.
[ ] ``upload structure persistence preserves existing file mtime`` in `tests/Server.Tests/DocumentPersistenceTests.fs` folds `planStubOps` ops; asserts uploaded File stays PullingFile after `DocumentPersistChange.persistGraphChange`.
[ ] ``SetDocumentState includes the containing parent and filters unwritable root`` in `tests/Shared.Tests/DocumentOpImpactTests.fs` uses a standalone `Op.SetDocumentState` (not `WorkspaceUploadStructure`); checks document-root impact only.

## 4. Copy-from

[ ] `DocumentOpImpact.noticesBeside` in `src/Shared/DocumentOpImpact.fs` maps each `Op.SetDocumentState` in a Change to the equivalent Notice: any `_, _, PullingFile` → `Notice.NodeUploading`; `PullingFile, Unparsed` → `Notice.NodeReceived`; other transitions to `Unparsed` → `Notice.MarkUnparsed`; `Unparsed, Current` → `Notice.NodeParsed` only when no op in the batch targets `PullingFile` or `Unparsed`.
[ ] `Api.postEvents` in `src/Server/Api.fs` calls `DocumentOpImpact.noticesBeside` through private `inferredNodeEvents` and posts each Notice with `CoreMailboxApi.postNotice` (ticket Look at row for `src/Server/Api.fs` line 253).
[ ] `Api.applyParseFile` in `src/Server/Api.fs` posts `Notice.NodeParsed` beside a graph-only parse Change after `CoreDoor.postGraphOnly` (explicit post beside ops, not inference from `WorkspaceUploadStructure`).
[ ] `LazyLoadReconciliationServer.reconcileReceivedFiles` in `src/Server/LazyLoadReconciliationServer.fs` posts `Notice.NodeReceived` in a `beside` callback after reconcile Changes (ticket Look at row for line 360 uses `Op.SetDocumentState` in `receivedFileIds` / plan, not in `WorkspaceUploadStructure.fs`).
[ ] `ResidentProjection.applyDocumentNotice` in `src/Shared/ResidentProjection.fs` applies the same graph write as `Op.SetDocumentState` for document-state Notices (ticket Look at row for `src/Shared/ResidentProjection.fs` line 20 is the `SetDocumentState` op arm; Notice apply is the paired helper at line 42).

## 5. Implement

[ ] [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md) requires each listed `SetDocumentState` site in `WorkspaceUploadStructure.fs` to also send an equivalent Notice through Mailbox; the module today emits only `Op` lists and has no `MailboxHost`, `CoreChangesDoor`, or `postNotice` parameter.
[ ] Ticket Context requires each Notice to be idempotent and identical to what `Op.SetDocumentState` would do; private `markUnparsed`, `markPullingFile`, and `markParentCurrent` already return `[]` when the node is already in the target enum (planner idempotence); `GraphMutate.setDocumentState` and `Ev.applyNotice` leave state unchanged when the enum already matches.
[ ] Server accept path already posts inferred Notices for ops from `planStubOps` and `planServerFilePresentOps` when those ops arrive in a Change through `Api.postEvents` or graph-only `FileAgent.postGraphOnlyNotices`; that path uses `DocumentOpImpact.noticesBeside`, not code inside `WorkspaceUploadStructure.fs`.
[ ] Client upload path `UpdateWorkspaceSync.applyStructureLocally` applies stub ops with `ResidentProjection.applyOps` before HTTP POST; it does not call `CoreMailboxApi.postNotice` on the client (Mailbox is server-side on the `CoreChangesDoor.host` used by `Api.postEvents`).
[ ] A combined `planStubOps` Change often includes PullingFile or Unparsed targets; `DocumentOpImpact.noticesBeside` then suppresses `Notice.NodeParsed` for `markParentCurrent` `Unparsed` → `Current` ops in the same batch (`uploadOrArrival` guard in `src/Shared/DocumentOpImpact.fs`).
[ ] [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md) section 6 states no new test is required; default test set run still applies per edited file.
[ ] Section 6 names no new test; no named new test seam observation applies for this slice.
