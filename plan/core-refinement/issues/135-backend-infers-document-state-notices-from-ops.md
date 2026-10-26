# 135 — Backend infers document-state Notices from ops

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

`Op.SetDocumentState` is redundant. The other ops in a Change already imply the state, and the backend should post the Notice. Today the backend reads the Notice back from the op instead:

- `DocumentOpImpact.noticesBeside`, called from `Api.inferredNodeEvents`, `FileAgent.postGraphOnlyNotices`, and `DatabaseProjection.noticesBeside` (from `DbAgent.commitPostChange`)
- `LazyLoadReconciliationServer.parsedFromPulling`, on staging only

The leading op also gets content ops past `History.blockedForApply` and `ChangeAmendment.applyForGraphOnly` on an Unparsed root.

After a rollback to `47a85e1d^`:
- `parsedFromPulling` and `postParsedFromPulling` are gone (122a6b96).
- The `Import:` preview hook (`ImportText.commitImportApply` in `FileAgent` and `DbAgent`, 8c7a1b3c) is gone.
- `ResidentProjection.applyDocumentNotice` is gone, and `Ev.documentStateFromNotice` is private again (47a85e1d).
- Rounds 3 and 6 come back: `DirectoryReconcile.markCurrent` and `WorkspaceUploadStructure.markUnparsed` construct their ops again, and `ParseThread.parseDirectory` no longer posts `NodeParsed` first. No ticket here covers them.

## 4. Change

One backend place infers. Tickets 125–133 each add their transition's rule to it.

- `DocumentOpImpact.noticesBeside` becomes `inferNotices`. It takes the graph before the Change, the event's command name, and the ops, and returns the Notices in apply order. Its `Op.SetDocumentState` arms stay until [45 — SetDocumentState leaves; NodeUnpersisted replaces it](45-remove-document-state-set-ops.md) removes the op case.
- **Lead.** In `ChangeAmendment.applyPosted`, which `FileAgent` and `DbAgent` both use, an inferred `NodeParsed` is applied with `Ev.apply` before the ops. This is the general form of the job `ImportText.commitImportApply` does for import on staging.
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
