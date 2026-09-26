# 02 — Lock bootstrap visible-closure set

**Type:** grilling
**Status:** needs-info
**Blocked by:** None

## 1. Question

What is the exact first-paint Resident set, and how does Zoom restore interact with it?

Bootstrap is Zoom-scoped visible-closure, not a complete Workspace. It uses the same edges-plus-Nodes package shape as ongoing wants. The locked set includes Children of reserved Nodes ROOT, TRASH, Workspaces Node, and SYSTEM (that SYSTEM spelling), ancestors of the Zoom root, and Included.

Lock:

1. **Reserved Children** — Does first paint always include every direct Child of ROOT, TRASH, Workspaces Node, and SYSTEM, even when those Children sit outside Included?
2. **Zoom ancestors** — Which ancestors of the restored Zoom root must be Resident and Loaded so the framing path is present?
3. **Zoom restore** — When saved Zoom is missing, stale, or outside the reserved set, what Resident set remains? Does restore widen bootstrap beyond Included plus reserved Children plus ancestors, or does it only choose the Zoom root inside that set?
