# 02 — Lock bootstrap visible-closure set

**Type:** grilling
**Status:** done
**Blocked by:** None
**Actual:** 20m

## 1. Answer

Locked 2026-09-26.

1. **Reserved Children** — First paint always includes every direct Child of ROOT, TRASH, Workspaces Node, and SYSTEM (SYSTEM spelling), even when those Children sit outside Included.
2. **Zoom ancestors** — Full ancestor chain of the Zoom root up to ROOT: every ancestor is Resident, and each has `childMap` Loaded (framing path Children lists exist).
3. **Zoom restore** — If saved Zoom is missing, stale, or outside the bootstrapable set, pick a safe default Zoom root inside that set (for example ROOT or the first Workspace Child). Do not widen bootstrap beyond reserved Children + ancestors + Included.

## Comments

- 2026-09-26: Grill locked reserved Children on first paint, Zoom ancestor chain Resident with `childMap` Loaded, and Zoom restore to a safe default inside the bootstrapable set.

## Time

- 2026-09-26 20m — recorded 2026-09-26 grill locks: reserved Children, Zoom ancestors Loaded, Zoom restore default (from chat)
