# 05 — Git Load/Save are Workspace-scoped

**Type:** grilling
**Status:** done
Blocked by: None
Actual: 5m

## 1. Question

- [x] When a person invokes git Load or git Save from a Workspace root or a subnode, what git scope does pull/push use?

## 2. Answer

Locked 2026-09-26 (Alan, chat).

**Git Load/Save are Workspace-scoped.** Wherever Load/Save is invoked from (Workspace root or a subnode), git pull/push always operates on the whole Workspace work tree / tracked branch — never file-level git.

**Parse follow-up:** still run Parse on the selection where appropriate after files land (keeps today’s Load → Parse pipeline; selection-scoped parse after whole-tree pull). The selection-parse nuance is a later ticket: [06 — Selection-scoped Parse after whole-tree git Load](06-selection-scoped-parse-after-whole-tree-git-load.md). Do not expand that nuance in v1 coding tickets.

Map gist: [[../map.md]] Decisions so far item 11.

## Comments

- 2026-09-26: Alan locked in chat. Status `done`. Whole-tree git. Parse on selection after files land; nuance later on [06 — Selection-scoped Parse after whole-tree git Load](06-selection-scoped-parse-after-whole-tree-git-load.md).

## Time

- 2026-09-26 5m — recorded lock from chat
