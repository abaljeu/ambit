# Event-sourced ops

Stage: charting
Summary: Increment-1 vocab locked; children, amendment order, rewind+replay accepted; POST vs Poll **split** (unification superseded); pipelined ACK is a signal, queue-empty Poll applies the list; soft-lock meaning accepted; slice 2 obsolete for recoverable kick-back (200 Merge); merge/conflict-kinds/fill-in still proposed; parked Load packages, Revision, /state Q4/Q5.
Updated: 2026-08-21

Related, not a replacement: [[.scratch/relaxed-concurrency/]]. That map examined Event Modeling / Event Sourcing and rejected full Event Sourcing with replay from genesis. This framework is a **more general relaxed concurrency** (global Change order, Server amends, Client rewind+replay) — [[more-general-relaxed-concurrency.md]]. Do not archive or cancel the older project.

Goal: [[goal.md]]. Relationship: [[more-general-relaxed-concurrency.md]]. Vocab: [[vocab.md]]. Conflict kinds: [[conflict-kinds.md]]. Merge: [[merge.md]]. Amendment order: [[merge.md#Amendment order]], [[amendment-order.md]]. Client correction: [[merge.md#Client correction]]. POST vs Poll (split; unification superseded): [[unified-messaging.md]], [[pipelined-post.md]]. Slice 2: [[slice-2-obsoleted.md]]. Name clash: [[name-clash-amb-conflict.md]]. Server-side Actor fit (assessment): [[server-side-actor.md]], [[in-process-apply.md]]. First Actor: [[parse-file-actor.md]]. Later: [[shell-command-actor.md]]. Server fill-in: [[server-fill-ops.md]]. Soft lock (meaning accepted): [[soft-lock.md]]. Undo: [[undo.md]] (open retained: [[undo.md#Unrestricted Undo desirability]]). Speak: [[collab-vocab.md]]. Inventory: [[whats-left.md]].
