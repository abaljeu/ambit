# 122 — Document state moves by Notice — resident projection

Ticket: [122 — Document state moves by Notice](plan/core-refinement/issues/122-document-state-moves-by-notice.md).

## 1. Direction

The `Op.SetDocumentState` arm applies an Op that arrived in an Ev from the Mailbox.

The same arm also applies an Op inside a Change that the client then posts to the Mailbox. That post already carries a matching Notice from the server accept path. The missing support was on the received path.

## 2. Received call chain

Each step is file then function. The Op enters `applyOpAllowing` only after the Ev is already built.

- `src/Client/Update.fs` `update` on `SysMsg PollDone`
- `src/Shared/SyncLogic.fs` `applySyncResponse`, or `consumeCatchUpPoll` when a catch-up baseline is present
- `src/Shared/SyncLogic.fs` `foldProjectedEvents`
- `src/Shared/ResidentProjection.fs` `applyOpsForSyncAllowing`
- `src/Shared/ResidentProjection.fs` `applyOpForSyncAllowing`
- `src/Shared/ResidentProjection.fs` `applyOpAllowing`

Load uses the same fold:

- `src/Client/Update.fs` `update` on `SysMsg LoadDone`
- `src/Shared/SyncLogic.fs` `applyLoadResponse`
- `src/Shared/SyncLogic.fs` `applySyncResponse`
- `src/Shared/SyncLogic.fs` `foldProjectedEvents`
- `src/Shared/ResidentProjection.fs` `applyOpsForSyncAllowing`
- `src/Shared/ResidentProjection.fs` `applyOpForSyncAllowing`
- `src/Shared/ResidentProjection.fs` `applyOpAllowing`

Boot novel events use the same fold:

- `src/Client/Program.fs` `applyBootNovel`
- `src/Shared/SyncLogic.fs` `applySyncResponse`
- `src/Shared/SyncLogic.fs` `foldProjectedEvents`
- `src/Shared/ResidentProjection.fs` `applyOpsForSyncAllowing`
- `src/Shared/ResidentProjection.fs` `applyOpForSyncAllowing`
- `src/Shared/ResidentProjection.fs` `applyOpAllowing`

An actor command tail uses the same fold:

- `src/Client/UpdateActorLive.fs` `applyCommandEvents`
- `src/Shared/SyncLogic.fs` `applyServerTail`
- `src/Shared/SyncLogic.fs` `applySyncResponse`
- `src/Shared/SyncLogic.fs` `foldProjectedEvents`
- `src/Shared/ResidentProjection.fs` `applyOpsForSyncAllowing`
- `src/Shared/ResidentProjection.fs` `applyOpForSyncAllowing`
- `src/Shared/ResidentProjection.fs` `applyOpAllowing`

Boot-cache replay of a stored Ev tail uses the resident apply directly. `decideBootRead` has no Client caller in this tree. Tests call it.

- `src/Shared/BootCache.fs` `decideBootRead`
- `src/Shared/BootCache.fs` `foldLog`
- `src/Shared/BootCache.fs` `applyLoggedEvent`
- `src/Shared/ResidentProjection.fs` `applyOpsAllowing`
- `src/Shared/ResidentProjection.fs` `applyOpAllowing`

These callers reach the same arm with a local pending Op or a confirmation suffix. The suffix is `Op.SetUpdateTime`.

- `src/Shared/SyncLogic.fs` `undoPendingGraph` → `ResidentProjection.applyOps`
- `src/Shared/SyncLogic.fs` `reapplyLeftoverPending` → `applyPendingEvent` → `ResidentProjection.applyOps`
- `src/Shared/SyncLogic.fs` `reconcileAck` → `projectSuffixes` → `ResidentProjection.applyOps`
- `src/Shared/ResidentProjection.fs` `applyOpsAllowing`
- `src/Shared/ResidentProjection.fs` `applyOpAllowing`

## 3. Posted call chain

A client Change that contains `Op.SetDocumentState` is applied, then posted.

- `src/Shared/WorkspaceUploadStructure.fs` `planServerFilePresentOps`, `planStubOps` (through `markUnparsed`, `markPullingFile`, `markParentCurrent`)
- `src/Client/UpdateWorkspaceSync.fs` `markServerFilesPresent` or `createWorkspaceOnServer` → `applyAndPostSync`
- `src/Client/UpdateWorkspaceSync.fs` `completeUploadInventoryWith` → `applyStructureLocally`
- `src/Shared/SyncLogic.fs` `applyLocalEvent`
- `src/Shared/ResidentProjection.fs` `applyOps`
- `src/Shared/ResidentProjection.fs` `applyOpsAllowing`
- `src/Shared/ResidentProjection.fs` `applyOpAllowing`
- `src/Client/UpdateWorkspaceSync.fs` `applyAndPostSync` posts `/{file}/changes`, or `src/Client/App.fs` handles `ContinuePostUploadStructure` and posts `/{file}/changes`
- `src/Server/Api.fs` `postEvents`
- `src/Server/Api.fs` `inferredNodeEvents`
- `src/Shared/DocumentOpImpact.fs` `noticesBeside`
- `src/Server/Core/CoreMailboxApi.fs` `postNotice`

