# Revise Undo plan for submitted-only History

## Files changed

- [[undo-implementation-plan.md]] is now internally based on submitted-only Browser History, exact in-flight confirmation lineage, and metadata-only ACK suffix projection.
- [[undo-wayfinder.md]] now states the same destination invariants, four-operation ClientHistory seam, ordered ACK contract, batch behavior, and focused checkpoints.
- [[undo-spec.md]] keeps the delivered ChangeRequest behavior as historical context and replaces its contradictory revised expectation with the approved Change-only destination decision.
- [[establish-undo-slices-3a-3b.md]] is marked superseded; its scheduling evidence remains historical, but its old same-record and History-amendment decisions are no longer active guidance.
- [[revise-undo-plan-submitted-history.md]] records this planning revision.

Historical implementation and review reports were not edited because they accurately describe the current Slice 2 source that the next correction must replace. [[audit-optimistic-undo-safety.md]] already marks complete-confirmed-Change History as the rejected alternative and submitted-only History as the enforceable destination. [[server-change-augmentation-audit.md]] remains current evidence about `SetUpdateTime` enrichment and append-to-last-new assignment.

## Exact decisions

- Browser History stores exactly the last client-submitted local Change for each logical record. ACK handling never amends or duplicates a History record.
- A complete confirmation must preserve submitted Ops as an exact prefix. Its suffix is computed after that prefix, may contain only `SetUpdateTime`, projects through ResidentProjection, and is never stored or inverted by Browser History.
- A client-submitted `SetUpdateTime` remains inside the submitted prefix and remains invertible.
- C, Undo, and Redo transitions for one `recordId` may share a batch. There is no same-record batch stop and no queued or in-flight inverse rewrite.
- SyncPlanner owns PendingTransition confirmation lineage and one private exact `InFlightBatch`. Retry resends exact in-flight membership even when pending grows.
- Synchronous workspace submissions and the async upload-structure bypass register singleton exact in-flight lineage before issuing their requests and reconcile through the common atomic ACK seam.
- Non-empty semantic Poll or Load Change tails clear History before projection. Empty tails preserve it. Package-only residency expansion preserves History only at the same settled Revision with no pending or in-flight local transition; a raced payload is refused and requires reload.
- Complete durable confirmations, duplicate payload lookup, unchanged-new rejection, and append-to-last-new persistence assignment remain part of the wire and Server plan.

## Slice 2 correction contract

ClientHistory has only `record`, `undo`, `redo`, and `clear`. It owns stack order, Emacs-style future folding, command names, stable record identity, and submitted-only applied Changes. `record`, `undo`, and `redo` return stable record identity for the queue seam.

Remove `confirm`, `pendingByRecord`, confirmation-prefix validation, record amendment, dependent inverse re-derivation, and their tests from ClientHistory. Move exact identity/prefix/suffix validation and transition retirement to SyncPlanner and the atomic ACK reconciliation seam. Keep ordinary inversion, detached create/paste retention, names, future folding, and no-duplicate-record tests.

## Slice boundaries

Slice 3a remains independently useful for ordinary queued Change conversion, private exact `InFlightBatch` ownership, retry isolation, workspace singleton registration, and restored-pending adaptation. It has no same-record dependency mechanism.

Slice 3b remains the runtime History and projected local-flow slice. It adds submitted-only normal recording, optimistic Undo/Redo through ResidentProjection, semantic remote-tail clearing, and settled package-only Load guards. Complete ACK suffix reconciliation remains in Slice 5 after Slice 4 supplies complete confirmations.

## Verification

- Inspected the final diffs for [[undo-implementation-plan.md]], [[undo-wayfinder.md]], [[undo-spec.md]], and [[establish-undo-slices-3a-3b.md]].
- Searched the changed planning documents for stale dependent re-derivation, same-record blocking, complete-confirmed-Change History, ClientHistory confirmation, and ACK History-amendment language.
- Retained rejected semantics only in [[audit-optimistic-undo-safety.md]], where they are explicitly marked as the rejected alternative, and in historical Slice 2 reports that describe current source.
- No F# source, tests, WORK board, project stage, project index, branch, commit, build, test, or remote operation was changed or run.

## Proposed WORK.md mutations

- `remove` [[undo-implementation-plan.md]] from Active because the submitted-only History revision is complete and verified.
- `move` [[correct-undo-slice-2-submitted-history.md]] from Blocked to Pending because the revised Slice 2 contract now unblocks it.
- `remove` [[audit-optimistic-undo-safety.md]] from Active if the root still has a stale audit entry; the audit is complete.
