# 18 — Parse Actor stack and file-lock ownership

**Type:** grilling
**Status:** done
Blocked by: [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md)
Actual: 15m

## 1. Question

- [x] Who holds file fine locks after git Load pull, and how do parse requests reach Parse?
- [x] May Parse serialize the exclusive Workspace gate across a long parse?
- [x] Does Parse take directory locks or reverse parent-then-child acquire order?
- [x] Does Parse’s file lock also block Persist, or is that Unparsed?

## 2. Answer

Locked 2026-09-28 (Alan, chat).

One Parse Actor with a stack of files to parse. Anybody may push a request onto that stack (the client can get certain things updated sooner).

File fine locks are held by the Parse Actor. Unlock when that file’s Graph update is done. Parse holds only file locks.

Directory fine locks stay on the directory-reconcile worker: [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md).

Parse Actor home: [[plan/parse-actor/project.md]]. This lock is filed here because fine locks are the post-pull handoff from [16 — Persist/git work-tree gate](16-persist-git-work-tree-gate.md).

Follow-up locked 2026-09-28 (Alan, chat).

Parse holding file locks must not hold or serialize the Workspace exclusive gate across the whole parse. A later git Load pull proceeds. See [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md).

The correct pipeline is github → file → parse → (merge) graph → file.

Deadlock rule: Parse holds only file locks. It does not take directory locks. Acquire only down the tree — lock parent, then children, never child-then-parent. Nobody takes a second lock while holding one that would reverse that order.

Clarification locked 2026-09-28 (Alan, chat).

The Parse stack and file fine locks coordinate concurrency (who Parses which file; later git Load pulls still proceed). They are not a second Persist-block. Don’t Graph→file Persist over a path that still needs Parse: that intent is the Unparsed marker. See [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md).

Map gist: [[../map.md]] Decisions so far item 18.

## Notes

- Do not invent parse-actor tickets on this lock. The Parse Project stays at [[plan/parse-actor/project.md]].
- This lock names the stack and file-lock ownership. It does not redesign Load around a future autonomous Parse. Load’s v1 Parse coupling stays until that Project builds the Actor.

## Comments

- 2026-09-28: Alan locked in chat. Status `done`. One Parse Actor stack; anybody may push; Parse holds file locks.
- 2026-09-28: Alan follow-up. Long parse must not block later git Load pulls. Pipeline github → file → parse → (merge) graph → file. Parse does not take directory locks or reverse acquire order.
- 2026-09-28: Alan clarification. Parse file locks are concurrency. Anti-Persist is Unparsed.

## Time

- 2026-09-28 5m — recorded lock from chat
- 2026-09-28 5m — recorded follow-up locks from chat
- 2026-09-28 5m — recorded Unparsed Persist-block clarification from chat
