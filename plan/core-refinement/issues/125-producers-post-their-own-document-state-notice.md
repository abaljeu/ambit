# 125 — Producers post their own document-state Notice

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

[122 — Document state moves by Notice](122-document-state-moves-by-notice.md) covered most producers only through the op. `DocumentOpImpact.noticesBeside` reads `Op.SetDocumentState` after the Change is accepted and returns the Notice. Its callers are `Api.inferredNodeEvents`, `FileAgent.postGraphOnlyNotices`, and `DatabaseProjection.noticesBeside` through `DbAgent.commitPostChange`. `LazyLoadReconciliationServer.postParsedFromPulling` (`parsedFromPulling`) and `receivedFileIds` (`planMarksUnparsed`) also read the op. So when a pass in [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md) removes the op, the Notice goes with it. The leading op also did a second job: it took an Unparsed root to Current before that Change's content ops ran. Without it, `History.blockedForApply` and `ChangeAmendment.applyForGraphOnly` refuse them with "operation cannot modify an unparsed document". Two transitions had no Notice at all. `noticesBeside` drops NodeParsed when the Change also targets PullingFile or Unparsed (`uploadOrArrival`). That means the parent `Unparsed → Current` in an upload or reconcile Change (passes 7 and 9) is not covered today.

## 4. The change

Each producer emits the Notice it covers, named at the producer and not read back from an op. The producer puts the Notice in its own result next to its ops, in apply order. The result type is the existing `EventBody`. A planner that returns `Op list` today returns `EventBody list`. That list is the lead Notices, then `EventBody.Change ops`, then the trail Notices. No new type is added.

- **Lead.** A `Notice.NodeParsed` that unlocks content ops in the same plan goes before the Change. The ops then run on a Current root, which is the job the leading op did.
- **Trail.** Every other Notice goes after the Change, in planner order. Two reasons: a new stub does not exist before its `Op.NewSpecialNode`, and a container's `MarkUnparsed` has to land after `markParentCurrent`.

Posting follows the list order.

- **Server producers** (parse thread, `Api.applyParseFile`, and reconcile) post each Notice through `CoreMailboxApi.postNotice`. They post each Change through the door they already use (`postGraphOnly` or `GraphOnlyChangePost.postChunks`), and wait for its reply before the next post. Passes 2 and 3 already do this, and it works.
- **Client producers** (upload and import) send the list as the request's events. `Api.postEvents` posts the leading Notices through `postNotice` before `CoreDoor.postEvents`, and the trailing Notices after `Ok`. It accepts only a Notice for which `Ev.documentStateFromNotice` is `Some`, and rejects every other Notice. `CoreEventDispatch.completeAction` is unchanged: no client Notice reaches the agents, which avoids the problem the review found in #401.
- **Planner folds** (`WorkspaceUploadStructure.foldStateOps`, `LazyLoadReconciliationApply.foldDocumentStateOps`, and `applyOps`) and the shared facts apply each body with `Ev.apply`, which already handles `EventBody.Notice`. After the op is removed, the Notice does the planning write too.

Why this placement is the simplest:

- The mailbox is FIFO, and every server door already posts Notices through `postNotice`.
- `Ev.apply` already applies a Notice.
- Every `Op.SetDocumentState` in these producers is either the first op for its node (`markCurrent @ parseOps`) or appended after the plan (`createOps @ stateOps`, and the file plan followed by `containingDirectoryMarkOps`). Before and after are the only two positions needed.

### Deleted

- `DocumentOpImpact.noticesBeside` and `documentStateTransition`
- `Api.inferredNodeEvents`
- `FileAgent.postGraphOnlyNotices`
- `DatabaseProjection.noticesBeside` and its call in `DbAgent.commitPostChange`
- `LazyLoadReconciliationServer.parsedFromPulling` and `postParsedFromPulling`. In `receivedFileIds`, `planMarksUnparsed` gives way: a renamed File is taken from the old path's File, as the Modified case does.
- The import preview: `ImportText.importCommand`, `tryImportFocus`, `stateForImportOps`, `commitImportApply`, and `Api.importParsedNotices`, plus their wiring in `FileAgent`, `DbAgent`, `BootCache`, `EventLog`, and `SyncLogic`. Import emits a lead `Notice.NodeParsed` instead.

### Accepted tradeoff

A lead Notice is applied as its own mailbox step. If the Change after it is rejected, the root stays Current or Parsed with the old content. `ParseThread.alreadyParsed` then does not retry it.

## 5. After this change, each hard pass is "just remove the line"

