# Ticket 24 — Retire Amble prefix FunCall, `of`, and legacy Run paths

Branch: `w/expr`. Tree left dirty. No commit.

## What changed

Amble prefix `FunCall` juxtaposition, infix `of`, and comma-as-`FunCall` no longer parse. `text #todo`, `name of children ./folder/`, and `#a , #b` are parse errors. `FunCall("text", …)` eval is gone.

[[src/Shared/AmbleRun.fs]] routes `=` / `Name=` through [[src/Shared/ExprRun.fs]]. A line that is not that form, including bare `//x/y` and prefix `text #todo`, returns no ops. Parse, type, and zero Answers on valid `=` / `Name=` still write one blueletter Child `No matches found`.

`>` shell parse and the legacy error-child path are unchanged. The Run command ([[src/Shared/CommandEntry.fs]] `Exec`, Ctrl+Enter) stays. [[src/Client/UpdateAmbleRun.fs]] still calls `runAmbleOp`; after apply it unfolds the focused SiteMap entry when Children are written.

## Tests

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~Amble"
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~Expr"
```

Result: **Passed — Amble 25/25**, **Expr 99/99** (Expr* plus RefExpr). Client was not compiled (Shared.Tests only).

## Leftover risks

- `FunCall` remains on the Amble AST; parse does not construct it. Ticket 23 must add comma-as-`OR` on the Expression parser, not restore Amble comma-as-`FunCall`.
- Postfix `text` is already a catalog row for Search type merge; `= #todo text` would materialise Text Answers. Enabling `text` as a user-facing Run spelling is still a later slice.
- `>` eval is still unimplemented: Run writes redletter Children of the line text.

## WORK.md mutations (for the root)

- `remove` [[.scratch/expression-language/issues/24-retire-amble-prefix-juxtaposition-and-legacy-run-paths.md]] — implemented; tests green
- `move` [[.scratch/expression-language/issues/23-and-or-not-and-comma-combinators.md]] from Blocked to Pending — unblocked now that 24 is done
