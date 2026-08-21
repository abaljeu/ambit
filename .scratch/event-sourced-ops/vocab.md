# Vocabulary

Mode: collaborative, incremental. Locked terms are increment 1. Not an architecture. Do not add these to [[CONTEXT.md]] yet.

Goal: a small framework for async behaviors. An Actor produces a Change. The framework is how that Change merges into a Local Graph.

## Locked

| Term | Sense |
| --- | --- |
| **Op** | The mutation. |
| **Change** | A collection of Ops. |
| **Actor** | Anything that produces a Change (sync or async). May have little or no Local Graph (Parse, later agents) — same kind as user-edit Actors. |
| **Subgraph** | Part of the Graph — the Nodes this process has. The database has the full Graph. |
| **Local Graph** | This process's graph state. Not a second type. |
| **Local Subgraph** | The same Local Graph, stressing it may be incomplete. Today Server is complete; Browser is always a Subgraph. |
| **Merge** | Apply Changes into the Server Graph or a Client Local Graph. **Not** a git three-way merge. Process (proposed): [[merge.md]] — common prior, sequence, adjust the second; never lose critical information. Algorithm / per-Op tables out of scope. Kinds: [[conflict-kinds.md]]. |

## Soft lock (soft lock)

Checkout **hint**, not a graph lock. Also the **cancel** surface. **Cancel ≠ Undo** (accepted). Undo thoughts: [[undo.md]]. [[soft-lock.md]].

Dropped: **View**, **branch**, **trunk**, **Deleted** (as a Graph state). Use **Orphaned**. Do not say checkout in the git sense.

## Document existing (not a new law)

| Term | As-implemented sense |
| --- | --- |
| **Orphaned** | Subgraph not reachable by Owned child references. Retained until GC. Not in [[CONTEXT.md]] yet. |
| **TRASH** | Recycle bin. Children are Owned by TRASH — not Orphaned. |
| Single owner | One Owned parent except ROOT and Orphaned. Extra Owned → Ref (proposed for merge). |

## Next increment

Server fill-in timing (same Change vs later Poll). Extra-Owned → Ref still proposed. Same-text HITL/reject still open.

Fill-in pattern: [[server-fill-ops.md]].

## Parked

- Changes vs Graph transfer (Load packages / `/state`).
- Whether **Revision** stays one global number.
- Action vs Change reframe (no stake). Actors produce **Changes**. Action stays History/undo speech. Types already apply Undo/Redo as Changes (`PendingKind`).
- Q4/Q5 (user paused `/state`).
- Server-partial Local Graph.

## Open, no stake — Action vs Change

[[CONTEXT.md]]: Action is a History entry (Change, Undo, or Redo). No `Action` DU. Leave the fork unpicked.
