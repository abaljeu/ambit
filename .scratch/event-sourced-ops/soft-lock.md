# Soft lock (soft lock)

Not a graph lock. Optional UX/HITL reservation. Merge stays independence + transform + HITL.

## Sense

Hint: "don't work here — someone else has it checked out." Does **not** reject Merge Changes. Does **not** replace CAS or the merge invariant.

**Area** = node fields and/or child-list spans (same as an optimistic Actor area). Not a Subgraph blob.

The same surface is the **cancel** point for the Actor that holds the area. **Cancel ≠ Undo** (accepted). Cancel = stop generating Changes. Already-merged Changes stay; they rewind only via **Undo**. Thoughts: [[undo.md]].

Hard locking is out of the standard. No glossary **cancel**. UI "cancel" today is prompts / folder pick, not Graph rewind.

## Open

Who issues the checkout (Actor start vs explicit). How it expires. Cancel likely implies the Actor that checked out — not pinned.
