# 16 — Persist/git work-tree gate

**Type:** grilling
**Status:** done
Blocked by: [10 — git Save is commit then push](10-git-save-commit-then-push.md)
Actual: 10m

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
- 2026-09-28: The later model in [17 — Git Load: Unparsed then Parse stack](17-post-pull-cascade-and-gate-handoff.md)–[19 — Parsed/Unparsed and Persisted/Unpersisted](19-file-newer-graph-newer.md) does not mention this exclusive gate. This ticket’s 2026-09-26 answer is not revoked. Whether the gate still stands beside Unparsed / Unpersisted is open ([[../map.md]] Not yet specified).

## Comments

- 2026-09-26: Alan locked in chat. Status `done`.
- 2026-09-28: Reconciling-as-gate naming withdrawn. 2026-09-26 gate lock left as-is; standing beside the two-axis model is open.

## Time

- 2026-09-26 5m — recorded lock from chat
- 2026-09-28 5m — stripped Reconciling rename; noted gate standing is open
