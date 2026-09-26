# 02 — Lock bootstrap visible-closure set

**Type:** grilling
**Status:** done
**Blocked by:** None
**Actual:** 10m

## 1. Question

What is the exact first-paint Resident set, and how does Zoom restore interact with it?

Bootstrap is Zoom-scoped visible-closure, not a complete Workspace. It uses the same edges-plus-Nodes package shape as ongoing wants. The locked set includes Children of reserved Nodes ROOT, TRASH, Workspaces Node, and SYSTEM (that SYSTEM spelling), ancestors of the Zoom root, and Included.

Lock:

1. **Reserved Children** — Does first paint always include every direct Child of ROOT, TRASH, Workspaces Node, and SYSTEM, even when those Children sit outside Included?
2. **Zoom ancestors** — Which ancestors of the restored Zoom root must be Resident and Loaded so the framing path is present?
3. **Zoom restore** — When saved Zoom is missing, stale, or outside the reserved set, what Resident set remains? Does restore widen bootstrap beyond Included plus reserved Children plus ancestors, or does it only choose the Zoom root inside that set?

## Answer

1. **Fold-aware bootstrap request** — The bootstrap request carries the Zoom root plus Fold occurrence identifiers represented by `FoldOccurrenceSnapshot list`. Runtime `SiteId` values are not durable wire ids. The Server reconstructs the SiteMap occurrences and computes Fold-aware Included before first paint.
2. **Exact visible-closure** — First paint includes Loaded ROOT, TRASH, Workspaces Node, and SYSTEM with every direct Child Resident; each ancestor on the Zoom framing path Loaded with the next path header Resident; and every Fold-aware Included Node Resident. Folded-away descendants are not Included.
3. **Zoom fallback** — A missing or stale saved Zoom falls back to ROOT. Restore does not widen bootstrap beyond reserved Children, the fallback Zoom framing path, and Fold-aware Included.
4. **One package** — Bootstrap uses the same edges-plus-Nodes shape as later Want answers. It does not send a complete Workspace.

## Time

- 2026-09-26 10m — accepted Fold-aware bootstrap request and visible-closure set (from chat)
