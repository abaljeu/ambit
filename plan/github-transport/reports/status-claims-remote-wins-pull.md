# Status claims — remote-wins pull

Alan confirmed the pull works on 2026-10-14. Fast-forward-only git Load `[o]` lines are deleted. The remote-wins `[ ]` successors are `[x]`. The negative reset line is deleted. Stage and ticket Status are unchanged. No commit.

## Files changed

1. [parse-persist](../../../doc/current/parse-persist.md)
2. [github-transport architecture](../arch.md)
3. [12 — Run the Workspace git tracked-branch round-trip](../issues/12-workspace-git-tracked-branch-round-trip.md)
4. [Implement 12 — remote-wins pull](implement-12-remote-wins-pull.md) — confirmation note only. The test-lock paragraph stays.
5. [Remote-wins pull lock](remote-wins-pull-lock-2026-10-14.md) — confirmation note only. The lock narrative stays.

These files had no `[o]` fast-forward pull and no `[ ]` remote-wins pull claim, so they stay as written: [spec](../spec.md), [map](../map.md), [project](../project.md), [01 — Which Workspaces and remotes](../issues/01-which-workspace-labels-and-remotes.md), [08 — Reject UX](../issues/08-reject-ux.md), [10 — git Save is commit then push](../issues/10-git-save-commit-then-push.md), [transport-layer map](../../transport-layer/map.md), [transport-layer project](../../transport-layer/project.md).

## Example

Deleted `[o]` line:

`[o] git Load pulls the tracked remote branch of the whole Workspace work tree fast-forward-only.`

`[x]` line that replaced it:

`[x] git Load is one merge of the tracked remote branch of the whole Workspace work tree, completed by committing that merge as part of the pull.`

Also deleted, and not checked, because it only restates the merge in the negative: `[ ] That merge is not a reset to the remote tip.`

## Left alone

Unrelated `[ ]` lines in [github-transport architecture](../arch.md) stay open. Commit-before-pull, fast-forward push, `ensurePushConfig`, and network, credential, and missing-remote failures stay `[x]`.
