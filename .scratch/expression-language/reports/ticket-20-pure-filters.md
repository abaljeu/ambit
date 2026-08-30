# Ticket 20 — Pure filter catalog rows

Branch: `w/expr`. Tree left dirty. No commit.

## Required

Pure filters on the input Answer only: `named` (quoted glob, Normal name), `ws`/`dir`/`file`/`normal` (Kind), `class` (exact cssClasses token). Do not walk Children. `#` stays content search. Do not evaluate `AND`.

## Implemented

[[src/Shared/ExprWalk.fs]] — `named`, `ws`, `dir`, `file`, `normal`, `classMember` via `keepInput` (no Children walk).

[[src/Shared/ExprPrimitive.fs]] — catalog rows for those spellings. `named` and `class` take a quoted slot (parse already wanted those literals).

`named` does not replace `#`: `named "blue"` on a File is empty; `#blue` from that File finds the child.

`containing "the" AND named "blue"` is still a reserved-word compile error until ticket 23.

## Tests

[[tests/Shared.Tests/ExprFilterTests.fs]] — 4 facts (named keep/wall, named vs `#`, `root ws` and `/ "d" dir`/`file`/`normal`, `class` exact membership).

```
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~Expr"
```

Result: **Passed** (filter facts green in the 99 Expr-matching run).

## Incomplete / blocked

Nothing blocked. Combinator intersection stays ticket 23.

## WORK.md mutations

- `remove` [[.scratch/expression-language/issues/20-pure-filter-catalog-rows.md]] — implemented; tests green
