# Tickets 21–22 — Run, Search, and Move consumers

Branch: `w/expr`. Tree left dirty. No commit.

## Ticket 21 — Run `=` and `Name=`

Required: Run statements only; current Node as input; Node Answers → Ref Children; Text Answers → new Owned Nodes; `Name=` also renames; parse/type/zero → one blueletter Child `No matches found`; unfold when Children are written; `#ident =` and bare Expression do nothing.

Implemented in [[src/Shared/ExprRun.fs]]:

- Classify `= E` and `Name=E` (whitespace around `=`). A left-hand cluster or reserved word is not a Name (`#todo =` is Ignore).
- [[src/Shared/ExprCompile.fs]] `inferType` / `evalOutcome` keep parse error, type error, and empty Answers distinct; Run merges them to the blueletter Child.
- Materialise Refs and Owned text Nodes; `unfold = true` when Children are written.

Amble Run is unchanged (ticket 24 retires the legacy path). Tests call `ExprRun.run`.

Tests: [[tests/Shared.Tests/ExprRunTests.fs]] — 4 facts. All passed.

## Ticket 22 — Search and Move leading `=`

Required: `=` evals from zoomRoot, `Node ⇒ Node` only; parse/type/zero/`Node ⇒ Text` show no hits; no leading `=` keeps word search.

Implemented in [[src/Shared/ExprDialog.fs]] and wired through [[src/Shared/ViewModelSearch.fs]] `searchNodes` (Move uses the same hit list; existing onPick still Zoom vs relocate).

A postfix `text` row (Node ⇒ Text) is registered so Search can reject `= root text`. That row is the spec’s reserved `text` producer, used here only for consumer type merge.

Tests: [[tests/Shared.Tests/ExprDialogTests.fs]] — 4 facts. All passed.

## Tests

```
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~Expr"
```

Result: **Passed — 99/99** (includes tickets 15–22 and RefExpr).

## Incomplete / blocked

Client Move relocate still uses the existing Search-dialog onPick; the Answer set is shared. Ticket 24 still retires Amble Run. Ticket 23 combinators are unblocked.

## WORK.md mutations

- `remove` [[.scratch/expression-language/issues/21-run-consumer-equals-and-name-equals-statements.md]] — implemented; tests green
- `remove` [[.scratch/expression-language/issues/22-search-and-move-consumer-leading-equals.md]] — implemented; tests green
- `add` [[.scratch/expression-language/issues/23-and-or-not-and-comma-combinators.md]] — Pending; unblocked now that 19 and 20 are done
