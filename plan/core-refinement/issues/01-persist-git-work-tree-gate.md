# 01 — Persist/git work-tree gate

**Type:** grilling
**Status:** done
Blocked by: [10 — git Save is commit then push](../../github-transport/issues/10-git-save-commit-then-push.md)
Actual: 15m

## 1. Question

- [x] How do Graph→file Persist and Peer Actor git operations avoid changing the same Workspace work tree at the same time?
- [x] Does the exclusive Persist/git work-tree gate still stand beside Unparsed / Unpersisted?

## 2. Answer

Locked 2026-09-26 (Alan, Github Sync room). **Revoked 2026-09-28** (Alan, chat).

The exclusive Persist/git work-tree gate does **not** still stand as the lasting protocol. Axes (Unparsed / Unpersisted) are drift markers; they are not a lock table that replaces that gate. Current protocol is the workspace lock and per-member persist locks: [[../arch.md]] §6 Core locking model. Axes: [04 — Parsed/Unparsed and Persisted/Unpersisted](04-parsed-unparsed-and-persisted-unpersisted.md).

Code still runs `WorkspaceGit.withWorkTreeGate` on Persist and git paths. That is not permission to keep the exclusive gate, and it is not permission to skip standing up the workspace lock. Catch-up is [[../arch.md]] §3 step 4 **§6 locks catch-up**: Expand stands the workspace lock and per-member persist locks beside the gate; Migrate moves Persist, parse-thread file use, and git Load/Save onto §6; Contract removes `withWorkTreeGate` once §6 is the only protocol.

git Save remains commit then push ([10 — git Save is commit then push](../../github-transport/issues/10-git-save-commit-then-push.md)). Persist stays independent of git Save. git Save is permitted while nodes are Unparsed or Unpersisted.

History: 2026-09-26 named one exclusive gate per Workspace work tree shared by Persist, git Load pull, and git Save commit. That gate is not the lasting protocol. The 2026-09-28 line “Unparsed / Unpersisted replace it” is superseded by the 2026-09-30 locking model on [[../arch.md]] §6.

Map gist: [[../map.md]] Decisions so far item 1.

## Notes

- Persist is a Core async task on a stack, not this gate: [04 — Parsed/Unparsed and Persisted/Unpersisted](04-parsed-unparsed-and-persisted-unpersisted.md).
- Locks: [[../arch.md]] §6 Core locking model.
- The 2026-09-26 implement notes on [12 — Run the Workspace git tracked-branch round-trip](../../github-transport/issues/12-workspace-git-tracked-branch-round-trip.md) and [13 — Run git Load and Save through the Server Peer Actor](../../github-transport/issues/13-actor-runs-git-load-save.md) still describe the old gate in code. This ticket does not keep that gate as the protocol; remove it via [[../arch.md]] §3 step 4 **§6 locks catch-up** Contract after Expand/Migrate.
- Formerly github-transport issue 16. Moved to [[plan/core-refinement/project.md]] on 2026-09-29.

## Comments

- 2026-09-26: Alan locked the exclusive gate.
- 2026-09-28: Alan revoked the exclusive gate. Unparsed / Unpersisted replace it. Status `done`.
- 2026-09-29: Moved from github-transport into core-refinement as [01 — Persist/git work-tree gate](01-persist-git-work-tree-gate.md).
- 2026-09-30: Alan locked workspace lock + persist locks on [[../arch.md]] §6; axes stay drift markers. Answer updated.
- 2026-09-30: Alan approved Candidate A catch-up order on [[../arch.md]] §3 step 4 **§6 locks catch-up** (stand §6 beside `withWorkTreeGate`, migrate Persist/parse/git onto it, then contract the gate). Answer updated so workers do not keep the exclusive gate or skip the workspace lock.

## Time

- 2026-09-26 5m — recorded lock from chat
- 2026-09-28 5m — stripped Reconciling rename; noted gate standing is open
- 2026-09-28 5m — recorded revocation; Unparsed/Unpersisted replace the gate
