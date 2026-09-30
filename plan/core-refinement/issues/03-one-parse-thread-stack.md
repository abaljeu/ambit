# 03 — One Parse actor stack

**Type:** grilling
**Status:** done
Blocked by: [02 — Git Load: Unparsed then Parse stack](02-git-load-unparsed-then-parse-stack.md)
Actual: 30m

## 1. Question

- [x] How many Parse actors exist, and who pushes work onto them?
- [x] What does Parse do on a Workspace, Directory, or File Node?

## 2. Answer

Locked 2026-09-28 (Alan, chat).

There is **one long-lived Parse actor**. Nobody starts an Actor after pull. Core **pushes a reconcile target** onto that actor’s stack. Others may also push onto the same stack (the client can get certain things updated sooner).

**Parse** = convert this disk object → graph. Same interface for Workspace, Directory, and File. Implementation differs by kind.

Workspace/directory Parse: reconcile **immediate members only**. Then set **Unparsed** on children that need updating, and mark this node **Parsed** (as a change event). File Parse converts that file’s disk object into graph content.

Parse Actor home: [[plan/parse-actor/project.md]]. Persist is **not** a second Actor. Persist is an async persisting task on Core: [04 — Parsed/Unparsed and Persisted/Unpersisted](04-parsed-unparsed-and-persisted-unpersisted.md).

Git Load and Upload handoff: [02 — Git Load: Unparsed then Parse stack](02-git-load-unparsed-then-parse-stack.md). Selection Load: [05 — Selection-scoped Parse after whole-tree git Load](05-selection-scoped-parse-after-whole-tree-git-load.md).

Map gist: [[../map.md]] Decisions so far item 3.

## Notes

- Do not invent parse-actor tickets on this lock. The Parse Project stays at [[plan/parse-actor/project.md]].
- This lock names the stack and who pushes. It does not start a new Actor per pull.
- Formerly github-transport issue 18. Moved to [[plan/core-refinement/project.md]] on 2026-09-29.

## Comments

- 2026-09-28: Alan locked one long-lived Parse actor. Core pushes reconcile targets. Status `done`. Directory-reconcile worker and “hold Unparsed” language withdrawn.
- 2026-09-28: Persist is a Core async stack task, not a Persist actor.
- 2026-09-29: Moved from github-transport into core-refinement as [03 — One Parse actor stack](03-one-parse-actor-stack.md).

## Time

- 2026-09-28 5m — recorded lock from chat
- 2026-09-28 5m — recorded follow-up locks from chat
- 2026-09-28 5m — recorded Unparsed Persist-block clarification from chat
- 2026-09-28 5m — aligned file lock to Unparsed and directory lock to Reconciling from chat
- 2026-09-28 5m — rewrote to one long-lived Parse actor from chat
- 2026-09-28 5m — Persist is Core async task, not an Actor
