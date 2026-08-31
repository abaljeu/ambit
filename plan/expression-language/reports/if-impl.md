# IF implementation

Issue [[../issues/31-if-pullback.md]] is done on `w/tree2-semantics`. Catalog spelling is `IF` (capitals, same class as `NOT` and `OUTER`). Independent of [[../issues/30-text-operations.md]]. No commit. No remotes.

## Behavior

`IF` is a prefix combinator with the same attach as `NOT` and `OUTER`. It is reserved, so it is not bind. Bare `IF` is a missing-operand parse error, same as bare `NOT`. Compound `AND` operands need parentheses.

`E⟦IF e⟧ x` yields `⟨x⟩` when `E⟦e⟧ x` is nonempty, otherwise `⟨⟩`. Same-input pullback. `OUTER` pullbacks while walking Owned descendants; `IF` pullbacks in place.

`NOT (NOT e)` denotes the same function under current `NOT` (emptiness only, not type). Tests use that as the oracle. There is no oracle gap. Direct `ifEval` is the inverted `notEval` emptiness test.

Today's catalog works: `IF child` keeps a Node that has Children; `IF containing "blue"` keeps the input Node. Text ops are out of this slice.

`re` / `rei` / `containing` stay Header filters. `OUTER` is unchanged.

## Code

- [[src/Shared/ExprPathClusterTypes.fs]] — `Expr.If`
- [[src/Shared/ExprParse.fs]] — token `IF`, same prefix family as `NOT` / `OUTER`
- [[src/Shared/ExprEval.fs]] — `ifEval`
- [[src/Shared/ExprCompile.fs]] — compile and type `IF`; reserve the word
- [[src/Shared/ExprRun.fs]] — `IF` is not a Run `Name=` token

## Tests

Focused Shared tests passed: `ExprCombinatorTests`, `ExprChapter11Tests`, and `ExprFilterTests` (`re` / `rei` / `containing`). Client compile gate `bash ./scripts/client.sh build` passed.

## Spec and glossary

[[../spec.md]] chapters 3, 4, 5, 6, 7, 9, and 11 now spell `IF`. [[CONTEXT.md]] glossary entry is `IF`.

## Board mutations for the parent

- `move` [[../issues/31-if-pullback.md]] Pending → Active — implement `IF` (same-input pullback); was Active while working
- `remove` [[../issues/31-if-pullback.md]] — implement `IF` per spec; completed
- `add` [[if-impl.md]] — HITL: Run `= … IF containing "…"` on `/ambit` or `/ambit?debug=1`; confirm Answers stay Nodes (the input Nodes, not an inner stream), and that lowercase `if` is not the combinator
