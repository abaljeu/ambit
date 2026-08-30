# 28 — Prefix combinator `OUTER` (fused Owned walk)

**Context:** User locked Design A on 2026-08-29 ("plan A"). Catalog spelling is `OUTER` (capitals, same class as `NOT`; override of the earlier working name `outer`). `OUTER` is a prefix combinator in the same parse family as `NOT`, not a generator row and not a `tree` replacement. The walk fuses the operand: at each Owned child N, if the operand is nonempty on N then yield N and do not visit descendants of N, else recurse on Owned Children of N. Work on branch `w/tree2-semantics` (cut from `selective-client-sync`).

**What to build:** Parse and evaluate `OUTER` per [[.scratch/expression-language/spec.md]] chapters 4, 6, and 7. Reserve `OUTER` so it is not bind. Attach like `NOT`. Fuse the predicate into an Owned walk strictly below the input (same start as `tree`). Unloaded is a miss and is never Loaded. Do not replace `tree` / `**`. Do not implement a post-pass prune. Do not add sugar `OUTER "blue"` or a Ref analog.

**Blocked by:** none.

**See also:** [[.scratch/expression-language/spec.md]] chapters 4, 6, 7, and 11; [[.scratch/expression-language/reports/tree2-semantics.md]]; [[.scratch/expression-language/reports/outer-spec-lock.md]]; [[src/Shared/ExprParse.fs]]; [[src/Shared/ExprEval.fs]] `notEval`; [[src/Shared/ExprWalk.fs]] `treeAnswers`; [[src/Shared/ExprCompile.fs]]. Tests belong next to existing Expr facts in [[tests/Shared.Tests/ExprCombinatorTests.fs]] and the chapter 11 row in [[tests/Shared.Tests/ExprChapter11Tests.fs]].

**Status:** done

- [x] `OUTER containing "blue"` and `root OUTER containing "blue"` parse as the combinator, not bind of a generator named `OUTER`.
- [x] Compound operands need parentheses, same as `NOT`. Bare `OUTER` is a missing-operand parse error.
- [x] A match yields and its Owned descendants are not visited; a non-match does not yield and the walk continues in its Owned Children; sibling matches both yield; a match under a non-match yields.
- [x] The walk is Owned only, strictly below the input, and does not follow Ref. Unloaded is a miss, never Loaded.
- [x] `tree` / `**` is unchanged. Evaluation is the fused walk, not a post-pass prune of `tree` Answers.
