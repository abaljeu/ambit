# 07 — Git changed list sets Unparsed

**Type:** coding
**Status:** defined
**Blocked by:** None — can start immediately

## Context

A pull lands files under the workspace lock. [02 — Git Load: Unparsed then Parse stack](02-git-load-unparsed-then-parse-stack.md) already locks the order. Take the workspace lock, drain, pull, mark Unparsed, release, then Unparsed starts the parse thread. The mark step says arrived files. This ticket names that mark. The source is the git changed list. The nodes are File Nodes and Directory Nodes. The order stays [core-refinement architecture](plan/core-refinement/arch.md) §6. This ticket does not reopen that sequence.

Directory reconcile stays the other approach. It is [07 — Directory Unparsed during reconcile](plan/parse-thread/issues/07-directory-unparsed-during-reconcile.md). Both stay.

## What to build

After the pull, Core takes the git changed list and immediately marks each matching File Node and Directory Node Unparsed. The list is the one `WorkspaceGit.changedPathsBetween` already returns. The mark is the mark step in [core-refinement architecture](plan/core-refinement/arch.md) §6 **Workspace lock**, **Drain then pull**. It runs before the lock releases. A changed path that matches a File Node marks that File Node. A changed path that matches a Directory Node marks that Directory Node. A modified file does not mark ancestor Directory Nodes. This ticket does not add a second axis path. The writer stays [core-refinement architecture](plan/core-refinement/arch.md) §5 **git pull finish**.

### 1. Git changed list

The mark uses the git changed list from the pull.

1. [ ] Changed list — The mark reads the git changed list from the pull. That list is `WorkspaceGit.changedPathsBetween`.
2. [ ] Immediate — Matching nodes are Unparsed in that mark step, before the workspace lock releases.
3. [ ] Sequence — The order stays take, drain, pull, mark, release, then Unparsed starts the parse thread. This ticket does not reorder [core-refinement architecture](plan/core-refinement/arch.md) §6.

### 2. Matching nodes

The list marks File Nodes and Directory Nodes.

1. [ ] File Node — A changed path that matches a File Node marks that File Node Unparsed.
2. [ ] Directory Node — A changed path that matches a Directory Node marks that Directory Node Unparsed.
3. [ ] No ancestors — A modified file does not mark ancestor Directory Nodes.
4. [ ] Both approaches — This ticket does not remove Directory reconcile. [07 — Directory Unparsed during reconcile](plan/parse-thread/issues/07-directory-unparsed-during-reconcile.md) stays.

## See also

[core-refinement map](plan/core-refinement/map.md)

[02 — Git Load: Unparsed then Parse stack](02-git-load-unparsed-then-parse-stack.md)

[core-refinement architecture](plan/core-refinement/arch.md)
