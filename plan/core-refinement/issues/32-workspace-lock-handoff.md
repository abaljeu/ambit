# 32 — Workspace lock handoff

**Status:** `defined`
**Type:** coding
**Blocked by:** 30 — Expand the workspace lock
**Trackers:** architecture

## Context

The architecture keeps Workspace lock handoff as its own beat under Parse stack Migrate. The beat starts after 30 — Expand the workspace lock stands the workspace lock. 31 — Migrate onto the workspace lock stays a separate beat. That migrate moves Persist, parse-thread file use, and git Load and git Save onto the lock while `withWorkTreeGate` still runs. This ticket is the landing order. 02 — Git Load: Unparsed then Parse stack names that order. This ticket builds it. 07 — Git changed list sets Unparsed owns the File Nodes and Directory Nodes that the changed list marks. This ticket runs that mark inside the landing order.

## What to build

After the workspace lock drains, pull or Upload lands. Arrived files are marked Unparsed. The lock releases. Unparsed starts the parse thread.

### Workspace lock handoff

1. [ ] **Drain** — The workspace lock drains in-flight member file use before pull or Upload lands.
2. [ ] **Land** — Pull or Upload then lands the files.
3. [ ] **Mark Unparsed** — Arrived files are marked Unparsed. The git changed list supplies that mark. The changed-list rules stay on 07 — Git changed list sets Unparsed.
4. [ ] **Release** — The workspace lock releases after that mark.
5. [ ] **Parse thread** — Unparsed starts the parse thread. Core pushes the work onto the Parse stack.

### Obligations

1. [ ] **Parse and persist claim** — The Parse and persist claim says Unparsed starts the parse thread after the workspace lock releases.
2. [ ] **Landing test** — A test runs the order for pull: drain, land, mark Unparsed, release, and the parse thread starts. A test runs that same order for Upload.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement](plan/core-refinement/project.md)
- [02 — Git Load: Unparsed then Parse stack](plan/core-refinement/issues/02-git-load-unparsed-then-parse-stack.md)
- [07 — Git changed list sets Unparsed](plan/core-refinement/issues/07-git-changed-list-unparsed.md)
- [30 — Expand the workspace lock](plan/core-refinement/issues/30-expand-the-workspace-lock.md)
- [31 — Migrate onto the workspace lock](plan/core-refinement/issues/31-migrate-onto-the-workspace-lock.md)
- [Parse and persist](doc/current/parse-persist.md)
