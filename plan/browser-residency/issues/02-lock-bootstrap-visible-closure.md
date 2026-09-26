# 02 — Lock bootstrap visible-closure set

**Type:** grilling
**Status:** done
**Blocked by:** None
**Actual:** 10m

## 1. Question

What is the exact first-paint Resident set, and which Browser identity keys the Server projection?

Bootstrap is Focus-scoped visible-closure, not a complete Workspace. It uses the same edges-plus-Nodes package shape as ongoing Wants. The locked set includes Children of reserved Nodes ROOT, TRASH, Workspaces Node, and SYSTEM (that SYSTEM spelling), the Focus ancestor path, and Focus Children.

Lock:

1. **Reserved Children** — Does first paint always include every direct Child of ROOT, TRASH, Workspaces Node, and SYSTEM, even when those Children sit outside Included?
2. **Focus path** — Which ancestors of Focus must be Resident and Loaded so the framing path is present?
3. **Missing Focus** — When saved Focus is missing or stale, what Resident set remains?

## Answer

1. **Focus bootstrap request** — The current `/state` request carries required `focusId`. The Server uses Focus as the key for backend projection. Fold occurrence snapshots are not on this wire.
2. **Exact visible-closure** — First paint includes Loaded ROOT, TRASH, Workspaces Node, and SYSTEM with every direct Child Resident; each ancestor on the Focus path Loaded with the next path header Resident; and Focus Loaded with every direct Child Resident.
3. **Focus fallback** — A missing or stale saved Focus falls back to ROOT. The fallback does not widen bootstrap to a complete Workspace.
4. **One package** — Bootstrap uses the same edges-plus-Nodes shape as later Want answers. It does not send a complete Workspace.
5. **Browser restore** — Zoom and Fold restore stay in Browser session state after State loads. The Browser then computes Fold-aware Included through `Want.compose`; the Server does not reconstruct Fold for bootstrap.

## Time

- 2026-09-26 10m — accepted and corrected Focus-scoped bootstrap request and visible-closure set (from chat)
