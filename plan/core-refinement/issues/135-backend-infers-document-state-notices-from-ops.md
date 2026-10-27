# 135 — Backend infers document-state Notices from ops

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

`Op.SetDocumentState` is redundant. The other ops in a Change already imply the state, and the backend should post the Notice. Today the backend reads the Notice back from the op instead:

- `DocumentOpImpact.noticesBeside`, called from `Api.inferredNodeEvents`, `FileAgent.postGraphOnlyNotices`, and `DatabaseProjection.noticesBeside` (from `DbAgent.commitPostChange`)
- `LazyLoadReconciliationServer.parsedFromPulling`

The leading op also gets content ops past `History.blockedForApply` and `ChangeAmendment.applyForGraphOnly` on an Unparsed root.

## 4. Change

One backend place infers. Tickets 125–133 each add their transition's rule to it.

- `DocumentOpImpact.noticesBeside` becomes `inferNotices`. It takes the graph before the Change, the event's command name, and the ops, and returns the Notices in apply order. Its `Op.SetDocumentState` arms stay until [45 — SetDocumentState leaves; NodeUnpersisted replaces it](45-remove-document-state-set-ops.md) removes the op case.
- **Lead.** In `ChangeAmendment.applyPosted`, which `FileAgent` and `DbAgent` both use, an inferred `NodeParsed` is applied with `Ev.apply` before the ops. This is the general form of the job `ImportText.commitImportApply` does for import today.
- **Trail.** The agents post every inferred Notice after commit through `persistFinished`, for Edit and for graph-only Changes alike. `Api.inferredNodeEvents` and `DatabaseProjection.noticesBeside` leave.
- No client posts a Notice, and no producer emits one. No new type is added.

Accepted tradeoff: a lead write is applied in the same apply step as the ops. If the Change is rejected, the state goes with it. A rejected Change no longer leaves the root Current.

Source: `DocumentOpImpact.fs`, `ChangeAmendment.fs`, `FileAgent.fs`, `DbAgent.fs`, `DatabaseProjection.fs`, `Api.fs`.

## 5. Tests

[ ] `DocumentOpImpactTests`
[ ] `NodeEventExpandTests`: the `… beside SetDocumentState` facts still pass through the op arms

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.

## 7. Use sites and replacing Notices

Mapped from the code, read statically. Notices are applied by `Ev.documentStateFromNotice`: `NodeUploading` writes PullingFile, `NodeReceived` and `MarkUnparsed` write Unparsed, and `NodeParsed` writes Current. "Lead" means the Notice must apply before the ops, because the op unlocks them past `History.blockedForApply`. "Trail" means it can post after commit.

### Production construction sites

