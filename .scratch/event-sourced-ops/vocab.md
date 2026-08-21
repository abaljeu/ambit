# Vocabulary

Mode: collaborative, incremental. Locked terms are increment 1. Not an architecture. Do not add these to [[CONTEXT.md]] yet.

Goal: a small framework for async behaviors. An Actor produces a Change. The framework is how that Change merges into a Local Graph. More general than [[.scratch/relaxed-concurrency/]] (sibling, not a replacement): [[more-general-relaxed-concurrency.md]].

## Locked

| Term | Sense |
| --- | --- |
| **Op** | The mutation. |
| **Change** | A collection of Ops. |
| **Actor** | Anything that produces a Change (sync or async). May have little or no Local Graph (Parse, later agents) — same kind as user-edit Actors. |
| **Subgraph** | Part of the Graph — the Nodes this process has. The database has the full Graph. |
| **Local Graph** | This process's graph state. Not a second type. |
| **Local Subgraph** | The same Local Graph, stressing it may be incomplete. Today Server is complete; Browser is always a Subgraph. |
| **Merge** | Apply Changes into the Server Graph or a Client Local Graph. **Not** a git three-way merge. Process (proposed): [[merge.md]] — never lose critical information. **Amendment order (accepted):** common prior, then other Actors' accepted Changes, then amend the newest Actor's Change. **Client correction (accepted):** rewind to that base, replay the Server sequence. Node-local corrections without the other Changes are invalid. Algorithm / per-Op tables out of scope. Kinds: [[conflict-kinds.md]]. Order: [[merge.md#Amendment order]]. Correction: [[merge.md#Client correction]]. |

## Soft lock (soft lock)

**Meaning accepted:** advisory checkout of a long-running Actor's subtree — recommended to work elsewhere; **not** a hard lock / not Reject. Concurrent edits there are legal; the job is amended as newest (200 Merge). Issuance / expiry / chrome still proposed. Also the **cancel** surface (proposed). **Cancel ≠ Undo** (accepted). Undo thoughts: [[undo.md]]. Unrestricted Undo desirability (open, retained, not parked): [[undo.md#Unrestricted Undo desirability]]. [[soft-lock.md]].

Dropped: **View**, **branch**, **trunk**, **Deleted** (as a Graph state). Use **Orphaned**. Do not say checkout in the git sense. **DocumentState** is deleted from this spec (inferred; see [[merge.md]]).

## Document existing (not a new law)

| Term | As-implemented sense |
| --- | --- |
| **Orphaned** | Subgraph not reachable by Owned child references. Retained until GC. Not in [[CONTEXT.md]] yet. |
| **TRASH** | Recycle bin. Children are Owned by TRASH — not Orphaned. |
| Single owner | One Owned parent except ROOT and Orphaned. A well-formed Change does not raise owner 1→2. Extra Owned → Ref is a **bug net**, not a merge case. |

## Next increment

Fill-in timing **accepted**: same Change as the delete ([[server-fill-ops.md]]). Owner count **accepted**: a Change does not 1→2; Extra-Owned → Ref is a bug net. Classes **accepted**: merge is set delta, not implemented replace. Same-text **accepted**: Server arrival is first; `B` on the Node; Add Node first child `amb-conflict` / `C`; optimistic Client **rewind+replays** (not `SetText C→B`). **Client correction (accepted):** rewind to the common prior, replay the Server sequence ([[merge.md#Client correction]]). **Leftover pending (accepted):** send unamended; Server amends on apply. POST/Poll report last-received Server Revision only. **POST vs Poll (accepted split):** POST ACK **informs** of external Changes + note **baseline**; queue-empty **Poll** applies the Change list (undo to baseline + replay). **"Poll = empty POST" superseded** ([[pipelined-post.md]], [[unified-messaging.md]]). **Neither** clears History (today's Poll clear is debt). Recoverable kick-back is 200 Merge, not slice 2 Reject ([[slice-2-obsoleted.md]]). **Name (accepted):** Server arrival is first; `amb-conflict` child with the new name (Merge success, not Reject). **DocumentState (accepted):** field deleted — NoServerFile / Unparsed inferred; not a merge case. **Children (accepted):** default positional Replace (posted Op / happy path). Conflict: best approximation; critical invariant is occurrence-bag Accept Both (edges, not Node ids). Server amends the newest Replace **after** other accepted Changes ([[merge.md#Amendment order]]) — not a node-local rewrite, and not instead of sending those Changes. Implemented span-CAS Replace is behavior to beat, not the spec. `amb-conflict` is a node indicator (text and name), not edges. That child is a **Normal Node**; conflict-ness is the `amb-conflict` name/role, not a Kind.

Fill-in pattern: [[server-fill-ops.md]].

## Parked

- Changes vs Graph transfer (Load packages / `/state`).
- Whether **Revision** stays one global number. Narrow pin only: POST/Poll carry the last Revision the Client **received from the Server** — not a locally advanced number.
- Action vs Change reframe (no stake). Actors produce **Changes**. Action stays History/undo speech. Types already apply Undo/Redo as Changes (`PendingKind`).
- Q4/Q5 (user paused `/state`).
- Server-partial Local Graph.

## Open, retained — Unrestricted Undo

Global event order makes unrestricted Undo **possible**. Desirability (see and understand those edits) is **open**. Home: [[undo.md#Unrestricted Undo desirability]]. Not parked.

## Open, no stake — Action vs Change

[[CONTEXT.md]]: Action is a History entry (Change, Undo, or Redo). No `Action` DU. Leave the fork unpicked.
