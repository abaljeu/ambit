# Change-only Undo behavioral contract

See also: [[undo-wayfinder.md]], [[undo-implementation-plan.md]], [[audit-optimistic-undo-safety.md]], [[server-change-augmentation-audit.md]], [[spec.md]]

## Status

Commit `4255c48` delivered the current explicit `ChangeRequest` behavior from [[plan/selective-client-loading/issues/15-introduce-change-request-messaging.md]]. The earlier abandoned `stash@{0}` must not be applied. The contract below is the approved Change-only destination and supersedes that delivered transport behavior as its implementation slices land.

## Contract

1. The Browser sends every normal command, Undo, and Redo as an ordinary Change. The Server applies, persists, confirms, Polls, and Loads ordinary Changes and keeps no Undo state.
2. Browser History is client-only. One History-worthy local Change creates one logical record containing its command name, stable record identity, and exactly the last submitted local Change for its applied direction. One user invocation may create several records.
3. Undo and Redo are optimistic. Each creates a complete inverse Change with a fresh request identity and projects it under Resident and Loaded rules. Effects for Absent Headers and Unloaded Children are consumed without widening residency.
4. Ordinary inversion reverses source Ops, swaps old and new values, and omits node-creation Ops. Undo of create or paste detaches created Nodes but keeps their Headers; Redo reconnects the same Node IDs.
5. A successful ACK returns one complete durable confirmed Change per submitted Change in request order. Each confirmation has the submitted identity and submitted Ops as an exact prefix. Any appended suffix contains only authoritative `SetUpdateTime` Ops.
6. ACK suffix Ops project atomically through the resident-projection seam but never enter or alter Browser History. A client-submitted `SetUpdateTime` is part of the submitted prefix and remains invertible.
7. C, Undo, and Redo for one logical record may share an ordered batch. All transitions remain eligible for selection, and confirmation neither amends History nor changes an already-planned inverse.
8. Exact ordered submission membership, submitted bodies, identities, and confirmation lineage survive later queue growth and retry. Retry preserves each Change body and identity; only its release-time base Revision may change.
9. A complete ACK is validated before state changes. Missing, reordered, unmatched, changed-prefix, partial-overlap, forward-Revision, duplicate-with-different-content, or forbidden-suffix responses require reload. A fully valid late response may be ignored only when all its submitted identities are already retired and its Revision is not ahead; it must not apply suffixes twice or move Revision backward.
10. Every normal and workspace submission establishes exact confirmation lineage before issuing the request. Synchronous workspace posts and asynchronous upload-structure posts use the same atomic confirmation rules even though they bypass the normal queue.
11. A rejected submission discards the persisted pending queue, marks synchronization as rejected, and requires reload. Do not reverse the optimistic chain or attempt a best-effort merge.
12. A non-empty semantic remote Poll or Load Change tail clears Browser History before projected application. An empty tail preserves History. Do not match or rebase remote tails against Browser History.
13. Package-only Load residency may preserve History only at the same settled Revision with no pending local transition or submission awaiting response or retry. Refuse a raced payload and require reload.
14. Browser refresh may restore pending Changes onto a fresh Server snapshot using the existing filter, projection, persistence, and retry behavior. Restored Changes have no History lineage and do not recreate Browser History.
15. Persistence stamps remain appended to the last newly persisted Change in a batch, including when later request items are duplicates. A duplicate returns its stored complete confirmation without another apply or persist. An unchanged first submission rejects the batch.

## Command names and feedback

Resolve names when a local History record is created. Use `Edit node`, `Paste`, `Cut`, `Load`, and explicit `Download` at audited non-registry sources. Automatic path refresh and auto-download create no History record.

After an optimistic transition, display `Undo: <command name>` or `Redo: <command name>`. Empty History displays `Undo: nothing to undo` or `Redo: nothing to redo`.

## Deferrals

- Durable or cross-session Browser History.
- Invocation grouping.
- Detached-Node garbage collection.
- A separate Undo endpoint or action codec.
- Compatibility decoding for explicit Undo or Redo requests.
- Conflict-policy, Revision, Poll/Load scope, or Server residency changes.
