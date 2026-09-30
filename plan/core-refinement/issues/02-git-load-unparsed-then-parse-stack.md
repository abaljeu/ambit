# 02 — Git Load: Unparsed then Parse stack

**Type:** grilling
**Status:** done
Blocked by: [01 — Persist/git work-tree gate](01-persist-git-work-tree-gate.md)
Actual: 25m

## 1. Question

- [x] After git Load pull, how does work reach Parse?
- [x] Does anyone start a Parse Actor after pull?
- [x] Is Client Upload a separate pipeline?

## 2. Answer

Locked 2026-09-28 (Alan, chat). Supersedes the Reconciling cascade on this ticket.

Git Load / Core github pull:

1. Set **Unparsed** on the Workspace Node.
2. Pull files.
3. Push the Workspace onto the Parse actor.

Nobody starts an Actor after pull. Core pushes a reconcile target onto the one long-lived Parse actor. See [03 — One Parse actor stack](03-one-parse-actor-stack.md).

Client **Upload** uses the same path: land on disk → mark the relevant node Unparsed → push onto Parse. No special Upload pipeline.

Workspace/directory Parse reconciles **immediate members only** (spot disk vs graph discrepancies, update the graph). Then set **Unparsed** on children that need updating, and mark this node **Parsed**.

Markers are Graph state Core and the Parse actor set and clear. They are not mutexes entities “hold.”

Map gist: [[../map.md]] Decisions so far item 2.

## Notes

- Axes and Persist: [04 — Parsed/Unparsed and Persisted/Unpersisted](04-parsed-unparsed-and-persisted-unpersisted.md).
- Parse Actor home: [[plan/parse-actor/project.md]].
- [01 — Persist/git work-tree gate](01-persist-git-work-tree-gate.md) exclusive gate is revoked. Unparsed / Unpersisted replace it.
- Formerly github-transport issue 17. Moved to [[plan/core-refinement/project.md]] on 2026-09-29.

## Comments

- 2026-09-28: Alan replaced the Reconciling cascade. Status `done`. Sequence is Unparsed on Workspace → pull → push Parse. Upload is the same path. No new Actor after pull.
- 2026-09-29: Moved from github-transport into core-refinement as [02 — Git Load: Unparsed then Parse stack](02-git-load-unparsed-then-parse-stack.md).

## Time

- 2026-09-28 5m — recorded lock from chat
- 2026-09-28 5m — recorded follow-up locks from chat
- 2026-09-28 5m — recorded Unparsed Persist-block clarification from chat
- 2026-09-28 5m — rewrote cascade to Workspace Reconciling sequence from chat
- 2026-09-28 5m — rewrote to Unparsed-then-Parse-stack model from chat
