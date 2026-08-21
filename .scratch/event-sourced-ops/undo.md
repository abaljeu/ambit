# Undo (thoughts, not locked)

Cancel ≠ Undo is **accepted**. Cancel stops future Ops. Undo names an already-merged Change and inverts it.

## As-implemented

[[CONTEXT.md]]: Undo/Redo are Actions (Emacs). Types: Undo is a **Change** (`Change.inverse` of the head of `ClientHistory.past`), tagged `PendingKind.Undo`, applied locally, then submitted. Redo is the same on `future`. Last History entry in **this process** only. Inverse is of the **recorded** Ops (creates are dropped from the inverse). No multiplayer undo protocol.

**Fill-in vs History (user correction):** as-practice, Server fill-in Ops are **relayed onto the Browser stack**. As-implemented today: ACK suffixes (`SetUpdateTime` only) project onto the Graph and **do not** enter History ([[.scratch/selective-client-loading/undo-spec.md]] item 6). Poll/Load with a non-empty Change tail **clears** History. Promote-then-remove planned in the Browser (`LocalDeleteWithPromotion`) **is** one History entry because it was recorded locally. If fill-in is a later Poll Change, that path **wipes** the stack unless we change Poll. For undo to invert delete + promote together, they must share one History entry (or we amend History on ACK).

## Thoughts for this framework

Undo is an Actor product. Merge rules apply (never lose critical information).

After concurrent merge, "undo last" is ambiguous: last local, last Server, last History here? Inverse of original Ops vs inverse of **adjusted** Ops that applied.

Server fill-in: undoing a partial-view Owned-edge delete may need to invert the promote-Ref too (same Change or follow-on). Completing Ops are part of what undo must consider.

Partial Local Graph: undo of a Change whose Nodes are Unloaded. Same-text HITL: undo of one Actor vs another still editing that field.

**Suggestion:** keep Undo History-linear **in one process** (today). Concurrent undo is just another Change through merge. Do not invent an undo DAG this increment. Redo follows.
