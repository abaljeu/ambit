# OR on strings

**Answer: yes.** `OR` concatenates same-type Answer sequences. After the IS / text-operations change, a quoted string is a Text Expression, so `"d" OR "e"` is a well-typed `Node ⇒ Text` Expression. `OR` was not rewritten to be Node-only or Text-only. Mixed Node and Text operands stay a type error.

## Spec

[[../spec.md]] chapter 5 types `OR`, `AND`, `IS`, and comma the same: both operands `τ1 ⇒ τ2`, result `τ1 ⇒ τ2`. `τ` is Node or Text. Chapter 6: `E⟦e1 OR e2⟧ x = E⟦e1⟧ x ++ E⟦e2⟧ x`. A quoted string yields that Text from any input. Chapter 3 and chapter 11 name `"d" OR "e"` as a legal combinator use. Juxtaposition `"d" "e"` is a parse error (`cannot juxtapose two quoted strings`). Mixed types across combinators are a type error.

The change that opened string operands is [[../issues/30-text-operations.md]]: quoted strings became Text Expressions so `"rapid"` can be an `IS` operand. That issue says `OR` is unchanged as a combinator. [[../issues/23-and-or-not-and-comma-combinators.md]] already typed `OR` over Answer sequences; its examples were Node searches (`#x OR #y`).

## Code

[[src/Shared/ExprEval.fs]] `orEval` appends two streams. It does not inspect Node versus Text. [[src/Shared/ExprCompile.fs]] compiles `Expr.Or` with `orEval`, and types `And` / `Or` / `Is` with one rule: both output types must agree. A quoted string compiles to one Text Answer ([[src/Shared/ExprParse.fs]] `ExprTerm.Text`; compile ignores the input). [[src/Shared/ExprAnswer.fs]] equality already compares Text by string equality.

So `"d" OR "e"` from a Node input types `Node ⇒ Text` and yields the two strings `d` then `e`. `text OR "fallback"` is the same Text family. `text OR root` is a type error.

## Tests

Source-level: [[tests/Shared.Tests/ExprTextOpsTests.fs]] parses `(text IF ("b" IS left 1)) OR "isn't a b word"` as `OR` with a quoted right operand, and evals `"rapid"` as one Text Answer. [[tests/Shared.Tests/ExprCombinatorTests.fs]] evals `#x OR #y` as Nodes, and asserts `text OR root` is `type error`. [[tests/Shared.Tests/ExprEvalTests.fs]] unit-tests `orEval` on Nodes; `andEval` already intersects Text Answers by string equality. [[tests/Shared.Tests/ExprChapter11Tests.fs]] locks `"d" "e"` as a parse error, not `"d" OR "e"`.

No test evals `"d" OR "e"` to `[d; e]`. The code path is the same as the quoted-string term plus `orEval`.

## AND and NOT

`AND` shares the type domain with `OR` and `IS`. `"d" AND "e"` types `Node ⇒ Text` and is empty (the strings are not equal). `"d" AND "d"` yields `d`.

`NOT` does not concatenate Answers. It yields the input Answer when the operand is empty. From a consumer Node, `NOT e` stays `Node ⇒ Node` even when `e` yields Text.

## Leftover

No source-level eval of `"d" OR "e"`. Issue 30 said `OR` is unchanged; the new surface is quoted strings as terms, not a new `OR` rule.
