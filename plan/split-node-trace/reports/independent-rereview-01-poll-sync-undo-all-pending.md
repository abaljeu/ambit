# Independent re-review: 01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge

Independent two-axis re-review of product PR [162 — Implement Poll/sync undo-all-pending then apply Server merge](https://github.com/abaljeu/ambit/pull/162) after the Must-fix at tip `6770efde`. Prior review: [165 — Review PR 162 Poll/sync undo-all-pending apply merge](https://github.com/abaljeu/ambit/pull/165). The reviewer did not write the implementation. Not approval. Ticket [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md) **Status:** stays `coded` on the product tip.

**Verdict: Good**

Prior Must-fix is **closed**. `applySyncResponse` no longer rewinds pending Graph ops on a non-empty Event list. `applyServerTail` (CommandDone Actor tails included) keeps leftover optimistic Graph ops while `SyncInfo.pending` still holds them.

**Pin:** product tip `6770efde` (named `cursor/poll-sync-undo-all-pending-bd9a`). User named `origin/staging`; three-dot `origin/staging...6770efde` is non-empty (merge-base `387738d3`).

**Commits:** `7154ad95` Implement Poll/sync undo-all-pending then apply Server merge. `6770efde` Scope pending rewind off Event tails.

**Spec:** [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md). Locked policy: never hard-fail the Poll fold; on recoverable `SetName` / `SetText` / `SetClasses` / `Replace` mismatch invert or drop all pending Graph ops, then apply the Server merge and continue; soft-skip-without-apply is wrong; leave `#edit-input` alone unless focus or the Node is gone. Want overwrite stays undo-then-apply Want.

**Mechanical scan:** `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` from the product tip. Exit 0. measure-fs-size bindings are 4–19 lines.

Axis drafts: [Standards](code-review-standards-poll-sync-undo-all-pending-rereview.md), [Spec](code-review-spec-poll-sync-undo-all-pending-rereview.md). Axes stay separate below.

## Standards

No documented-standard violations or smells on [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md).

## Spec

No Spec findings.

## Must-fix check

Prior Must-fix at `7154ad95`: `hasSyncPayload` treated a non-empty Event list as a rewind trigger. `rewindPending` inverted every pending Graph op and cleared `ClientSyncState.pending` before `foldProjectedEvents`. `applyServerTail` builds `nodes = []` and `childMap = Map.empty` but still passed Events, so CommandDone Actor tails rewound leftover optimistic Graph ops. `withAppliedSync` wrote that Graph and left `SyncInfo.pending`.

Closed at `6770efde`:

1. `applySyncResponse` folds Events first. `rewindPendingBeforeWant` runs only when `nodes` or `childMap` is non-empty. `applyServerTail` has no Want payload, so ActorStart / ActorStop and a clean Change keep the optimistic Graph.
2. Recoverable `SetName` / `SetText` / `SetClasses` / `Replace` mismatch still undoes all pending in `applyOpForSync`, then applies the Server merge.
3. Want still rewinds pending Graph ops, then `installWantAnswer`.
4. New tests: `applyServerTail ActorStart keeps pending Graph ops` and `applyServerTail non-mismatch Change keeps pending Graph ops`.

The prior non-Must-fix (second-apply CAS still `Unchanged` when undo cannot make the old-value precondition true) stays out of this verdict. Force-write of the Server new value was not specified.

## Verification

1. **Mechanical standards scan** — exit 0 from the product tip. No FILE, long-line, tab, or `mutable` hits. Bindings in the range are 4–19 lines.
2. **Source at tip** — [applySyncResponse](src/Shared/SyncLogic.fs) does not call `undoPendingGraph` on Events. [applyOpForSync](src/Shared/ResidentProjection.fs) still undoes all pending on recoverable CAS, then applies. [applyCommandEvents](src/Client/UpdateActorLive.fs) still uses `applyServerTail` then `withAppliedSync`.
3. **Locked policy that holds** — Recoverable pending mismatch applies the Server merge and continues. The Poll fold does not Error that case. No Client file rewrites `#edit-input`. [4.1 Truncated-line split](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md) expects suffix `lo` and prefix `hel`. [4.2 Want overwrite proving test](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md) applies Want after undo; it does not skip Want.

## Summary

Standards: 0 findings. Spec: 0 findings. Overall: **Good**. Prior Must-fix closed.