| Pass | Producer | Notice it emits | Place | Pass then |
|---|---|---|---|---|
| 1 | `ImportText` (Unparsed import root) | `NodeParsed root` | lead | Nothing left to remove. Staging already has no op here. Pass 1 is the preview deletion above. |
| 2 | `ImportDocument.planParseFile` `markCurrent` | `NodeParsed fileId` | lead | Delete the op. The 8 `ImportDocumentTests` facts fold `Ev.apply` and pass. |
| 4 | `WorkspaceUploadStructure.planServerFilePresentOps` | `NodeReceived fileId` | trail | Delete the op. The list is only Notices, and the Change is omitted, not posted empty. |
| 5 | `WorkspaceUploadStructure.markPullingFile` | `NodeUploading fileId` | trail | Delete both arms. |
| 7 | `WorkspaceUploadStructure.markParentCurrent` (and `promoteUnparsedDirsWithMembers`) | `NodeParsed dirId` | trail | Delete the op. |
| 8 | `LazyLoadReconciliationApply.markUnparsed` | `Current → MarkUnparsed`, `PullingFile → NodeReceived` | trail | Delete both arms. Directory reconcile (`beside = ignore`) is covered by the producer Notice. |
| 9 | `LazyLoadReconciliationApply.markParentCurrent` | `NodeParsed dirId` | trail | Delete the op. |
| 10 | `LazyLoadReconciliationApply.parseDirInfoIfPresent` `markCurrent` | `NodeParsed nodeId` (from Unparsed or PullingFile) | lead | Delete both arms. |
| 11 | `LazyLoadReconciliation.unparsedStateOp` | `MarkUnparsed dirId` | trail, after pass 9's Notice | Delete the op. |

Each pass still updates the tests that assert its op, as the pass rules in 124 say.

## 6. Size

The whole change is about 18 source files, so it splits into five parts. In parts a through d, the Notice is posted beside the op that still exists, and a second post is idempotent (124 §5), so those parts land in any order. Part e lands last.

| Part | Source files | Test files |
|---|---|---|
| a. Parse file | `ImportDocument.fs`, `DocumentPersistWrite.fs`, `ParseThread.fs`, `Api.fs` (`applyParseFile`) | `ImportDocumentTests`, `NodeEventExpandTests` |
| b. Reconcile | `LazyLoadReconciliationApply.fs`, `LazyLoadReconciliation.fs`, `LazyLoadReconciliationReport.fs` (`Report.ops`), `LazyLoadReconciliationServer.fs` | `LazyLoadReconciliationTests`, `LazyLoadReconciliationServerTests`, `NodeEventExpandTests` |
| c. Upload | `WorkspaceUploadStructure.fs`, `UpdateWorkspaceSync.fs`, `WorkspaceCloudUpload.fs`, `AmbitSession.fs` (`postOps`) | `WorkspaceUploadStructureTests`, `NodeEventExpandTests` |
| d. Client door and import | `Api.fs` (`postEvents`), `ImportText.fs`, `FileAgent.fs`, `DbAgent.fs`, `BootCache.fs`, `EventLog.fs`, `SyncLogic.fs` | `ImportTextTests`, `ImportDocumentTests`, `SpecialNodeTestHelpers`, `NodeEventExpandTests` |
| e. Delete the op readers | `DocumentOpImpact.fs`, `Api.fs`, `FileAgent.fs`, `DatabaseProjection.fs`, `DbAgent.fs`, `LazyLoadReconciliationServer.fs` | `NodeEventExpandTests` |

Each part stays within 8 source files and 4 test files. The individual facts edited inside those files may go over 4, because each fold-over-ops edit is mechanical.

## 7. Open questions

1. **Client Notices.** Should `Api.postEvents` take state Notices from any logged-in caller for any node? Or only for a node named by that request's Change, or for a node that already exists?
2. **NodeParsed for a parent.** `applyNodeParsed` also calls `pushParsedUnpersisted` and `noteParsed`. Is that right for an upload or reconcile parent that goes `Unparsed → Current` without a parse (passes 7 and 9)? Today `uploadOrArrival` hides it.
3. **121.** [121 — MarkUnparsed leaves Notice](121-markunparsed-leaves-notice.md) removes `Notice.MarkUnparsed`, which passes 8 and 11 use. If 121 lands first, which Notice covers `Current → Unparsed`?
4. **Pass 10 lead.** A `parseDirInfoIfPresent` node created earlier in the same reconcile plan cannot take a lead Notice, because it does not exist yet. Should it trail, so the Change keeps the op until a later Change?
5. **Pass 4 reply.** When a request is only Notices, what does `Api.postEvents` reply? The current event id with no new events?
6. **No caller for import.** On staging, `ImportText.buildImportChange` and `buildDirectoryMergeChange` have no `src` caller. Only tests call them. Is pass 1 live code?

## 8. Done

[ ] Parts a through e land.
[ ] A repo search under `src` finds no `Op.SetDocumentState` match outside apply, invert, serialize, `ResidentProjection`, `DatabaseProjection.nodeIdFromOp`, `DocumentOpImpact.opAnchor` and `touchedNodeIds`, and `DocumentColdParse`.
[ ] Run `scripts/agent-test.sh` with no arguments, then the server filters for `NodeEventExpandTests` and `LazyLoadReconciliationServerTests`.
[ ] All executed tests pass.
