# 132 — Dir info parse drops its SetDocumentState

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

This is round 10 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md), in the same reconcile code as [130](130-reconcile-markunparsed-drops-setdocumentstate.md) and [131](131-reconcile-parent-drops-setdocumentstate.md). In `LazyLoadReconciliationApply.parseDirInfoIfPresent`, `markCurrent` (`Unparsed → Current` or `PullingFile → Current`) leads the artifact parse ops and unlocks them. `noticesBeside` derives a Notice for `Unparsed → Current` only when there is no other Unparsed or PullingFile target. `LazyLoadReconciliationServer.parsedFromPulling` and `postParsedFromPulling` read the op for `PullingFile → Current`.

The reconcile path must post the parse Notices itself, as `reconcileReceivedFiles` already posts NodeReceived, NodeUnparsed (130) and NodeParsed (131). It must not rely on `noticesBeside` or any inference, and must not read SetDocumentState ops.

## 4. Change

- The Notice node is the Directory being parsed by `parseDirInfoIfPresent`.
- Post `Notice.NodeParsing dirId` before `parseDirInfoIfPresent` starts.
- Post `Notice.NodeParsed dirId` after the reconcile Change, i.e. after `parseDirInfoIfPresent` completes.
- Post on the core mailbox from `reconcileReceivedFiles` (an actor), as for NodeReceived.
- Replace `parsedFromPulling` and `postParsedFromPulling` with the direct NodeParsed.
- Delete both `markCurrent` arms once the tests pass without them.

`parseDirInfoIfPresent` is also called from `LazyLoadReconciliation.fs:269` and `LazyLoadReconciliationReport.fs:154`; check those callers.

Source: `LazyLoadReconciliationApply.fs`, `LazyLoadReconciliationServer.fs`.

## 5. Tests

[ ] `LazyLoadReconciliationTests` `exact amb add with text parses outline immediately`, `exact amb modify with text reparses instead of leaving unparsed`, `exact amb modify warm keeps node id on text edit`
[ ] `LazyLoadReconciliationServerTests`
[ ] New: git Load reconcile posts NodeParsing before and NodeParsed after the dir-info parse, passing with the `markCurrent` arms emptied.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.

## 8. Round

[ ] `LazyLoadReconciliationReport.parsedDirIds` lists each Directory or Workspace `parseDirInfoIfPresent` parsed.
[ ] `invalidateOrParseModified` and `finalizeAdded` supply that id. `parseDirInfoInfos` drops it. That planner returns ops.
[ ] `LazyLoadReconciliationServer.reconcileReceivedFiles` plans Added and Modified directory files with the received files. `receivedFileChanges` still chooses `Notice.NodeReceived`.
[ ] That actor posts `Notice.NodeParsing` for each `parsedDirIds` entry before the reconcile Change, and `Notice.NodeParsed` for each entry after the Change.
[ ] A renamed directory file stays off that plan. The actor posts `Notice.NodeParsed` for the existing directory id. It does not post `Notice.NodeParsing` for that rename, and it does not rename the directory.
[ ] `parsedFromPulling` and `postParsedFromPulling` are gone.
[ ] `DocumentOpImpact.noticesBeside` is unchanged.
[ ] `CoreMailboxFilesAxis.applyNodeParsed` still sets legacy DocumentState to Current.
[ ] `git load reconcile posts NodeParsing before and NodeParsed after the dir-info parse` checks that order on a Current directory. The fact holds with the `markCurrent` arms emptied.
[ ] `parseDirInfoIfPresent` keeps the `Unparsed`, `PullingFile`, and `Current` branch for previous artifact text. That function emits no `Op.SetDocumentState`.
[ ] `ChangeAmendment.applyPosted` with graph-only true, and `ChangeAmendment.applyForGraphOnly`, apply with `guardUnparsed` false. That apply allows outline ops on an Unparsed document.
[ ] `LazyLoadReconciliationApply.applyOps` uses that same graph-only apply.
[ ] `Notice.NodeParsing` still changes nothing. `Notice.NodeParsed` after the Change sets legacy Current.
[ ] `git load reconcile parses an Unparsed directory file and NodeParsed sets Current` checks the outline text and legacy Current.
