# 18 — Parse Actor stack and file-lock ownership

**Type:** grilling
**Status:** done
Blocked by: [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md)
Actual: 20m

## 1. Question

- [x] Who holds file fine locks after git Load pull, and how do parse requests reach Parse?
- [x] May Parse serialize Workspace Reconciling across a long parse?
- [x] Does Parse take directory Reconciling or reverse parent-then-child acquire order?
- [x] Is the file fine lock Unparsed, or a separate lock?

## 2. Answer

Locked 2026-09-28 (Alan, chat).

One Parse Actor with a stack of files to parse. Anybody may push a request onto that stack (the client can get certain things updated sooner).

The file fine lock **is** Unparsed on the File Node. Not a separate lock table. Parse holds that Unparsed marker and clears it when that file’s Graph update is done (`SetDocumentState` Unparsed → Current). Parse holds only file Unparsed. It does not set or clear directory Reconciling.

Directory Reconciling stays on the directory-reconcile worker: [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md).

Parse Actor home: [[plan/parse-actor/project.md]]. This lock is filed here because Unparsed is the post-pull file lock from [16 — Persist/git work-tree gate](16-persist-git-work-tree-gate.md) / [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md).

Parse holding Unparsed must not hold or serialize Workspace Reconciling across the whole parse. A later git Load pull proceeds (takes Workspace Reconciling again). See [17 — Post-pull cascade and gate handoff](17-post-pull-cascade-and-gate-handoff.md).

The correct pipeline is github → file → parse → (merge) graph → file.

Deadlock rule: acquire only down the tree — lock parent, then children, never child-then-parent. Nobody takes a second lock while holding one that would reverse that order.

Map gist: [[../map.md]] Decisions so far item 18.

## Notes

- Do not invent parse-actor tickets on this lock. The Parse Project stays at [[plan/parse-actor/project.md]].
- This lock names the stack and Unparsed ownership. It does not redesign Load around a future autonomous Parse. Load’s v1 Parse coupling stays until that Project builds the Actor.

## Comments

- 2026-09-28: Alan locked in chat. Status `done`. One Parse Actor stack; anybody may push; file lock is Unparsed; Parse does not take directory Reconciling; long parse must not block later git Load pulls.

## Time

- 2026-09-28 5m — recorded lock from chat
- 2026-09-28 5m — recorded follow-up locks from chat
- 2026-09-28 5m — recorded Unparsed Persist-block clarification from chat
- 2026-09-28 5m — aligned file lock to Unparsed and directory lock to Reconciling from chat
