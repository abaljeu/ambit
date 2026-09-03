# 31 — Prefix combinator `IF` (same-input pullback)

**Context:** Yield the input Answer when the operand yields any Answer from it; otherwise miss. User spelling `IF` (capitals, same class as `NOT` and `OUTER`). Independent of text operations: `IF` is useful with today's catalog (`IF child` keeps a Node that has Children; `NOT (NOT e)` is the same emptiness inversion). Work on branch `w/tree2-semantics`.

**What to build:** Parse and evaluate `IF` per [[../spec.md]] chapters 4, 6, and 7. Same parse family as `NOT` / `OUTER` (prefix, reserved, not bind). Bare `IF` is a missing-operand parse error. Compound operands need parentheses. Do not implement [[30-text-operations.md]]. Do not replace `tree`. Do not post-pass prune. Do not redefine `containing` / `re` / `rei`.

**Blocked by:** none. Not blocked by [[30-text-operations.md]]. [[28-OUTER-prefix-combinator.md]] is done.

**See also:** [[../spec.md]] chapters 4, 6, 7, and 11; [[src/Shared/ExprEval.fs]] `notEval` / `ifEval`; [[src/Shared/ExprParse.fs]] prefix attach. Tests belong next to existing combinator facts in [[tests/Shared.Tests/ExprCombinatorTests.fs]] and the chapter 11 row in [[tests/Shared.Tests/ExprChapter11Tests.fs]]. Report: [[../reports/if-impl.md]].

**Status:** ready-for-human

- [x] `IF containing "blue"` and `root IF containing "blue"` parse as the combinator, not bind. Lowercase `if` is not the combinator.
- [x] Compound operands need parentheses, same as `NOT`. Bare `IF` is a missing-operand parse error.
- [x] `IF child` keeps a Node that has Children; `IF containing "blue"` keeps the input Node when the Header matches.
- [x] `NOT (NOT e)` is the same function as `IF e` under current `NOT` semantics (test oracle). No oracle gap.
- [x] `OUTER`, `re`, `rei`, and `containing` are unchanged.
- [ ] HITL: Run `= … IF containing "…"` on `/ambit` or `/ambit?debug=1`; confirm Answers stay Nodes (not an inner stream), and that lowercase `if` is not the combinator.

## Comments

- 2026-09-02: Parked from WORK.md. Implementation stays done; HITL remains. Report: [[../reports/if-impl.md]].
