# Ticket 26 — `section` filter and `subsection` spelling

Branch: `w/expr`. Tree left dirty (includes tickets 24, 23, 25). No commit.

## Required

Add `section` as a zero-argument pure filter: keep a named Normal Node; unnamed Normal, File, Directory, and Workspace yield nothing; no Children walk. Register `subsection` as a spelling of the existing `#` search (`subsection "todo"` equals `#todo`). Bare `subsection` is a missing-argument parse error, uniform with bare `#`. Keep `named` distinct from `section`.

## Implemented

[[src/Shared/ExprWalk.fs]] — `section` via `keepInput`: Normal Kind and a Filename value. No walk.

[[src/Shared/ExprPrimitive.fs]] — `section` row, same shape as `dir` / `normal`. The content-search row spellings are now `subsection` and `#`, same shape as `tree` / `**` (NameGlob slot). `/` is unchanged.

[[src/Shared/ExprParse.fs]] — `subsection` takes a trailing quoted literal, with the same missing-argument parse as `named` / `containing` / `class`. Bare `#` still fails in the cluster parser with the same message.

`named "blue"` stays a name glob on the input. `section` is named-Normal classification with no argument.

## Tests

[[tests/Shared.Tests/ExprSectionTests.fs]] (one Compile Include in [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]]) — 4 facts: named-Normal keep and miss on unnamed / File / Directory / Workspace with no walk; `subsection "todo"` equals `#todo`; bare `subsection` and bare `#` both `missing argument`; `named "blue"` vs `section` on `todo`.

Did not edit [[tests/Shared.Tests/ExprChapter11Tests.fs]].

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ExprSection"
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~Expr"
```

Result: **Passed — ExprSection 4/4**, **Expr 126/126**. No chapter 11 combinator-row failures. Client was not compiled (Shared.Tests only).

## Incomplete / blocked

Nothing blocked. Ticket 25 leftover combinator rows in the chapter 11 harness were already green in this Expr run; this agent did not add `section` / `subsection` rows there.

## WORK.md mutations (parent applies; this agent did not edit [[WORK.md]])

- `remove` [[.scratch/expression-language/issues/26-section-and-subsection-catalog-rows.md]] from Pending — implemented; tests green
