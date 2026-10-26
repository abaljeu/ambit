# 45 — SetDocumentState use changes to

Each production `Op.SetDocumentState` becomes one `Notice` posted through `CoreMailboxApi.postNotice`, or one direct mailbox write. `Graph.setDocumentState` stays. It is the graph mutator. It is not the Op.

## Rule

[ ] [45 — SetDocumentState leaves the Op list](../issues/45-remove-document-state-set-ops.md) removes the case after the migrate blockers remove the production constructors.
[ ] [Document-state events](../spec.md) §2 item 10: `Op.SetDocumentState` is not an Op. No other Set op writes parse state, persist state, or PullingFile.
[ ] [Message shape](../cqrs-messages.md) §2 item 8 **Notice** names the Node notices. `Notice.SetDocumentState` is not a case. An Unpersisted mark is the open `Notice.NodeUnpersisted`.
[ ] `Op.NewSpecialNode` stays. It inserts the node as Unparsed.
[ ] A holder of the notice function posts a `Notice`. Today's function is `CoreMailboxApi.postNotice`.
[ ] [44 — Apply document state from Node events](../issues/44-apply-document-state-from-file-events.md) is done. `CoreMailboxLoad.applyNotice` applies `Notice.NodeUploading`, `Notice.NodeReceived`, and `Notice.NodeParsed`.

## Mapping

| Use | From | To | Owner | Today |
| --- | --- | --- | --- | --- |
| File stub upload | `WorkspaceUploadStructure.markPullingFile`: `Current` or `Unparsed` to `PullingFile` | `Notice.NodeUploading` | [50 — Upload emits NodeUploading](../issues/50-upload-emits-nodeuploading.md), then [57 — Upload Actor posts Node notices](../issues/57-upload-actor-posts-document-state.md) | Constructor remains. `Api.inferredNodeEvents` posts the notice. |
| Incoming file | `WorkspaceUploadStructure.planServerFilePresentOps`: `PullingFile` to `Unparsed` | `Notice.NodeReceived` | [53 — Arrival and reconcile emit NodeReceived](../issues/53-arrival-and-reconcile-emit-nodereceived.md), then 57 — Upload Actor posts Node notices | Constructor remains. Inference posts the notice. |
| Git received File | `LazyLoadReconciliationApply.markUnparsed` on a File: `Current` or `PullingFile` to `Unparsed` | `Notice.NodeReceived` | 53 — Arrival and reconcile emit NodeReceived | Constructor remains. `reconcileReceivedFiles` posts the notice beside the op. |
| Directory Unparsed | `WorkspaceUploadStructure.markUnparsed`, `LazyLoadReconciliation.unparsedStateOp`, and `markUnparsed` on a Directory | `Notice.NodeUnparsed` | [121 — MarkUnparsed renamed NodeUnparsed](../issues/121-markunparsed-renamed-nodeunparsed.md) | Constructors remain. The direct write passes `Notice.MarkUnparsed` into `writeParseState`. |
| Import, file parse, directory reconcile Parsed | `Unparsed` to `Current` in `ImportText.markDocumentCurrentBeforeParse`, `ImportDocument.planParseFile`, `DirectoryReconcile.markCurrent` | `Notice.NodeParsed` from `ParseThread.finishParse` | [54 — Parse and import stop the Parsed Set](../issues/54-parse-and-import-stop-the-parsed-set.md) | Constructors remain. `finishParse` and `Api.applyParseFile` already post the notice beside the op. |
| Parent and directory-artifact Parsed | `markParentCurrent` in `WorkspaceUploadStructure` and `LazyLoadReconciliationApply`; `parseDirInfoIfPresent` from `Unparsed` to `Current` | `Notice.NodeParsed` | 54 — Parse and import stop the Parsed Set | Constructors remain. 54 does not name these functions. |
| Directory artifact still PullingFile | `parseDirInfoIfPresent`: `PullingFile` to `Current` | The arm stops. `Notice.NodeReceived` clears PullingFile. `Notice.NodeParsed` then sets Parsed. | 53 — Arrival and reconcile emit NodeReceived, then 54 — Parse and import stop the Parsed Set | Constructor remains. |
| Ordinary Edit Unpersisted | No `Op.SetDocumentState`. `CoreEventDispatch.markForPersist` calls `GraphMutate.setUnpersistedAt` and tags `Notice.NodePersisting`. | `Notice.NodeUnpersisted` through `postNotice`. `setUnpersistedAt` stays. | [119 — Add Notice.NodeUnpersisted](../issues/119-add-notice-nodeunpersisted.md), then [120 — Ordinary edit posts NodeUnpersisted](../issues/120-ordinary-edit-posts-nodeunpersisted.md) | `Notice.NodeUnpersisted` is absent. The Edit still uses `Notice.NodePersisting`. |
| Undo and Redo Unpersisted | No `Op.SetDocumentState`. Same `setUnpersistedAt` mark. | Stays `GraphMutate.setUnpersistedAt`. The axis notice stays `Notice.NodePersisting`. | [47 — Undo and redo set Unpersisted](../issues/47-undo-and-redo-set-unpersisted.md), kept by 120 — Ordinary edit posts NodeUnpersisted | 47 — Undo and redo set Unpersisted is done. |
| Stored replay Unpersisted | No `Op.SetDocumentState` constructor. `EventLog.recover` applies stored ops. | Direct Unpersisted mark on the nearest owning special node. Replay skips the Node notices, including `Notice.NodeUnpersisted` when that case exists. | [55 — Stored replay sets Unpersisted](../issues/55-stored-replay-sets-unpersisted.md) | Status `defined`. `Ev.apply` of a `Notice` body has no ops. |
| Inferred notices | `Api.inferredNodeEvents` matches `Op.SetDocumentState` | The function leaves. The upload Actor posts `Notice.NodeUploading` and `Notice.NodeReceived`. `ParseThread.finishParse` posts `Notice.NodeParsed`. | 57 — Upload Actor posts Node notices | The match is still in `Api.postEvents`. |