`src/Server/Core/FileAgent.fs` `postGraphOnlyNotices` posts that same Notice list for a graph-only Change.

Undo and redo of a local Change use the arm, then `SyncPlanner.enqueuePending` posts the inverse Ev:

- `src/Client/UpdateOps.fs` undo and redo handlers, and `src/Client/UpdateWorkspaceSync.fs` undo handler
- `src/Shared/SyncLogic.fs` `applyLocalUndo` or `applyLocalRedo`
- `src/Shared/SyncLogic.fs` `applyInverse`
- `src/Shared/ResidentProjection.fs` `applyOpsAllowing`
- `src/Shared/ResidentProjection.fs` `applyOpAllowing`

`src/Client/UpdateHelpers.fs` `applyAndPost` uses `applyLocalEvent` for other commands. Those command builders leave `Op.SetDocumentState` to the workspace paths above.

## 4. Matching Notice on the received path

Before this change, `SyncLogic.foldProjectedEvents` sent `EventBody.Notice` to `applyLoggedNotice`.

- `Notice.NodePersisted` wrote persist fields in `applyNodePersisted`.
- Every other Notice advanced the event id and left the document-state enum as it stood.
- `BootCache.foldLog` took ops from `Ev.ops`. A Notice has no ops, so the enum stayed as it stood.
- `History.Ev.apply` already applies a Notice on the full Graph through `applyNotice` and `documentStateFromNotice`.

The resident rule for `Op.SetDocumentState` is: a Node present in the resident Graph is written; an absent Node stays unchanged.

The matching Notice uses that rule. `Ev.documentStateFromNotice` is the shared map:

- `Notice.NodeUploading` writes `PullingFile`
- `Notice.NodeReceived` and `Notice.MarkUnparsed` write `Unparsed`
- `Notice.NodeParsed` writes `Current`
- `Notice.NodeParsing`, `Notice.NodePersisting`, `Notice.PersistFailed`, and `Notice.NodePersisted` leave the enum unchanged

`Graph.setDocumentState` writes the new enum and ignores the old enum. A second apply of the same enum returns the same Graph. A Change that also contains `Op.SetDocumentState` and a Notice for that enum therefore land on one enum.

`Notice.NodePersisted` still writes persist fields in `SyncLogic.applyNodePersisted` after the enum step. `BootCache.applyLoggedEvent` writes only the enum. Persist fields for `Notice.NodePersisted` stay on the SyncLogic path.

## 5. Matching Notice on the posted path

The posted Change reaches `Api.postEvents`. `inferredNodeEvents` builds `DocumentOpImpact.noticesBeside` and `CoreMailboxApi.postNotice` places each Notice on the Mailbox. `FileAgent.postGraphOnlyNotices` does that for a graph-only Change. `Op.SetDocumentState` stays in the Change.

## 6. Change

- `src/Shared/History.fs`: `Ev.documentStateFromNotice` is `internal`, so the resident apply uses the same map as `Ev.applyNotice`.
- `src/Shared/ResidentProjection.fs`: `applyDocumentNotice` writes that enum when the Node is resident. An absent Node stays unchanged. `Op.SetDocumentState` stays.
- `src/Shared/SyncLogic.fs`: `applyLoggedNotice` applies `applyDocumentNotice`, then the existing `Notice.NodePersisted` persist write.
- `src/Shared/BootCache.fs`: `applyLoggedEvent` applies `applyDocumentNotice` for a Notice and keeps `applyOpsAllowing` for every other Ev.

## 7. Tests

`scripts/agent-test.sh` mapped the edited source to `BootCacheTests`, `HistoryTests`, and `SyncLogicTests`.

Passed: 141. Failed: 0. Skipped: 0.

`ResidentProjection.fs` has no stem file `ResidentProjectionTests`. The module tests are `ResidentProjectionApplyTests`.

`scripts/agent-test.sh --filter FullyQualifiedName~ResidentProjectionApplyTests shared`

Passed: 9. Failed: 0. Skipped: 0.

Ticket checkboxes stay as they were.