| # | Site | Transition and condition | What it achieves | Round / ticket | Replacing Notice | Inference source | Ease |
| --- | --- | --- | --- | --- | --- | --- | --- |
| P1 | `ImportText.fs:47` `markDocumentCurrentBeforeParse`, prepended in `buildImportChange` and `buildDirectoryMergeChange` | `Unparsed → Current` on the focus, when it is an Unparsed document root | Lets the import's `Replace` and paste ops through the Edit guard (`guardsUnparsedDocument` is true). `noticesBeside` posts `NodeParsed` from it through `Api.inferredNodeEvents`. | Round 1 / [125](125-import-drops-setdocumentstate.md) | `NodeParsed focusId`, lead | `inferNotices` on the Edit path (`Api.postEvents`): a `Replace` that attaches owned children to an Unparsed document root in the pre-Change graph | Hard. The ops match an ordinary edit under an Unparsed root, which the guard exists to refuse, and the command name is empty. Open: no `src` caller (125 §6). |
| P2 | `ImportDocument.fs:130` `planParseFile` `markCurrent`, via `DocumentPersistWrite.planParseFile` from `ParseThread.parseFile` and `Api.applyParseFile` | `Unparsed → Current` on a File that is Unparsed. Current adds nothing, and PullingFile is refused earlier. | Lets the parse ops into the Unparsed File root. Under `Parse`, `blockedForApply` exempts only document-root suffix attaches. `finishParse` and `applyParseFile` already post a trail `NodeParsed`. | Round 2 / [126](126-planparsefile-drops-setdocumentstate.md) | `NodeParsed fileId`, lead | `inferNotices`: a graph-only `Parse` Change whose ops land inside an Unparsed File root (`owningSpecials` on the pre-Change graph) | Easy when there are parse ops. Hard for a blank file: there are no ops, so only the `finishParse` trail covers it. |
| P3 | `WorkspaceUploadStructure.fs:149,151` `markPullingFile`, in `markNewStubsUnparsed` from `planStubOps` | `Current → PullingFile` or `Unparsed → PullingFile` on a new File stub. Targets come from `NewSpecialNode File` in the plan, which inserts the stub Unparsed, so the Current arm looks unreachable from this caller. | Marks the body as not yet on the server, so `planParseFile` refuses it ("no file on server"). The `NodeUploading` mailbox handler also sets Unpersisted. The Change is built by the client (`UpdateWorkspaceSync`, `WorkspaceCloudUpload`) and posted as `displayName Load` to `POST /changes`. | Round 5 / [128](128-upload-stub-drops-setdocumentstate.md) | `NodeUploading id`, trail | `inferNotices` on the Edit path: `NewSpecialNode(id, File, _)` under the `Load` command name | Hard. The ops are the same as create-file (`FileNodeOps.planCreateOwnedFile`), and only the command name differs. The client's local apply also loses the mark until the Notice syncs back. |
| P4 | `WorkspaceUploadStructure.fs:158` `markParentCurrent`, from `markNewStubsUnparsed` and `promoteUnparsedDirsWithMembers` | `Unparsed → Current` on a Directory or Workspace that is Unparsed and now has owned members | Marks the shell's members as known. No Notice covers it today: `noticesBeside` drops `NodeParsed` when any op in the Change targets PullingFile or Unparsed (`uploadOrArrival`), so the backend gets it only through `Op.apply`. | Round 7 / [129](129-upload-parent-drops-setdocumentstate.md) | `NodeParsed dirId`, trail. It fits the enum. Open: the mailbox side effects (`pushParsedUnpersisted`, `noteParsed`), see 129 §6. | `inferNotices`: a `Replace` that attaches owned stubs to an Unparsed Directory or Workspace under `Load` | Hard. The promote step scans the whole graph, so a promoted directory may have no op in the Change. A received-file reconcile uses the same `Replace` and ends Unparsed (P9). |
| P5 | `WorkspaceUploadStructure.fs:317` `planServerFilePresentOps`, from client `markServerFilesPresent` and `WorkspaceCloudUpload.markBodiesPresent` | `PullingFile → Unparsed` on a File that is PullingFile, after a PUT or an mtime skip | Makes the File parseable. `NodeReceived` also sets Persisted. Today it arrives through `Api.inferredNodeEvents`. | Round 4 / [127](127-server-file-present-drops-setdocumentstate.md) | `NodeReceived fileId` | None from ops: this op is the whole Change. The only backend point is `WorkspaceWebDav.handlePut`, which has no core handle. An mtime skip sends no request at all. | Hard. This can't be inferred in `DocumentOpImpact`. |
| P6 | `LazyLoadReconciliationApply.fs:23,25` `markUnparsed`, from `invalidateOrParseModified` (modified path), `markAddedDocumentsUnparsed` (added stubs) and `LazyLoadReconciliationReport.renamedFileUnparsedOps` (renamed File) | `Current → Unparsed` or `PullingFile → Unparsed` | Marks a git-changed File stale so it parses on demand, and makes a PullingFile stub whose body arrived parseable. Posted as `Parse` through `GraphOnlyChangePost.postChunks`. `LazyLoadReconciliationServer.receivedFileIds` reads the op for a rename (`planMarksUnparsed`). | Round 8 / [130](130-reconcile-markunparsed-drops-setdocumentstate.md) | A git-received File gets `NodeReceived id`, which `reconcileReceivedFiles` already posts. Today `noticesBeside` also posts `MarkUnparsed` (from Current) or `NodeReceived` (from PullingFile). | Rename: `inferNotices` on a `Replace` that moves a File. Modified file: the changed-path list in `reconcileReceivedFiles`, not the ops. | Hard. A modified file's plan is only this op. Directory reconcile (`reconcileChangedPathsWithDiscovery`, `beside = ignore`) has no changed-path post. `Notice.NodeUnparsed` covers `Current → Unparsed` ([121 — MarkUnparsed renamed NodeUnparsed](121-markunparsed-renamed-nodeunparsed.md)). `NodeReceived` also writes Persisted (130 §6). |
| P7 | `LazyLoadReconciliationApply.fs:32` `markParentCurrent`, from `markAddedDocumentsUnparsed` | `Unparsed → Current` on an Unparsed Directory or Workspace parent of added stubs | Same as P4, for reconcile. No Notice today (`uploadOrArrival`). In a received-file plan, P9 is appended after it and wins. | Round 9 / [131](131-reconcile-parent-drops-setdocumentstate.md) | `NodeParsed dirId`, trail. Open: the same side-effect question (131 §6). | `inferNotices`: a `Parse` Change whose `Replace` attaches owned stubs to an Unparsed container | Hard. Every reconcile and every file parse uses `Parse`, and P9 has the same `Replace`. |
| P8 | `LazyLoadReconciliationApply.fs:145,147` `parseDirInfoIfPresent` `markCurrent`, from `invalidateOrParseModified`, `parseDirInfoInfos` and the Report | `Unparsed → Current` or `PullingFile → Current` on a Directory or Workspace whose `.amb` text was read | Lets the artifact parse ops into the root. `LazyLoadReconciliationServer.parsedFromPulling` and `postParsedFromPulling` read the PullingFile arm, because `noticesBeside` has no Notice for it. | Round 10 / [132](132-dir-info-parse-drops-setdocumentstate.md) | From Unparsed: `NodeParsed nodeId`, lead. From PullingFile: no single Notice fits on the mailbox path (note 1). | `inferNotices`: a `Parse` Change whose ops land inside an Unparsed or PullingFile Directory File root | Hard. A node created earlier in the same Change isn't in the pre-Change graph (132 §6). |
| P9 | `LazyLoadReconciliation.fs:370` `unparsedStateOp`, from `containingDirectoryMarkOps`, appended in `LazyLoadReconciliationReport.planReceivedFiles` | Any state `→ Unparsed`, including `Unparsed → Unparsed`, on the nearest existing containing Directory of an added, deleted or renamed file in a git pull | Marks the container's members stale. It overrides P7 because it comes later. `noticesBeside` posts `MarkUnparsed`, or `NodeReceived` when the container was PullingFile. | Round 11 / [133](133-received-file-container-drops-setdocumentstate.md) | `NodeUnparsed dirId`, trail, ordered after P7's `NodeParsed`. 121 — MarkUnparsed renamed NodeUnparsed keeps that name (133 §6). | `inferNotices`: a `Replace` that adds or removes an owned File under a Directory | Hard. The same `Replace` ends Current in P7, and an Edit create or delete has it too. Only `reconcileReceivedFiles` knows the pull came from git. |

