# Undo (thoughts, not locked)

Cancel ≠ Undo is **accepted**. Cancel stops future Ops. Undo names an already-merged Change and inverts it.

## As-implemented

[[CONTEXT.md]]: Undo/Redo are Actions (Emacs). Types: Undo is a **Change** (`Change.inverse` of the head of `ClientHistory.past`), tagged `PendingKind.Undo`, applied locally, then submitted. Redo is the same on `future`. Last History entry in **this process** only. Inverse is of the **recorded** Ops (creates are dropped from the inverse). No multiplayer undo protocol.

**Fill-in vs History (user correction):** as-practice, Server fill-in Ops are **relayed onto the Browser stack**. As-implemented today: ACK suffixes (`SetUpdateTime` only) project onto the Graph and **do not** enter History ([[.scratch/selective-client-loading/undo-spec.md]] item 6). Poll/Load with a non-empty Change tail **clears** History. Promote-then-remove planned in the Browser (`LocalDeleteWithPromotion`) **is** one History entry because it was recorded locally. Later Poll fill-in is **rejected** (timing accepted: same Change). Undo of that History entry inverts delete + promote together.

## Thoughts for this framework

Undo is an Actor product. Merge rules apply (never lose critical information).

After concurrent merge, "undo last" is ambiguous: last local, last Server, last History here? Inverse of original Ops vs inverse of **adjusted** Ops that applied. Amendment order ([[merge.md#Amendment order]]) says the applied newest Change is the **amended** one; Undo inverts that, not the posted Ops. Client correction is rewind+replay ([[merge.md#Client correction]]): after replay, History should invert the **replayed** amended own Change. Other Actors' accepted Changes in the list are not this process's undo entries. **Neither POST nor Poll clears History** (accepted, [[unified-messaging.md]], [[post-ack-history.md]]). Today's Poll-with-tail clear is software debt. A Client that recorded only node-local corrections cannot invert the full applied Change.

Server fill-in: undoing a partial-view Owned-edge delete inverts the promote-Ref too (same Change). Completing Ops are part of that History entry.

Partial Local Graph: undo of a Change whose Nodes are Unloaded. Same-text: undo of the adjusted Change (conflict child included if it rode in that Change).

**Suggestion:** keep Undo History-linear **in one process** (today). Concurrent undo is just another Change through merge. Do not invent an undo DAG this increment. Redo follows.

## Unrestricted Undo desirability

**Open. Retained. Not answered. Not parked. Undo is not locked.**

Amendment order produces a global sequence of Changes. Rewind+replay consumes that sequence. So **unrestricted Undo is possible**: an Actor can name any already-merged Change in that order and invert it (invert the **amended** Change — that part is already the thought for *what* Undo inverts).

**Desirability is the open question:** can Actors **see and understand** those edits well enough to choose Undo properly? Do not invent a UI here. Do not resolve yes or no. Grill Q3 → **C**: leave this open; do not pin increment-2 Undo to own History only.

Distinct from: when Undo runs, invert the amended Change (not the posted Ops). That is *what* to invert. This section is *whether* unrestricted Undo is desirable now that a global order exists.

Worker report: [[undo-unrestricted-desirability.md]].