## PullingFile

[ ] `WorkspaceUploadStructure.markPullingFile` builds `Op.SetDocumentState(nodeId, Current, PullingFile)` and `Op.SetDocumentState(nodeId, Unparsed, PullingFile)`.
[ ] `planStubOps` calls it for a new File stub. `NewSpecialNode` inserts that File as Unparsed, so the emitted arm is `Unparsed` to `PullingFile`.
[ ] Callers of `planStubOps`: `WorkspaceCloudUpload` and `UpdateWorkspaceSync`.
[ ] The replacement notice is `Notice.NodeUploading` of that node id.
[ ] 50 — Upload emits NodeUploading is coded. Until the upload Actor exists, `Api.postEvents` posts that notice.
[ ] 57 — Upload Actor posts Node notices is `defined`. The Actor posts `Notice.NodeUploading`. `markPullingFile` does not post.
[ ] The handler in `CoreMailboxLoad.writePullingFileUnpersisted` sets PullingFile in memory and Unpersisted. The `document_state` column stores Parsed or Unparsed only.

## Received File

[ ] `WorkspaceUploadStructure.planServerFilePresentOps` builds `Op.SetDocumentState(fileId, PullingFile, Unparsed)` when the File is PullingFile.
[ ] Callers: `WorkspaceCloudUpload` and `UpdateWorkspaceSync`.
[ ] The replacement notice is `Notice.NodeReceived` of that file id.
[ ] 53 — Arrival and reconcile emit NodeReceived is coded for the post. Until the upload Actor exists, `Api.postEvents` posts that notice from the op.
[ ] 57 — Upload Actor posts Node notices makes the Actor the poster. The planner does not post.
[ ] The handler `writeReceived` calls `GraphMutate.setParseState` with Unparsed and `GraphMutate.setPersistState` with Persisted. `Node.withParseState` clears PullingFile.
[ ] `LazyLoadReconciliationApply.markUnparsed` builds `Op.SetDocumentState(nodeId, Current, Unparsed)` and `Op.SetDocumentState(nodeId, PullingFile, Unparsed)`.
[ ] File callers of that function: `invalidateOrParseModified` for a modified path, `LazyLoadReconciliationReport.renamedFileUnparsedOps` for a renamed File, and `markAddedDocumentsUnparsed` for an added PullingFile stub.
[ ] `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `Notice.NodeReceived` beside those ops. `receivedFileIds` selects a Modified File that is Current or PullingFile, an Added PullingFile, and a Renamed File the plan marks Unparsed.
[ ] The File constructor leaves. The `Notice.NodeReceived` post stays.

## Directory Unparsed

[ ] `WorkspaceUploadStructure.markUnparsed` contains the same two Unparsed constructors for a Directory stub inside `planStubOps`.
[ ] `NewSpecialNode` inserts a new Directory as Unparsed. That call emits an empty list when the node is already Unparsed. The `Current` and `PullingFile` arms still build the op.
[ ] `LazyLoadReconciliation.unparsedStateOp` builds `Op.SetDocumentState` from the node's current `DocumentStateEnum` to `Unparsed`.
[ ] `containingDirectoryMarkOps` maps that op onto the containing Directory of an added, removed, or renamed file. `planReceivedFiles` appends those ops.
[ ] `markAddedDocumentsUnparsed` uses `markUnparsed` for an added Directory stub.
[ ] `Api.inferredNodeEvents` maps `PullingFile` to `Unparsed` onto `Notice.NodeReceived`. It has no arm for `Current` to `Unparsed`.
[ ] The replacement notice is `Notice.NodeUnparsed`. `markUnparsedThenPush` writes that notice, then pushes the node for parsing.
[ ] 121 — MarkUnparsed renamed NodeUnparsed renames `Notice.MarkUnparsed` to `Notice.NodeUnparsed`. The direct write stays. Today the write gives `Notice.MarkUnparsed` to `writeParseState`. After that ticket, the same write gives `Notice.NodeUnparsed`.

## Parsed

[ ] `Current` is the stored word for Parsed.
[ ] `ImportText.markDocumentCurrentBeforeParse` builds `Op.SetDocumentState(focusId, Unparsed, Current)` when the focus is an Unparsed document root.
[ ] `ImportText.buildImportChange` and `ImportText.buildDirectoryMergeChange` put that op on the Change.
[ ] `ImportDocument.planParseFile` builds `Op.SetDocumentState(fileId, Unparsed, Current)` when the File is Unparsed.
[ ] `DocumentPersistWrite.planParseFile` calls it. `ParseThread.parseFile` and `Api.applyParseFile` run that plan.
[ ] `DirectoryReconcile.markCurrent` builds `Op.SetDocumentState(directoryId, Unparsed, Current)` when the directory is Unparsed.
[ ] `DirectoryReconcile.planDirectoryReconcile` emits it. `ParseThread.parseDirectory` runs that plan.
[ ] The replacement is `Notice.NodeParsed` posted by `ParseThread.finishParse`.
[ ] 54 — Parse and import stop the Parsed Set is `defined`. Those three functions stop the op. Import and `Api.postParseFile` move onto the parse thread so `finishParse` is the poster.
[ ] `ParseThread.finishParse` already posts `Notice.NodeParsed`.
[ ] `Api.applyParseFile` already posts `Notice.NodeParsed` beside the op.
[ ] The handler `applyNodeParsed` calls `GraphMutate.setParseState` with Parsed when `pullingFile` is clear. PullingFile stays PullingFile.
[ ] `WorkspaceUploadStructure.markParentCurrent` builds `Op.SetDocumentState(nodeId, Unparsed, Current)` for an Unparsed Directory Node or Workspace Node.
[ ] `planStubOps` emits it for a parent of a new stub and for an Unparsed directory that already has owned members.
[ ] `LazyLoadReconciliationApply.markParentCurrent` builds the same op. `markAddedDocumentsUnparsed` emits it.
[ ] `LazyLoadReconciliationApply.parseDirInfoIfPresent` builds `Op.SetDocumentState(nodeId, Unparsed, Current)` when the directory artifact node is Unparsed.
[ ] 54 — Parse and import stop the Parsed Set names three producers. These three functions are further producers of `Unparsed` to `Current`. The replacement is the same `Notice.NodeParsed`.
[ ] `parseDirInfoIfPresent` also builds `Op.SetDocumentState` from `PullingFile` to `Current`.
[ ] That arm stops. `Notice.NodeParsed` leaves PullingFile as PullingFile. `Notice.NodeReceived` sets Unparsed and clears PullingFile. A later `Notice.NodeParsed` sets Parsed.

## Unpersisted

[ ] `Op.SetDocumentState` carries `DocumentStateEnum`: `Current`, `Unparsed`, or `PullingFile`.
[ ] No production constructor of that op writes Unpersisted.
[ ] An ordinary Edit is `Order.Change` with `ChangeOrigin.Edit`. `CoreEventDispatch.markForPersist` calls `GraphMutate.setUnpersistedAt` at the Edit event id.
[ ] That write's axis notice today is `Notice.NodePersisting`. `writeAxis` stores the graph. It does not call `postNotice`.
[ ] 119 — Add Notice.NodeUnpersisted adds `Notice.NodeUnpersisted of nodeId`. The `applyNotice` arm matches `Notice.NodePersisting`: `GraphMutate.setPersistState` with Unpersisted. The case is absent from `Notice` today.
[ ] 120 — Ordinary edit posts NodeUnpersisted posts `Notice.NodeUnpersisted` through `postNotice` after a successful ordinary Edit. `setUnpersistedAt` stays for that Edit. That ticket leaves `Op.SetDocumentState` in `Op`.
[ ] 47 — Undo and redo set Unpersisted is done. Undo and Redo call the same `setUnpersistedAt` path. 120 — Ordinary edit posts NodeUnpersisted keeps `Notice.NodePersisting` for Undo and Redo.
[ ] The browser mark is `SyncLogic.markUndoRedoVersion`. It calls `GraphMutate.setUnpersistedAt`.
[ ] 55 — Stored replay sets Unpersisted is `defined`. `EventLog.recover` and `BootCache.foldLog` mark the nearest owning special node Unpersisted by a direct write.
[ ] Replay skips `Notice.NodeUploading`, `Notice.NodeReceived`, `Notice.NodeParsing`, `Notice.NodeParsed`, `Notice.NodePersisting`, and `Notice.NodePersisted`.
[ ] Message shape decision 14 **Stored replay**: when `Notice.NodeUnpersisted` exists, replay skips that notice too.
[ ] Replay posts no notice.

## Inference

[ ] `Api.inferredNodeEvents` reads `Op.SetDocumentState` on `EventBody.Change`.
[ ] New state `PullingFile` becomes `Notice.NodeUploading`.
[ ] Old state `PullingFile` and new state `Unparsed` becomes `Notice.NodeReceived`.
[ ] Old state `Unparsed` and new state `Current` becomes `Notice.NodeParsed` only when that Change has no op whose new state is `PullingFile` or `Unparsed`.
[ ] `Api.postEvents` posts the list through `CoreMailboxApi.postNotice`.
[ ] 57 — Upload Actor posts Node notices removes `inferredNodeEvents`.

## What stays

[ ] `GraphOps.Graph.setDocumentState` calls `GraphMutate.setDocumentState`.
[ ] `Op.apply` and the undo arm are the only callers. Those arms leave with the Op case. The mutator stays.
[ ] `CoreMailboxLoad.applyNotice` writes with `GraphMutate.setParseState`, `GraphMutate.setPersistState`, and `Node.withDocumentState`. Those writes are not ops.
[ ] `markUnparsedThenPush` writes Unparsed on the mailbox loop. That write is not an op.

## Match sites that leave with the case

[ ] 45 — SetDocumentState leaves the Op list removes these arms. They are not a second state writer.
[ ] `History.fs`: the `Op` case, `involvedNodeIds`, `applyAllowed`, `invert`, `undoAllowed`, and `ChangeValidation.opChangesGraphShape`.
[ ] `Serialization.encodeOp` and `Serialization.decodeOp`. After the case leaves, the type string `SetDocumentState` fails decode.
[ ] `ResidentProjection.applyOpAllowing`.
[ ] `DatabaseProjection.nodeIdFromOp`.
[ ] `DocumentOpImpact.opAnchor` and `DocumentOpImpact.touchedNodeIds`.
[ ] `DocumentColdParse.targetsDocumentRoot`.
[ ] `LazyLoadReconciliationServer.receivedFileIds` local `planMarksUnparsed`.

## Tests that still construct the op

[ ] 45 — SetDocumentState leaves the Op list removes `Op.SetDocumentState round-trip` from `SerializationTests.fs`.
[ ] That ticket removes `SetDocumentState includes the containing parent and filters unwritable root` from `DocumentOpImpactTests.fs`.
[ ] That ticket adds `SetDocumentState leaves the Op list`.
[ ] These test files still construct or match `Op.SetDocumentState`: `SpecialNodeStateAxesTests.fs`, `HistoryTests.fs`, `SerializationTests.fs`, `DocumentOpImpactTests.fs`, `WorkspaceUploadStructureTests.fs`, `ImportTextTests.fs`, `ImportDocumentTests.fs`, `DirectoryReconcileTests.fs`, `LazyLoadReconciliationTests.fs`, `ClientHistoryTests.fs`, `ViewModelJoinOpsTests.fs`, `NodeEventExpandTests.fs`, `StateEndpointTests.fs`, `DatabaseProjectionTests.fs`, `DatabaseProjectionContractTests.fs`, `LazyLoadReconciliationServerTests.fs`, `ParseThreadLoadTests.fs`, `LoadSaveCommandTests.fs`, `GithubTransportLoadPullTests.fs`, and `GithubTransportActorTests.fs`.
