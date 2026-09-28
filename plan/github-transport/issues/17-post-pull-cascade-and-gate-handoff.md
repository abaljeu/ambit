# 17 — Post-pull cascade and gate handoff

**Type:** grilling
**Status:** done
Blocked by: [16 — Persist/git work-tree gate](16-persist-git-work-tree-gate.md)
Actual: 5m

## 1. Question

- [x] After git Load pull, when does the exclusive Workspace gate release, and how do directory fine locks cascade?

## 2. Answer

Locked 2026-09-28 (Alan, chat).

After pull and fine-lock install, release the Workspace exclusive gate. Writers (including Graph→file Persist) proceed on unlocked paths and queue only when they need a still-locked path or directory.

[16 — Persist/git work-tree gate](16-persist-git-work-tree-gate.md)’s exclusive gate still covers the pull itself: Persist queues while pull holds the workspace gate.

Directory cascade from Workspace root: lock children that need work → unlock parent → process children. Simple recursive algorithm. Start by locking the Workspace root; when that work decides which immediate children need work, lock those, unlock parent, continue.

Directory fine locks are held by a directory-reconcile worker, unlocked as each directory’s Graph nodes are updated. Parse holds only file locks. File-lock ownership is [18 — Parse Actor stack and file-lock ownership](18-parse-actor-stack-and-file-lock-ownership.md).

Map gist: [[../map.md]] Decisions so far item 17.

## Notes

- This ticket refines the post-pull window after [16 — Persist/git work-tree gate](16-persist-git-work-tree-gate.md). It does not rewrite that exclusive-gate lock.
- File this on github-transport. Parse stack and file locks: [18 — Parse Actor stack and file-lock ownership](18-parse-actor-stack-and-file-lock-ownership.md). Parse Actor home: [[plan/parse-actor/project.md]].

## Comments

- 2026-09-28: Alan locked in chat. Status `done`. Release exclusive gate after pull + fine-lock install. Directory cascade. Directory locks on the reconcile worker.

## Time

- 2026-09-28 5m — recorded lock from chat
