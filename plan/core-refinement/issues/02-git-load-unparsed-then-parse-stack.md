# 02 — Git Load: Unparsed then Parse stack

**Type:** grilling
**Status:** done
Blocked by: [01 — Persist/git work-tree gate](01-persist-git-work-tree-gate.md)
Actual: 30m

## 1. Question

- [x] After git Load pull, how does work reach Parse?
- [x] Does Core push onto the Parse stack after pull?
- [x] Is Client Upload a separate pipeline?

## 2. Answer

Locked 2026-09-28 (Alan, chat). Supersedes the Reconciling cascade on this ticket.

Git Load / Core github pull (current truth: [[../arch.md]] §6 Core locking model):

1. Take the **workspace lock** (announces member files may change; waits while members are already changing; while pending, new persist locks for those members cannot be taken).
2. When current file changes have drained, **pull** files.
3. Mark arrived files **Unparsed**.
4. **Release** the workspace lock.
5. **Unparsed** starts the **parse thread** (Core pushes the Workspace onto the one long-lived Parse stack).

Nobody starts an Actor after pull. Core pushes a reconcile target onto the one long-lived Parse stack. See [03 — One Parse thread stack](03-one-parse-thread-stack.md).

Client **Upload** uses the same path when it changes member files: workspace lock → land on disk → mark the relevant node Unparsed → release → Unparsed starts the parse thread. No special Upload pipeline.

Workspace/directory Parse reconciles **immediate members only** (spot disk vs graph discrepancies, update the graph). Then set **Unparsed** on children that need updating, and mark this node **Parsed**.

Axes are Graph drift markers Core and the parse thread set and clear. They are not a lock table. Workspace lock and per-member persist locks are additional: [[../arch.md]] §6.

Map gist: [[../map.md]] Decisions so far item 2.

## Notes

- Axes and Persist: [04 — Parsed/Unparsed and Persisted/Unpersisted](04-parsed-unparsed-and-persisted-unpersisted.md).
- Parse product home: [[plan/parse-thread/project.md]].
- [01 — Persist/git work-tree gate](01-persist-git-work-tree-gate.md) exclusive gate is revoked. Locks: [[../arch.md]] §6.
- Formerly github-transport issue 17. Moved to [[plan/core-refinement/project.md]] on 2026-09-29.

## Comments

- 2026-09-28: Alan replaced the Reconciling cascade. Status `done`. Sequence was Unparsed on Workspace → pull → push Parse. Upload is the same path. No new Actor after pull.
- 2026-09-29: Moved from github-transport into core-refinement as [02 — Git Load: Unparsed then Parse stack](02-git-load-unparsed-then-parse-stack.md).
- 2026-09-30: Alan locked pull order on [[../arch.md]] §6: workspace lock → drain → pull → mark Unparsed → release → parse thread. Answer updated; the older “mark Unparsed then pull” sequence is not current truth.

## Time

- 2026-09-28 5m — recorded lock from chat
- 2026-09-28 5m — recorded follow-up locks from chat
- 2026-09-28 5m — recorded Unparsed Persist-block clarification from chat
- 2026-09-28 5m — rewrote cascade to Workspace Reconciling sequence from chat
- 2026-09-28 5m — rewrote to Unparsed-then-Parse-stack model from chat
- 2026-09-30 5m — corrected Answer wording
