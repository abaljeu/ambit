# Ticket 15 — Answer-sequence eval core and catalog row shape

## Summary

Built the Shared evaluation foundation for the Expression language on branch `w/expr`. Every term is a predicate `ExprAnswer -> ExprAnswer list`; four combinators implement spec chapter 6; catalog rows are data (spellings, slot kind, signature, evaluate hook); `ExprWalk.childAnswers` is the Unloaded miss hook for walks that need Children.

## Modules

| Module | Role |
| --- | --- |
| [[src/Shared/ExprAnswer.fs]] | `ExprAnswer` (Node \| Text), `ExprAnswer.equal`, `ExprAnswerType`, `ExprSignature` |
| [[src/Shared/ExprEval.fs]] | `Predicate` alias; `bind`, `orEval`, `andEval`, `notEval` |
| [[src/Shared/ExprCatalog.fs]] | `ExprCatalogRow`, `ExprSlotKind`, `ExprBoundSlot`, register/lookup/invoke |
| [[src/Shared/ExprWalk.fs]] | `childAnswers` — empty on Unloaded, Children in order on Loaded |

## Semantics (spec ch. 6)

- **bind** — `List.collect` left-to-right (monadic bind).
- **OR** — operand concatenation; repeats allowed.
- **AND** — left-operand order; intersection by `ExprAnswer.equal`; each Answer at most once.
- **NOT** — negation-as-failure: `⟨input⟩` when inner is empty, else `⟨⟩`.

## Tests

[[tests/Shared.Tests/ExprEvalTests.fs]] — 10 facts covering bind order, OR repeat, AND dedup (Node and Text), NOT empty/succeed, Answer equality, catalog stub invoke, Unloaded miss, Loaded Children order.

```bash
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ExprEvalTests"
```

Result: **Passed — 10/10**.

## Files touched

- `src/Shared/ExprAnswer.fs` (new)
- `src/Shared/ExprEval.fs` (new)
- `src/Shared/ExprCatalog.fs` (new)
- `src/Shared/ExprWalk.fs` (new)
- `src/Shared/Gambol.Shared.fsproj`
- `tests/Shared.Tests/ExprEvalTests.fs` (new)
- `tests/Shared.Tests/Gambol.Shared.Tests.fsproj`

## Ticket 16 parallelism

Ticket 16 (path cluster parse) can proceed in parallel with **no semantic conflict**:

- Ticket 15 owns `ExprAnswer`, `ExprEval`, `ExprCatalog`, `ExprWalk` under `src/Shared/Expr*.fs` (except parse modules).
- Ticket 16 should own `ExprPathClusterTypes`, `ExprPathClusterParse`, `ExprParse` and their tests; register those in the fsproj after ticket 15 lands.
- WIP ticket-16 files were present untracked on the branch; they were **excluded** from the fsproj so ticket 15 builds green. Ticket 16 re-adds its own compile entries.

Merge note: only `Gambol.Shared.fsproj` and `Gambol.Shared.Tests.fsproj` may need a trivial combine (append ticket-16 `<Compile>` lines after ticket-15 lines).
