# 131 — Reconcile parent drops its SetDocumentState

**Status:** `coded`

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

This is round 9 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `LazyLoadReconciliationApply.markParentCurrent` moves an Unparsed parent `Unparsed → Current` after its stubs attach. No Notice covers this today (`uploadOrArrival`).

## 4. Change

- Delete the `Op.SetDocumentState(nodeId, Unparsed, Current)` line.

Backend inference: a `Parse` graph-only Change whose `Replace` attaches owned stubs to an Unparsed Directory or Workspace infers a trail `NodeParsed dirId`. The rule goes in `DocumentOpImpact.inferNotices` (135).

Hard: every reconcile and every file parse posts with command name `Parse` (`GraphOnlyChangePost.postChunks`, `ParseThread.postParseOps`). The same `Replace` in a received-file reconcile ends with the container Unparsed (133). The ops cannot tell the two apart.

Source: `LazyLoadReconciliationApply.fs`, `DocumentOpImpact.fs`.

## 5. Tests

[ ] `LazyLoadReconciliationTests` (the facts where stubs attach under an Unparsed parent)
[ ] `LazyLoadReconciliationServerTests`

## 6. Answer (Alan)

Answered. Tabled.

No. `applyNodeParsed`'s `pushParsedUnpersisted` and `noteParsed` is not right for an upload or reconcile parent that goes `Unparsed → Current` without a parse.

Alan believes the question concerns the WebDAV-channel upload case.

Fixing it is tabled.

The intended direction: such uploads should go through the existing Load channel, which invokes parse at the right moments. It is unknown whether Load supports everything needed.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.

## 8. Round

[ ] `LazyLoadReconciliationApply.parentsOfAttachedStubs` names the Unparsed Directory or Workspace that owns each attached stub. `markParentCurrent` uses that same container check. The ids do not come from reading `Op.SetDocumentState`.
[ ] `LazyLoadReconciliationReport.parsedParentIds` carries those ids. `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `Notice.NodeParsed` for each id on the core mailbox, the same post as `Notice.NodeReceived`.
[ ] `git load reconcile posts NodeParsed for an Unparsed parent` asserts that Notice and legacy Current. It does not require the `SetDocumentState` op.
[ ] `DocumentOpImpact.noticesBeside` is unchanged.
[ ] `CoreMailboxFilesAxis.applyNodeParsed` already sets legacy DocumentState to Current through `Node.withParseState` and `DocumentState.toLegacy`. `NodeParsed sets legacy DocumentState to Current` asserts that.
[ ] `LazyLoadReconciliationApply.markParentCurrent` still writes the `SetDocumentState` op.
[ ] [6 — Answer (Alan)](#6-answer-alan) is answered and tabled. This round leaves `pushParsedUnpersisted` and `noteParsed` unchanged.
