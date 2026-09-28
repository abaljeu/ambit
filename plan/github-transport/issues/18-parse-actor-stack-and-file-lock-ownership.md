# 18 — Parse Actor stack and file-lock ownership

**Type:** grilling
**Status:** done
Blocked by: [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md)
Actual: 5m

## 1. Question

- [x] Who holds file fine locks after git Load pull, and how do parse requests reach Parse?

## 2. Answer

Locked 2026-09-28 (Alan, chat).

One Parse Actor with a stack of files to parse. Anybody may push a request onto that stack (the client can get certain things updated sooner).

File fine locks are held by the Parse Actor. Unlock when that file’s Graph update is done. Parse holds only file locks.

Directory fine locks stay on the directory-reconcile worker: [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md).

Parse Actor home: [[plan/parse-actor/project.md]]. This lock is filed here because fine locks are the post-pull handoff from [16 — Persist/git work-tree gate](16-persist-git-work-tree-gate.md).

Map gist: [[../map.md]] Decisions so far item 18.

## Notes

- Do not invent parse-actor tickets on this lock. The Parse Project stays at [[plan/parse-actor/project.md]].
- This lock names the stack and file-lock ownership. It does not redesign Load around a future autonomous Parse. Load’s v1 Parse coupling stays until that Project builds the Actor.

## Comments

- 2026-09-28: Alan locked in chat. Status `done`. One Parse Actor stack; anybody may push; Parse holds file locks.

## Time

- 2026-09-28 5m — recorded lock from chat
