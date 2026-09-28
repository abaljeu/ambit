# 16 — Persist/git work-tree gate

**Type:** grilling
**Status:** done
Blocked by: [10 — git Save is commit then push](10-git-save-commit-then-push.md)
Actual: 5m

## 1. Question

- [x] How do Graph→file Persist and Peer Actor git operations avoid changing the same Workspace work tree at the same time?

## 2. Answer

Locked 2026-09-26 (Alan, Github Sync room).

Each Workspace work tree has one exclusive gate shared by Graph→file Persist, git Load pull, and git Save commit.

The second caller queues behind the holder until the gate is free. Contention waits; it does not reject as busy.

Persist stays independent of git Save. The gate coordinates their work-tree changes; it does not merge Persist into Save. git Save remains commit, then push.

Map gist: [[../map.md]] Decisions so far item 16.

## Notes

- The implementation belongs with [12 — Run the Workspace git tracked-branch round-trip](12-workspace-git-tracked-branch-round-trip.md) and [13 — Run git Load and Save through the Server Peer Actor](13-peer-actor-runs-git-load-save.md); this decision does not create another coding ticket.
- 2026-09-28: [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md) and [18 — Parse Actor stack and file-lock ownership](18-parse-actor-stack-and-file-lock-ownership.md) refine the post-pull cascade after this exclusive gate. This ticket’s locked answer is unchanged. The exclusive gate still covers the pull itself; Persist queues while pull holds the workspace gate. Fine locks and parse must not hold this exclusive gate across a long parse; a later git Load pull proceeds.

## Time

- 2026-09-26 5m — recorded lock from chat
