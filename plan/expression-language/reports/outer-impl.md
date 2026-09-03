# OUTER implementation

Issue [[../issues/28-outer-prefix-combinator.md]] is done on `w/tree2-semantics`. Catalog spelling is `OUTER` (capitals, same class as `NOT`). Working name `tree2` stays history only in [[tree2-semantics.md]]. No commit. No remotes.

## Behavior

`OUTER` is a prefix combinator with the same attach as `NOT`. It is reserved, so it is not bind. Bare `OUTER` is a missing-operand parse error, same as bare `NOT`. Compound `AND` operands need parentheses.

The walk is the locked algorithm in [[../spec.md]] chapter 6: Owned, depth-first, strictly below the input. At each Node N, if `E⟦inner⟧ N` is nonempty then yield N and do not visit descendants of N; else recurse on Owned Children of N. Unloaded is a miss and is never Loaded. The walk does not follow Ref. `tree` / `**` is unchanged. There is no post-pass prune and no sugar `OUTER "blue"`.

`re` / `rei` stay Header filters. They work as `OUTER` operands because the operand is any predicate.

## Code

- [[src/Shared/ExprPathClusterTypes.fs]] — `Expr.Outer`
- [[src/Shared/ExprParse.fs]] — token `OUTER`, same prefix family as `NOT`
- [[src/Shared/ExprWalk.fs]] — `outerAnswers` (fused prune-during-accept)
- [[src/Shared/ExprCompile.fs]] — compile and type `OUTER`; reserve the word
- [[src/Shared/ExprRun.fs]] — `OUTER` is not a Run `Name=` token

## Tests

Focused Shared tests passed: all `Expr*` (146), including [[tests/Shared.Tests/ExprCombinatorTests.fs]], [[tests/Shared.Tests/ExprChapter11Tests.fs]], and [[tests/Shared.Tests/ExprFilterTests.fs]] (`re` / `rei`). Client compile gate `./scripts/client.sh build` passed.

## Spec and glossary

[[../spec.md]] chapters 3, 4, 5, 6, 7, 9, 10, and 11 now spell `OUTER`. [[CONTEXT.md]] glossary entry is `OUTER`.

## Board mutations for the parent

- `remove` [[../issues/28-outer-prefix-combinator.md]] — implement `OUTER` per spec (fused Owned walk); completed
- `add` [[outer-impl.md]] — HITL: Run `= root OUTER containing "…"` on `/ambit` or `/ambit?debug=1`; confirm prune of nested matches, Owned-only walk, and that lowercase `outer` is not the combinator
