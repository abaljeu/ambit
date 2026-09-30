# 05 — Selection-scoped Parse after whole-tree git Load

**Type:** grilling
**Status:** done
Blocked by: [03 — One Parse actor stack](03-one-parse-actor-stack.md)
Actual: 10m

## 1. Question

- [x] After a Workspace-scoped git pull (whole work tree / tracked branch), how does Load run Parse on the current selection?

## 2. Answer

Locked 2026-09-28 (Alan, chat).

The Client runs Load on a file node (the selection). Selected nodes are pushed onto the one Parse actor’s stack. Parse processes them in the normal course of stack processing.

Git Load may still have work it triggered. That does not matter. Selection does not need a special path or priority scheme beyond push-on-stack.

See [03 — One Parse actor stack](03-one-parse-actor-stack.md). Whole-tree git stays [05 — Git Load/Save are Workspace-scoped](../../github-transport/issues/05-git-load-save-workspace-scoped.md).

Map gist: [[../map.md]] Decisions so far item 5.

## Notes

- This lock names who pushes the selection. It does not invent a second Parse actor or a priority queue.
- Formerly github-transport issue 06. Moved to [[plan/core-refinement/project.md]] on 2026-09-29.

## Comments

- 2026-09-26: Filed later. Status `needs-info`. Not the v1 implement frontier.
- 2026-09-28: Alan locked push-on-stack. Status `done`.
- 2026-09-29: Moved from github-transport into core-refinement as [05 — Selection-scoped Parse after whole-tree git Load](05-selection-scoped-parse-after-whole-tree-git-load.md).

## Time

- 2026-09-26 5m — stubbed later ticket from chat
- 2026-09-28 5m — recorded selection push-on-stack lock from chat
