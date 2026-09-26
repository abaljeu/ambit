# 02 — Lock bootstrap visible-closure set

**Type:** grilling
**Status:** done
**Blocked by:** None
**Actual:** 10m

## 1. Question

What is the exact first-paint Resident set, and where is the later Fold-aware set computed?

Bootstrap is a small Zoom-scoped visible-closure, not a complete Workspace. It uses the same edges-plus-Nodes package shape as ongoing Wants. The locked set includes Children of reserved Nodes ROOT, TRASH, Workspaces Node, and SYSTEM (that SYSTEM spelling), the ancestor path to saved Zoom, and the Zoom root's Children.

Lock:

1. **Reserved Children** — Does first paint always include every direct Child of ROOT, TRASH, Workspaces Node, and SYSTEM, even when those Children sit outside Included?
2. **Zoom path** — Which ancestors of saved Zoom must be Resident and Loaded so the framing path is present?
3. **Missing Zoom** — When saved Zoom is missing or stale, what Resident set remains?

## Answer

1. **Saved Zoom only** — The existing `/state` Zoom query scopes the initial Server projection. It is not the residency Want and does not tell the Server which Nodes are Included.
2. **Exact visible-closure** — First paint includes Loaded ROOT, TRASH, Workspaces Node, and SYSTEM with every direct Child Resident; each ancestor on the Zoom path Loaded with the next path header Resident; and the Zoom root Loaded with every direct Child Resident.
3. **Zoom fallback** — Missing or stale saved Zoom falls back to ROOT. The fallback does not widen bootstrap to a complete Workspace.
4. **One package** — Bootstrap uses the same edges-plus-Nodes shape as later Want answers. It does not send a complete Workspace.
5. **Browser computes the set** — Fold restore stays in Browser session state after State loads. As with Agent Actor `graphIds`, the Browser uses its ViewModel to compute the set: here `Want.compose` produces `want`. The Server answers that list; it does not derive Included.

## Time

- 2026-09-26 10m — accepted bootstrap base and corrected the ViewModel-derived Want ownership (from chat)
