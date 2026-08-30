# Ticket 23 — AND, OR, NOT, and comma combinators

Branch: `w/expr`. Tree left dirty (includes ticket 24 and a parallel ticket 25 harness). No commit.

## What changed

[[src/Shared/ExprParse.fs]] now lexes `(`, `)`, and `,`, and parses `AND`, `OR`, `NOT`, and comma on the Expression grammar at locked precedence: juxtaposition, then `NOT`, then `AND`, then `OR`/comma. Parentheses group a sub-Expression. Comma is `OR` here; Amble comma-as-`FunCall` is not restored.

[[src/Shared/ExprPathClusterTypes.fs]] adds `Expr` (`Term`, `Pipe`, `Not`, `And`, `Or`). `parseExpr` returns that tree. [[src/Shared/ExprCompile.fs]] walks it: juxtaposition still binds; `OR`/comma use `ExprEval.orEval` (concat, may repeat); `AND` uses `andEval` (left order, at most once); `NOT` uses `notEval` (negation-as-failure). Mixed operand types on `AND`/`OR`/comma are `type error`. Run and `>` are unchanged.

## Tests

[[tests/Shared.Tests/ExprCombinatorTests.fs]] (registered in [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]]): `#x , #y` / `#x,#y` / `#x OR #y` concat; `#x , #x` repeats; `containing "the" AND named "blue"` same-input intersection; `root descendant NOT containing "draft"` keeps empty inner; `d AND b OR c` equals `(d AND b) OR c`; mixed `text OR root` is a type error. Path-cluster parse tests now assert `Expr.Pipe`/`Expr.Term`.

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ExprCombinatorTests"
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~Expr"
```

Result: **Passed — ExprCombinatorTests 6/6**, **Expr 118/118** (includes RefExpr and the parallel chapter 11 harness). Client was not compiled (Shared.Tests only).

## Leftover risks

- Chapter 11 combinator rows (`#x , #y`, `containing … AND named …`, `NOT containing`, `// OR /`) are still omitted from [[tests/Shared.Tests/ExprChapter11Tests.fs]] (ticket 25 leftover). `// OR /` is now a missing-argument parse error on the first bare cluster.
- `section` / `subsection` are not in the trailing-literal word list (ticket 26).
- `ExprCompile.eval` still does not type-check; `inferType` / `evalOutcome` do. Run and dialog already use `evalOutcome`.

## WORK.md mutations (for the root)

- `remove` [[.scratch/expression-language/issues/23-and-or-not-and-comma-combinators.md]] from Pending — implemented; tests green
- Ticket 25 is not on the board (harness already `Status: done` without combinator rows). It is unblocked for those omitted chapter 11 rows; 21–22 are done. Parent may add a small follow-up to Pending, or leave 25 as done with that leftover.
