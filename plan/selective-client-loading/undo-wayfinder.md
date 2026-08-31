# Change-only Undo destination

See also: [[undo-spec.md]], [[undo-implementation-plan.md]], [[audit-optimistic-undo-safety.md]], [[server-change-augmentation-audit.md]], [[spec.md]], [[doc/current/sync-mvp.md]], [[doc/arch.md]]

## Destination

Every local graph action sends an ordinary `Change`. A normal command sends its planned Change. Undo sends a new inverse Change. Redo sends a new inverse of the last applied Undo Change. The Browser applies each Change optimistically through resident-projection rules, and the Server applies the same Change to its full Graph.

The Server confirms each accepted request with the complete persisted Change for the submitted identity. Browser History keeps only submitted local Changes. Poll and Load continue to carry complete Changes from ChangeLog.

The wire batch, pending queue, Server apply path, ChangeLog, Poll, and Load therefore share one modification unit. Explicit Undo and Redo request cases and process-local Server History leave this path, so a Server restart cannot remove Undo capability.

## Why this destination

Current create and paste Undo remove each created Node separately and rebuild the Graph once per removal. Ordinary inverse Changes detach created Nodes through inverse Replace Ops and keep their Headers for Redo, which removes that proven per-Node rebuild.

Server-interpreted Undo also depends on process-local History. A materialized inverse Change makes retry, persistence, restart, Poll, and Load use the same durable unit.

Submitted-only Browser History separates user intent from Server persistence enrichment. This permits C, Undo, and Redo for one logical record to share a batch without confirmation changing an inverse that was already planned.

## Major decisions

- [[undo-spec.md]] owns the behavioral contract and invariants.
- Browser History stores exactly the last submitted local Change for each logical record. ACK suffix metadata never enters History.
- Complete confirmations preserve submitted Ops as an exact prefix and may append only `SetUpdateTime`.
- C, Undo, and Redo may share an ordered batch. All transitions remain eligible for selection, and confirmation never changes an already-planned inverse.
- Submission identity and body stay stable across retry. A valid late response cannot apply metadata twice or move Revision backward.
- Every normal and workspace submission carries exact confirmation lineage, including workspace paths that bypass the normal queue.
- Non-empty semantic Poll or Load tails clear Browser History before projection. Empty tails preserve it. Package-only residency may preserve History only at the same settled Revision with no local submission or transition.
- Rejection or invalid confirmation requires reload; there is no optimistic rollback or best-effort merge.
- Browser refresh may restore pending Changes, but does not recreate Browser History.
- One local Change creates one History record. A user invocation may create several Changes, including dirty-text Undo and multi-phase Load.

## Client History and inversion

Client History is a pure Browser module with `record`, `undo`, `redo`, and `clear`. It owns stack movement, stable logical record identity, command names, and submitted Changes. Synchronization owns pending confirmation lineage.

Ordinary inversion reverses source Ops, swaps old and new values, omits node-creation Ops, and assigns a fresh request identity. Undo of create or paste detaches Nodes; Redo reconnects the same Node IDs. Detached-Node garbage collection remains separate.

## Command feedback

Resolve command names at the event source. Use `Edit node`, `Paste`, `Cut`, `Load`, and explicit `Download` for audited non-registry sources. Automatic path refresh and auto-download create no History record.

Display `Undo: <command name>` and `Redo: <command name>` after the optimistic transition. Empty History displays `Undo: nothing to undo` or `Redo: nothing to redo`.

## Delivery

Implement [[undo-implementation-plan.md]] in order. Slice 3a owns the existing submit/retry mechanics; Slice 5 owns complete ACK reconciliation. Evidence remains in the linked audits and implementation reports rather than this destination document.

## Deferrals

- No durable or cross-session Browser History.
- No invocation grouping.
- No detached-Node garbage collection.
- No second action codec, Undo endpoint, or compatibility decoder.
- No conflict-policy, Revision, Poll/Load scope, or Server residency redesign.
- No secondary optimization without measurement.
