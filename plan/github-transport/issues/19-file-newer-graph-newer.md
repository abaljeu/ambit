# 19 — Parsed/Unparsed and Persisted/Unpersisted

**Type:** grilling
**Status:** done
Blocked by: [17 — Git Load: Unparsed then Parse stack](17-post-pull-cascade-and-gate-handoff.md)
Actual: 10m

## 1. Question

- [x] What Graph state do special nodes carry for Parse and Persist?
- [x] Is there a conflicted DocumentState when both sides changed?

## 2. Answer

Locked 2026-09-28 (Alan, chat).

Special nodes (Workspace, Directory, File) each carry two independent axes Core sets:

- **Parsed | Unparsed**
- **Persisted | Unpersisted**

**Parse** = convert this disk object → graph. **Persist** = convert this graph → disk. Same interface for all three kinds; implementation differs by type.

Land on disk (git pull, Upload, or any other disk change) → set **Unparsed** on the relevant node → push onto Parse. Parse clears Unparsed by marking the node **Parsed**.

Any **graph edit** sets its owning special node **Unpersisted**. Persist later writes that node to disk and clears Unpersisted.

Do not invent a Conflicted state. The two axes are independent markers, not a lock table.

`DocumentState` today is `Current` | `Unparsed` | `NoServerFile` ([[src/Shared/Model.fs]]). Parsed is the other pole of Unparsed (`Current` is today’s name). Unpersisted is new. That is implement.

Map gist: [[../map.md]] Decisions so far item 19.

## Notes

- Git Load / Upload handoff: [17 — Git Load: Unparsed then Parse stack](17-post-pull-cascade-and-gate-handoff.md).
- Parse actor: [18 — One Parse actor stack](18-parse-actor-stack-and-file-lock-ownership.md).
- File Newer / Graph Newer was an earlier formulation. Unparsed is disk-newer. Unpersisted is graph-newer. Do not keep a third Conflicted value.

## Comments

- 2026-09-28: Alan locked the two axes. Status `done`. No Conflicted. No Reconciling.

## Time

- 2026-09-28 5m — recorded File Newer / Graph Newer formulation from chat
- 2026-09-28 5m — rewrote to Parsed/Unparsed and Persisted/Unpersisted axes from chat
