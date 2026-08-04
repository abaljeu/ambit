# 26 — Move selected content into an unloaded destination

**What to build:** Let MoveSelected alone move content canonically into an Unloaded destination without loading it, while the resident projection removes the visible source, reports the destination, and follows canonical Undo and Redo actions.

**Blocked by:** 18 — Synchronize a resident projection safely; 25 — Guard structural commands at unloaded boundaries.

**Status:** ready-for-agent

- [ ] MoveSelected into an Unloaded destination submits the complete canonical move without requesting or installing that destination's Workspace.
- [ ] Projected application removes the source edge, skips insertion into the Unloaded destination list, and makes content with no remaining resident occurrence disappear from the current projection.
- [ ] The fully resident server applies and records both source removal and destination insertion as one canonical move.
- [ ] Normal command feedback names the destination even when the moved content disappears from the resident projection.
- [ ] An explicit Undo HistoryAction restores the projected source and removes the hidden canonical destination without loading it; an explicit Redo removes the projected source again.
- [ ] Structural move commands other than MoveSelected remain guarded when an Unloaded destination would require retaining or focusing the moved content.
