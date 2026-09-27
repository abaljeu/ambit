# 06 — Selection-scoped Parse after whole-tree git Load

**Type:** coding
**Status:** needs-info
Blocked by: None
Actual: 5m

## 1. Question

After a Workspace-scoped git pull (whole work tree / tracked branch), how does Load run Parse on the current selection?

[05 — Git Load/Save are Workspace-scoped](05-git-load-save-workspace-scoped.md) locks: git pull/push is always the whole Workspace work tree. Parse still runs on the selection where appropriate after files land (today’s Load → Parse pipeline). Alan: this nuance is a later ticket. Do not expand it in v1 coding tickets.

Grill or spec the selection-parse detail when v1 whole-tree git Load/Save is in place. Then set Status `defined` before implement.

## Comments

- 2026-09-26: Filed later. Status `needs-info`. Not the v1 implement frontier.

## Time

- 2026-09-26 5m — stubbed later ticket from chat
