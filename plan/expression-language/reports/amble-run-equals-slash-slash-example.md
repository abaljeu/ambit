# Run on `= //Example`

Stay on `w/expr`. Did not change `src/**`. Did not edit [[WORK.md]].

## Actual error

Not a parse error, type error, thrown exception, or `Error` from [[src/Shared/AmbleRun.fs]]. `ExprParse.parseExpr "//Example"` is `Ok (Term (Cluster ([Root; Structural "Example"], None)))`. `ExprCompile.evalOutcome` is `Hits` with the empty list, or with the Workspace named Example when that Workspace sits at the first structural layer under ROOT.

`AmbleRun.run` then returns `Ok` with `ExprRun.Apply`. Zero Answers become one blueletter Child whose Header is `No matches found` ([[src/Shared/ExprRun.fs]] `noMatches`). That is the user-visible "error". Redletter Children are only for `>` shell. Bare `//Example` without `=` is not a Run statement and yields empty ops.

## Root cause

[[.scratch/expression-language/spec.md]] spells `//` as shorthand for `root /`. Structural search `/` does Owned recursive descent strictly below the input and does not enter the Children of a Directory Node or Workspace Node. Deeper names need a chain (`//ws/x`).

ROOT is a Workspace Node. The Workspaces container is not a Directory Node or Workspace Node, so `/` does enter it and can match a Workspace named Example. A Directory Node or File Node named Example that is Owned under some other Workspace is behind that wall, so `= //Example` is a miss.

This matches the existing eval lock in [[tests/Shared.Tests/ExprStructuralEvalTests.fs]]: `root / "x"` is empty; `//ws/x` finds Directory `x`.

## Code changed

No production code. The Run path already matches the spec. Added regression Facts in [[tests/Shared.Tests/ExprRunTests.fs]]: parse of `//Example`; `= //Example` writes a Ref to a first-layer Workspace named Example via both `ExprRun.run` and `AmbleRun.run`; nested Directory Example writes blueletter `No matches found`.

## Tests run

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ExprRunTests"
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~Example"
```

First ExprRunTests run: 7 passed (build succeeded). Later rebuild hit a locked `Gambol.Shared.dll` (another .NET Host). `--no-build` Example filter: 3 passed.

## What to type on the Node

Run the Normal Node whose Header is the statement (not a Special Node; Run on Special is empty ops).

- Workspace named Example (child of Workspaces): `= //Example`
- Directory Node or File Node named Example inside Workspace `ws`: `= //ws/Example`
- File Node inside Directory `src` of that Workspace: `= //ws/src/Example`

`= //Example` does not search the whole Graph for that name.

## WORK.md mutations

None.
