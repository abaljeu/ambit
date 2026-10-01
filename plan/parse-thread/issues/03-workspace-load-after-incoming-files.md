# 03 — Workspace Load after incoming files

**Type:** grilling
**Status:** defined
Blocked by: None

## 1. Question

After transport lands files, how does Workspace-level reconcile interact with Directory reconcile and File Parse?

[02 — Git Load: Unparsed then Parse stack](../../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md) is the git-pull handoff. Core pushes a Workspace reconcile target after files land. The immediate-members sentence in that ticket is imprecise if over-read. See [01 — Directory Parse body home](01-directory-parse-body-home.md).

Lock:

1. **Workspace target** — What does the parse thread do when the reconcile target is the Workspace Node?
2. **Children** — What does that pass push for child Directory Nodes and File Nodes?
3. **Directory Load** — How does that Workspace pass interact with a Directory Load that uses structure-match? See [02 — Structure-match Directory Load](02-structure-match-directory-load.md).

The workspace lock, the pull order, and GitHub pull mechanics stay out of this question. See [[../map.md]] Out of scope.

## Comments

- 2026-10-01 — Filed from the directory/Load chart. Status `defined`. Open for grilling.
