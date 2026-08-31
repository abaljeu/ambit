# Undo

Thoughts, not locked. One question inside is **open and retained** — it is not parked, and it is not answered.

## Cancel is not Undo (accepted)

Cancel stops future Ops. Undo names an already-merged Change and inverts it.

## What Undo inverts

Undo is an Actor product, so the merge rules apply to it like any other Change.

After a concurrent merge, "undo the last thing" is ambiguous: the last local Change, the last Server Change, or the last entry in this process's History. The answer that follows from amendment order is that the Change which **applied** is the **amended** one, so Undo inverts that — not the Ops as posted. After a Client rewinds and replays, its History should invert the **replayed** amended Change of its own. The other Actors' Changes in that list are not this process's undo entries.

A Client that recorded only node-local corrections cannot invert the full applied Change. That is another reason the correction strategy is rewind and replay ([[client-consume.md]]).

Completing Ops are part of the same History entry, so undoing a partial-view delete also inverts the promotion ([[completing-ops.md]]).

## History is not cleared (accepted)

**Neither post nor poll clears History.** Today's poll-with-tail clear is software debt ([[as-implemented-facts.md]]).

## Suggestion for this increment

Keep Undo linear **within one process**, as today. A concurrent undo is just another Change through merge. Do not invent an undo graph in this increment. Redo follows Undo.

## Open, retained — unrestricted Undo

Amendment order produces a global sequence of Changes, and rewind and replay consumes it. So unrestricted Undo is **possible**: an Actor could name any already-merged Change in that order and invert it — inverting the amended Change, as above.

**Whether that is desirable is open.** The global order makes it possible. A **permanent** global log ([[permanent-history-and-genesis.md]]) would retain the full sequence across server restart, which strengthens the invert-walk to genesis but does not answer the UX question.

This is distinct from *what* Undo inverts, which is settled above. This section is about *whether* unrestricted Undo should exist now that a global order does.

The user's explicit instruction was to leave this open, and not to pin this increment's Undo to own-History-only.

## Unresolved edges

- Undo of a Change whose Nodes are Unloaded in a partial Local Graph.
- Undo of an adjusted Change that carried an `amb-conflict` child.
