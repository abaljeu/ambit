# 01 — Directory Parse body home

**Type:** grilling
**Status:** done
Blocked by: None
Actual: 5m

## 1. Question

Who owns the Directory Parse body, and does [02 — Git Load: Unparsed then Parse stack](../../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md) replace [core-refinement architecture](../../core-refinement/arch.md) §5 item 10?

## 2. Answer

Locked in the directory/Load discussion. Recorded 2026-10-01.

This Project owns the Directory Parse body. That body is [core-refinement architecture](../../core-refinement/arch.md) §5 item 10. It walks all nodes tied to that Directory File (`.amb`), not only immediate children. It creates missing File Nodes. When disk is newer, it marks the File Node Unparsed and pushes when the stack exists.

[02 — Git Load: Unparsed then Parse stack](../../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md) is mainly about files changed by a git pull. Its sentence that workspace/directory Parse reconciles immediate members only is imprecise if over-read. That sentence stays the git-pull handoff. §5 item 10 stays the Directory Parse body. The two stand together.

This ticket does not reopen the Core seam.

Map gist: [[../map.md]] Decisions so far item 1.

## Comments

- 2026-10-01 — Recorded from the directory/Load discussion. Status `done`.

## Time

- 2026-10-01 5m — recorded the directory/Load lock (from chat)
