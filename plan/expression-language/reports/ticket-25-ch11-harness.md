# Ticket 25 — chapter 11 regression harness

Non-combinator chapter 11 table is covered by [[tests/Shared.Tests/ExprChapter11Tests.fs]], registered in [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]]. Seam is `ExprCompile.evalOutcome` (Run rows use `ExprRun.run`). Asserts spec outcomes. Did not edit parse or combinator files owned by ticket 23. Did not commit.

## Covered

Fixture graph under Workspaces: workspace `ws` with `x`, file `inner.fs` (todo wall, `blue` under todo, unnamed current, sibling), workspace-level `todo`; Directory and File both named `d`; File `file`; spaced names `filename with spaces` and `a b`/`c d`; `a#b#c` chain; named `blue` whose Header contains `the`; `h1` class node; Run focus under ROOT.

Valid: `//ws`, `root / "ws"`, `// "ws"`, `//ws/x`, `//file`, `// "filename with spaces"`, `// "a b" / "c d"`, `d/e`, `root tree` equals `**`, `root ws` equals `root`, `/ "d" dir` (structural then `dir`; not a pure filter), `d#e`, `a#b#c`, `^#blue` misses walled `blue`, `^#todo#blue` finds it, `wsroot`, `wsroot #todo`, `child` equals `:*`, `class "h1"`, `named`, `containing`, `root descendant containing "the" named "blue"`, `#todo text` (`Node ⇒ Text`), Run `= root descendant named "blue"` and `todo=…`.

Errors: `// ws`, `"d" "e"`, `/`, `root descendant containing root` (parse, missing argument), `3` (locked number wording), `text #todo` (type error). Zero Answers: `!-249053534` is empty Hits, not parse or type failure.

## Leftover (parent after 23, then 26)

Ticket 23 combinators, omitted so this file stays green and off parse:

- `#x , #y`
- `containing "the" AND named "blue"`
- `root descendant NOT containing "draft"`
- `// OR /` (parse error once `OR` parses)

Ticket 26, not in catalog; do not treat as `#` / `named` substitutes beyond the table’s `#todo` spelling:

- `subsection "todo"`
- `section`

## Verify

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ExprChapter11"
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~Expr"
```

ExprChapter11: 13 passed. FullyQualifiedName~Expr: 118 passed (23 had not landed failing combinator tests).

## WORK.md mutations

Ticket 25 was not on the board. No `add`. No `remove`. Ticket 23 stays Pending; it did not block this non-combinator harness.
