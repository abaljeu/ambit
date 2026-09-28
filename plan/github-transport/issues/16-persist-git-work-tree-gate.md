# 16 — Persist/git work-tree gate

**Type:** grilling
**Status:** done
Blocked by: [10 — git Save is commit then push](10-git-save-commit-then-push.md)
Actual: 15m

## 1. Question

- [x] How do Graph→file Persist and Peer Actor git operations avoid changing the same Workspace work tree at the same time?
- [x] Does the exclusive Persist/git work-tree gate still stand beside Unparsed / Unpersisted?

## 2. Answer

Locked 2026-09-26 (Alan, Github Sync room). **Revoked 2026-09-28** (Alan, chat).

The exclusive Persist/git work-tree gate does **not** still stand. Unparsed / Unpersisted replace it. See [19 — Parsed/Unparsed and Persisted/Unpersisted](19-file-newer-graph-newer.md).

git Save remains commit then push ([10 — git Save is commit then push](10-git-save-commit-then-push.md)). Persist stays independent of git Save. git Save is permitted while nodes are Unparsed or Unpersisted.

History: 2026-09-26 named one exclusive gate per Workspace work tree shared by Persist, git Load pull, and git Save commit. That gate is not current truth.

Map gist: [[../map.md]] Decisions so far item 16.

## Notes

- Persist is a Core async task on a stack, not this gate: [19 — Parsed/Unparsed and Persisted/Unpersisted](19-file-newer-graph-newer.md).
- The 2026-09-26 implement notes on [12 — Run the Workspace git tracked-branch round-trip](12-workspace-git-tracked-branch-round-trip.md) and [13 — Run git Load and Save through the Server Peer Actor](13-peer-actor-runs-git-load-save.md) still describe the old gate in code. This ticket no longer requires that gate.

## Comments

- 2026-09-26: Alan locked the exclusive gate.
- 2026-09-28: Alan revoked the exclusive gate. Unparsed / Unpersisted replace it. Status `done`.

## Time

- 2026-09-26 5m — recorded lock from chat
- 2026-09-28 5m — stripped Reconciling rename; noted gate standing is open
- 2026-09-28 5m — recorded revocation; Unparsed/Unpersisted replace the gate
