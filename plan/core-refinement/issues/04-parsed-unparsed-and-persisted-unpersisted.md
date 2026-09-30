# 04 — Parsed/Unparsed and Persisted/Unpersisted

**Type:** grilling
**Status:** done
Blocked by: [02 — Git Load: Unparsed then Parse stack](02-git-load-unparsed-then-parse-stack.md)
Actual: 15m

## 1. Question

- [x] What Graph state do special nodes carry for Parse and Persist?
- [x] Is there a conflicted DocumentState when both sides changed?
- [x] Who runs Persist, and when is a file Persist blocked?
- [x] Is git Save permitted while nodes are Unparsed or Unpersisted?

## 2. Answer

Locked 2026-09-28 (Alan, chat).

Special nodes (Workspace, Directory, File) each carry two independent axes Core sets:

- **Parsed | Unparsed**
- **Persisted | Unpersisted**

**Parse** = convert this disk object → graph. **Persist** = convert this graph → disk. Same interface for all three kinds; implementation differs by type.

Land on disk (git pull, Upload, or any other disk change) → set **Unparsed** on the relevant node → push onto Parse. Parse clears Unparsed by marking the node **Parsed**.

Any **graph edit** sets its owning special node **Unpersisted** and feeds Core’s Persist stack.

Persist is an **async persisting task on Core**. It works off a **stack**. It is **not** a Persist Actor. Persisting a file is **blocked** while that node is marked **Unparsed**. After Unparsed is off, Persist writes Unpersisted nodes to disk and clears Unpersisted.

git Save is **permitted** while nodes are Unparsed or Unpersisted.

Do not invent a Conflicted state. The two axes are independent markers, not a lock table. [01 — Persist/git work-tree gate](01-persist-git-work-tree-gate.md)’s exclusive gate is revoked.

`DocumentState` today is `Current` | `Unparsed` | `NoServerFile` ([[src/Shared/Model.fs]]). Parsed is the other pole of Unparsed (`Current` is today’s name). Unpersisted is new. That is implement.

Map gist: [[../map.md]] Decisions so far item 4.

## Notes

- Git Load / Upload handoff: [02 — Git Load: Unparsed then Parse stack](02-git-load-unparsed-then-parse-stack.md).
- Parse stack: [03 — One Parse thread stack](03-one-parse-thread-stack.md).
- File Newer / Graph Newer was an earlier formulation. Unparsed is disk-newer. Unpersisted is graph-newer. Do not keep a third Conflicted value.
- Formerly github-transport issue 19. Moved to [[plan/core-refinement/project.md]] on 2026-09-29.

## Comments

- 2026-09-28: Alan locked the two axes. Status `done`. No Conflicted. No Reconciling.
- 2026-09-28: Persist is a Core async stack, fed by graph edits, blocked while Unparsed. git Save is permitted while Unparsed or Unpersisted. Exclusive gate revoked.
- 2026-09-29: Moved from github-transport into core-refinement as [04 — Parsed/Unparsed and Persisted/Unpersisted](04-parsed-unparsed-and-persisted-unpersisted.md).

## Time

- 2026-09-28 5m — recorded File Newer / Graph Newer formulation from chat
- 2026-09-28 5m — rewrote to Parsed/Unparsed and Persisted/Unpersisted axes from chat
- 2026-09-28 5m — Persist Core stack, Unparsed blocks Persist, git Save permitted