### Production readers that act on the op

These leave with their round, or with 135.

| Site | Reads | Leaves with |
| --- | --- | --- |
| `DocumentOpImpact.fs:34–66` `documentStateTransition` and `noticesBeside`. Called by `Api.inferredNodeEvents` (`Api.fs:247`, Edit and `POST /changes`), `FileAgent.postGraphOnlyNotices` (`FileAgent.fs:108`, graph-only) and `DatabaseProjection.noticesBeside` (`DatabaseProjection.fs:137`, called by `DbAgent.fs:316`, graph-only) | Every transition, mapped to a Notice | 135 (`inferNotices`). The op arms stay until 45. |
| `LazyLoadReconciliationServer.fs:273,275` `parsedFromPulling` and `postParsedFromPulling` | `PullingFile → Current` | Round 10 / 132 |
| `LazyLoadReconciliationServer.fs:382` `receivedFileIds` / `planMarksUnparsed` | `_ → Unparsed` on a renamed File | Round 8 / 130 |

### Match sites that stay until 45

- `History.fs`: the `Op` case (line 14), `involvedNodeIds` (119), `applyAllowed` (248), `invert` (332–333), `undoAllowed` (372) and `opChangesGraphShape` (681)
- `DocumentOpImpact.fs`: `opAnchor` (76) and `touchedNodeIds` (98)
- `DatabaseProjection.fs:95` `nodeIdFromOp`
- `ResidentProjection.fs:20` `applyOpAllowing`
- `DocumentColdParse.fs:294` `targetsDocumentRoot`
- `Serialization.fs`: `encodeOp` (358–360) and `decodeOp` (410–412)
- Client: there is no match under `src/Client`. The client reaches the op only through the shared `History` and `Serialization`.
- Comments only: `ImportDocument.fs:92`, `DatabaseProjection.fs:136`, `ResidentProjection.fs:40` and `DocumentOpImpact.fs:38`

