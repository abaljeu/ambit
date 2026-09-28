# Independent review: 01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge

Independent two-axis review of product PR [162 — Implement Poll/sync undo-all-pending then apply Server merge](https://github.com/abaljeu/ambit/pull/162). The reviewer did not write the implementation. Not approval. Ticket [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md) **Status:** stays `coded` on the product tip.

**Verdict: Must-fix**

One Spec Must-fix: `applySyncResponse` rewinds every pending Graph op on any non-empty Event or Want payload, not only on recoverable `SetName` / `SetText` / `SetClasses` / `Replace` mismatch. That path is live on `applyServerTail` (including CommandDone Actor tails). `withAppliedSync` writes the rewound Graph and leaves `SyncInfo.pending`. Optimistic Graph ops disappear from the Graph while the post queue still holds them.

**Pin:** product tip `7154ad95` (named `cursor/poll-sync-undo-all-pending-bd9a`). User named `origin/staging`; three-dot `origin/staging...7154ad95` is non-empty (merge-base `387738d3`).

**Commits:** `7154ad95` Implement Poll/sync undo-all-pending then apply Server merge.

**Spec:** [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md). Locked policy: never hard-fail the Poll fold; on recoverable `SetName` / `SetText` / `SetClasses` / `Replace` mismatch invert or drop all pending Graph ops, then apply the Server merge and continue; soft-skip-without-apply is wrong; leave `#edit-input` alone unless focus or the Node is gone.

**Mechanical scan:** `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` from the product tip. Exit 0. measure-fs-size bindings are 5–19 lines.

Axis drafts: [Standards](code-review-standards-poll-sync-undo-all-pending.md), [Spec](code-review-spec-poll-sync-undo-all-pending.md). Axes stay separate below.

## Standards

No documented-standard violations or smells on [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md).

## Spec

1. Scope creep — rewind on every Event or Want payload. [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md) What to build: "When Poll or sync apply hits a recoverable field mismatch, the Browser undoes every pending Graph op, applies the Server merge for that field, and continues the fold." [1. Recoverable Poll/sync field mismatch](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md) names that trigger as `SetName`, `SetText`, `SetClasses`, or `Replace` CAS mismatch. `applySyncResponse` calls `rewindPending` when `events`, `nodes`, or `childMap` is non-empty, including a clean Event tail with no mismatch. `rewindPending` sets `pending = []` on `ClientSyncState`. [Client correction — rewind and replay](plan/event-sourced-ops/details/client-consume.md) leftover pending (accepted): "Those stay as planned and unamended." Browser `SyncInfo.pending` is not taken from that field (`withAppliedSync` copies graph, history, eventId, and actorLiveFocusIds only), so the leftover queue is not dropped. The Graph still loses leftover optimistic Ops on every payload, not only on recoverable mismatch.
2. Implemented path still soft-skips when the second apply fails. [2.2 Never soft-skip without apply](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md): "Do not skip a mismatched authoritative payload and continue. After undo, apply the Server merge for that field." [1.2 Apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md): "Do not leave the field at the undone local value." After `undoAllPending`, if `applyOp` still returns a recoverable CAS Invalid, `applyOpForSync` returns `Unchanged` and does not write the Server new value. Tests `applyServerTail soft-skips SetName CAS`, `applyServerTail soft-skips SetText CAS`, `applyServerTail soft-skips SetClasses CAS`, and `applyServerTail soft-skips Replace CAS` still assert that skip.

## Must-fix

1. Restrict rewind to the ticket trigger. Keep undo-all-pending then apply on recoverable `SetName` / `SetText` / `SetClasses` / `Replace` mismatch, and keep undo-then-apply of a Want payload ([4.2 Want overwrite proving test](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md)). Do not call `rewindPending` on every non-empty Event list. `applyCommandEvents` uses `applyServerTail`, which now rewinds before ActorStart / ActorStop. `Ev.ops` is none for Actor bodies, so the only Graph change is the undo. A person can hold pending Graph ops while a Command returns. The Graph loses those ops; `SyncInfo.pending` still posts them later.

Spec finding 2 is not Must-fix. The empty-pending soft-skip tests are the never-hard-fail remainder when undo cannot make the old-value precondition true. Force-write of the Server new value was not specified. The pending-conflict path now applies the Server merge (`server.md` / `server` / `hel` plus suffix `lo`).

## Verification

1. **Mechanical standards scan** — exit 0 from the product tip. No FILE, long-line, tab, or `mutable` hits. Bindings in the range are 5–19 lines.
2. **Focused Shared tests** — `dotnet test tests/Shared.Tests/Gambol.Shared.Tests.fsproj --filter "FullyQualifiedName~SplitOriginTraceTests|FullyQualifiedName~ResidentProjectionApplyTests|FullyQualifiedName~SyncLogicTests"` passed 57 of 57 on tip `7154ad95`.
3. **Locked policy that holds** — Recoverable pending mismatch applies the Server merge and continues. The Poll fold does not Error that case. No Client file rewrites `#edit-input`. [4.1 Truncated-line split](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md) expects suffix `lo` and prefix `hel`. [4.2 Want overwrite proving test](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md) applies Want after undo; it does not skip Want.

## Summary

Standards: 0 findings. Spec: 2 findings; worst in-axis is rewind on every Event or Want payload. Overall: **Must-fix** for Spec finding 1.
