# Remote-wins pull lock

Alan, Github Sync, 2026-10-14. Doc lock only. Product code stays as it is until a later implement. No commit.

## Lock

git Load is one merge of the tracked remote branch, completed by committing that merge as part of the pull, and both content conflicts and edit-versus-delete resolve to the remote action with no conflict markers; git push stays fast-forward-only.

## Layers

1. **Map** — changed. Destination and Decisions so far item 2 — Operator sets remote and credentials no longer treat the round-trip as fast-forward-only.
2. **Spec** — changed. Solution item 4 — Fast-forward push, remote-wins pull holds the procedure. Related stories and Further Notes item 8 — Remote-wins pull record the same lock.
3. **Architecture** — changed. It stated pull and push as fast-forward-only. New merge lines are unchecked because that pull is not built. Shipped fast-forward pull stays marked as shipped, not as the current required pull.
4. **Tickets** — changed where they still locked pull as fast-forward or could be read as a new Save. No new issue. [03 — When pull and push fire](../issues/03-when-pull-and-push-fire.md) stays about when Load and Save fire.
5. **Code** — unchanged.
6. **Chapter** — [Send to and from GitHub](../../roadmap/epics/chapters/send-to-and-from-github.md) unchanged. It does not say pull is fast-forward.

[transport-layer](../../transport-layer/project.md) map row said the GitHub leg was fast-forward-only. That cell and a pointer note now match this lock. Stage on [github-transport](../project.md) stays `build`.

## What the lock says

1. git push stays fast-forward-only. A conflicted or non-fast-forward push is rejected. Server `ensurePushConfig` sets `receive.denyNonFastForwards`.
2. A conflict reject names at least one file path. Other failures use a matching short error that reflects what git reports, condensed. That wording applies to Load and Save failures (network, credentials, a missing remote, and a non-fast-forward push). It does not mean pull is fast-forward.
3. git Load is one merge of the tracked remote branch, completed by committing that merge. The merge commit is part of the pull. It is not a new Save and not a new commit step before the merge. Commit-before-pull already happens.
4. Pull is not fast-forward-only and it does not replace the work tree with the remote tip.
5. A file only the Server changed stays the Server file. A file only the remote changed becomes the remote file.
6. A content conflict (both sides edited the file, or both sides added the path) resolves to the remote file, and git writes no conflict markers. Having both remote and Server edits does not prevent a clean pull.
7. An edit-versus-delete is remote-wins, the same rule as a content conflict. The Server edited the path and the remote deleted it: the merge result removes the file. The Server deleted the path and the remote edited it: the merge result is the remote file. git writes no conflict markers. The unmerged edit/delete path is resolved to that remote action so the merge commit can be created.
8. The merge commit includes the remote tip, so a later git Save can still fast-forward.
9. Network, credentials, and a missing remote still fail the pull.
10. The later parse step that merges the pulled file with the Server graph stays on [core-refinement](../../core-refinement/project.md).

## Files

1. [spec.md](../spec.md)
2. [map.md](../map.md)
3. [arch.md](../arch.md)
4. [project.md](../project.md)
5. [01 — Which Workspaces and remotes](../issues/01-which-workspace-labels-and-remotes.md)
6. [08 — Reject UX](../issues/08-reject-ux.md)
7. [10 — git Save is commit then push](../issues/10-git-save-commit-then-push.md)
8. [12 — Run the Workspace git tracked-branch round-trip](../issues/12-workspace-git-tracked-branch-round-trip.md)
9. [transport-layer map](../../transport-layer/map.md)
10. [transport-layer project](../../transport-layer/project.md)

## Later

Alan confirmed the pull works on 2026-10-14. The lock text above is the record of that earlier doc lock.