### Test-only uses

These are `rg -c` line counts: 80 lines in 20 files.

`SpecialNodeStateAxesTests` 13 · `NodeEventExpandTests` 10 · `HistoryTests` 9 · `LazyLoadReconciliationTests` 9 · `LazyLoadReconciliationServerTests` 9 · `DirectoryReconcileTests` 7 · `ImportDocumentTests` 4 · `SerializationTests` 4 · `ClientHistoryTests` 2 · `DocumentOpImpactTests` 2 · `GithubTransportLoadPullTests` 2 · `ImportTextTests` 1 · `WorkspaceUploadStructureTests` 1 · `ViewModelJoinOpsTests` 1 · `DatabaseProjectionTests` 1 · `DatabaseProjectionContractTests` 1 · `GithubTransportActorTests` 1 · `LoadSaveCommandTests` 1 · `ParseThreadLoadTests` 1 · `StateEndpointTests` 1

Several Server tests build an `Unparsed → Current` workspace fixture with the op (`GithubTransport*`, `LoadSaveCommandTests`, `StateEndpointTests`, `ParseThreadLoadTests`, `LazyLoadReconciliationServerTests`). They need a fixture change, not a Notice, when 45 removes the case.

### Notes (open, for Alan)

1. **Two Notice apply paths disagree.** `Ev.apply` (`documentStateFromNotice` → `Graph.setDocumentState`) writes the enum. That is the path the lead in `ChangeAmendment.applyPosted`, `ResidentProjection`, `BootCache` and `SyncLogic` use. The mailbox path (`CoreMailboxLoop.applyNotice` → `CoreMailboxFilesAxis`) uses `GraphMutate.setParseState`, which leaves a PullingFile node unchanged for `NodeParsed` (`GraphMutate.fs:233`). It also writes persist state for `NodeUploading` and `NodeReceived`, and pushes or calls `noteParsed` for `NodeParsed`. So P8's `PullingFile → Current` works today only because the op clears PullingFile before `postParsedFromPulling`'s Notice arrives. Open: `NodeReceived` then `NodeParsed` (the 45 mapping report's choice), or a lead applied through `Ev.apply`.
2. **No Notice is missing for a target enum.** The gaps are in sources and semantics: P5 has no op to infer from; P8 `PullingFile → Current` (note 1); and `NodeParsed` side effects for parents that were never parsed (P4, P7). `Notice.NodeUnparsed` covers `Current → Unparsed` (P6, P9, and 121 — MarkUnparsed renamed NodeUnparsed).
3. **Lead vs trail.** P1, P2 and P8 need a lead, because their op unlocks the rest. The others can trail. Round 3 is already done by a backend producer posting the lead before its ops (`ParseThread.parseDirectory`). §4 says "no producer emits one". Open: whether that precedent is allowed for P2 and P8, which are also backend (ParseThread, LazyLoadReconciliationServer), or must move to `inferNotices`.
4. **Command-name dependence.** P3, P4, P7 and P9 are told apart from ordinary edits mainly by the command name (`Load` vs `Parse` vs Edit) and by server-side knowledge (git-received vs directory reconcile), not by the ops. Open: whether `inferNotices` may key on those, or whether the poster (`reconcileReceivedFiles`) supplies them, as it does `NodeReceived` today.
5. **Client-built Changes.** P1, P3, P4 and P5 are built in the client (or `WorkspaceCloudUpload`) and applied locally before the post. Without the op, the client graph shows the new state only after the Notice comes back through sync (`SyncLogic` folds Notices).

### Core inventory

The core vs non-core dependency list and every Notice poster are in [reports/135-core-inventory.md](../reports/135-core-inventory.md).
