# Run error message, not echo

Branch: `w/expr`. Tree left dirty. No commit. Did not change `//` cluster parse or eval.

## Before

| Line | Child text | Class |
| --- | --- | --- |
| `= /` (parse fail) | `No matches found` | blueletter |
| `= root text child` / `= text #todo` (type fail) | `No matches found` | blueletter |
| `= named "zzz"` (zero Answers) | `No matches found` | blueletter |
| `> python` (eval fail) | `> python` (echo of input) | redletter |
| `>` (parse fail) | `>` (echo of input) | redletter |

[[src/Client/UpdateAmbleRun.fs]] still applies ops from [[src/Shared/AmbleRun.fs]] `run`. No Client change.

## After

| Line | Child text | Class |
| --- | --- | --- |
| `= /` | `missing argument` | blueletter |
| `= root text child` / `= text #todo` | `type error` | blueletter |
| `= named "zzz"` | `No matches found` | blueletter |
| `> python` | `Expression type not implemented` | redletter |
| `>` | `empty command stage` | redletter |

Parse and type failure use the `Error` string from [[src/Shared/ExprParse.fs]] / [[src/Shared/ExprCompile.fs]] (or Amble parse/eval). The Child is not a copy of the input. Zero Answers stay `No matches found`. Expression `=` errors keep blueletter. Amble `>` errors keep redletter.

Empty `=` still writes `No matches found` (the empty-source shortcut in `ExprRun.run`, not `evalOutcome`). Search and Move still merge parse/type fail into no hits ([[src/Shared/ExprDialog.fs]]).

## Files changed

- [[src/Shared/ExprRun.fs]] — `ParseFailed` / `TypeFailed` write the error string as a blueletter Child
- [[src/Shared/AmbleRun.fs]] — `legacyRun` writes the parse/eval message (redletter), not the input line; empty Amble specs write `No matches found`
- [[tests/Shared.Tests/ExprRunTests.fs]] — split parse / type / zero-Answer cases
- [[tests/Shared.Tests/AmbleRunTests.fs]] — same split, plus `>` parse and eval message cases

Left the concurrent `= //Example` tests in [[tests/Shared.Tests/ExprRunTests.fs]] in place (other agent). Restored `hasBlueletter` so those tests still compile.

## Tests

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ExprRunTests|FullyQualifiedName~AmbleRunTests"
```

Result: **Passed 23/23**.

## WORK.md mutations (for the root)

None. This presentation fix was not on the board.
