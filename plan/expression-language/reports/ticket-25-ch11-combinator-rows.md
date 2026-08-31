# Ticket 25 — chapter 11 combinator rows

Added the leftover chapter 11 combinator rows to [[tests/Shared.Tests/ExprChapter11Tests.fs]]. Same fixture graph (plus section Nodes `x` and `y`, and a Header that contains `draft`). Same seam: `ExprCompile.evalOutcome`. Did not duplicate [[tests/Shared.Tests/ExprCombinatorTests.fs]]. Did not edit `src/**` or the test fsproj. Did not commit. Stayed on `w/expr`.

## Rows added

- `#x , #y` — concatenates subsection-search Answers from the inner File; `#x , #x` yields the same Node twice
- `containing "the" AND named "blue"` — keeps `theBlue`; empty on `headed` (contains `the`, name is not `blue`) and on `blueUnderTodo` (named `blue`, Header has no `the`)
- `root descendant NOT containing "draft"` — keeps descendants such as `theBlue`, `headed`, and section `x`; drops the draft Node
- `// OR /` — parse error, needle `missing argument` (first bare cluster)

No other chapter 11 combinator rows were missing. Ticket 26 rows `section` and `subsection "todo"` stay omitted.

## Verify

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ExprChapter11"
```

Build succeeded. ExprChapter11: 17 passed (was 13; three Facts plus one Theory case).

Ticket comments: [[plan/expression-language/issues/25-spec-ch11-worked-example-regression-harness.md]] — combinator leftover filled (except 26 rows).

## WORK.md mutations

- `remove` [[tests/Shared.Tests/ExprChapter11Tests.fs]] — add leftover chapter 11 combinator rows (`#x , #y`, `AND`/`NOT`/`OR`) now that ticket 23 landed (parent: [[plan/expression-language/issues/25-spec-ch11-worked-example-regression-harness.md]]) from Pending
