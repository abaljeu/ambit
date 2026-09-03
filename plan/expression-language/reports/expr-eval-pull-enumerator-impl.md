# Expression eval pull enumerator

Stay on `w/expr`. Did not edit [[WORK.md]]. Did not commit.

## Pull type

[[src/Shared/ExprEval.fs]] `Predicate` is now `ExprAnswer -> Stream`. `Stream` is a private delayed cell: `unit -> (ExprAnswer * Stream) option`. One pull yields one Answer and a leftover cursor. The leftover is not forced.

`ExprEval.take : int -> Stream -> ExprAnswer list * Stream option` is the page function. It matches the Search dialog generator shape (count in, page plus leftover option out). `toList` pulls until empty. Combinators pull on demand: `bind` / `OR` append streams; `NOT` pulls the first inner Answer only; `AND` still materialises the right operand (intersection needs the full right set) and filters the left as it is pulled.

Walks in [[src/Shared/ExprWalk.fs]] (`descendant`, `tree`, structural `/`, content `#`) keep a frame stack and a seen set. Each pull yields one match and continues later. They do not finish the graph before the first yield.

## Search generator type and seam

Word Search already uses this generator:

- Type: `ViewModelSearch.SearchCursor` (private record: graph, word-search filters, BFS `DiscoveryState`)
- Start: `ViewModelSearch.startSearch : string -> NodeId -> Graph -> SearchCursor option`
- Page: `ViewModelSearch.takeResults : int -> SearchCursor -> NodeSearchResult list * SearchCursor option`
- Dialog: [[src/Client/SearchDialog.fs]] `searchPageSize = 12`; `loadPage` calls `takeResults`

The Search dialog pager is still word-search only. `startSearch` does not compile an `=` Expression. [[src/Shared/ExprDialog.fs]] `tryHits` still uses `evalOutcome`, which materialises the full Answer list. `ViewModelSearch.searchNodes` calls `tryHits` for a leading `=`, then falls back to word Search. Move uses the same `tryHits` list. That is not the dialog pager.

This slice does **not** wire Search or Move to the new stream. It only matches the generator shape: `ExprEval.take` has the same `(count, cursor) -> (page, leftover option)` contract as `takeResults`. A later adapter can wrap a compiled `Stream` in `SearchCursor` (or a thin sibling cursor) and page with `take`. Do not apply `ExprRun.maxMaterialisedAnswers` (50) to that pager. Search already has page size 12.

## Run cap

[[src/Shared/ExprRun.fs]] `maxMaterialisedAnswers = 50`. Run compiles the Expression and calls `take` with that binding. It materialises at most 50 Children. It does not write a truncated Child. Unfold still occurs when Children are written. `//` semantics, error-message strings, and commit-before-run are unchanged.

`ExprCompile.eval` and `evalOutcome` still return a full `ExprAnswer list` (they call `toList`). Tests and `tryHits` keep that eager consumer. Run does not use `evalOutcome` for hits.

## Tests

- `take two from a three-hit stream leaves the third unforced` — test-double counter; `take 2` does not force the third cell
- `descendant take two resumes at the late unique child` — leftover stream yields the third child only when pulled
- `equals root descendant named hit writes at most maxMaterialisedAnswers` — 60 matching Nodes, Run writes 50 Refs, unfold stays true
- Focused `FullyQualifiedName~Expr`: 134 passed. AmbleRun / Search / ExprDialog consumers: 57 passed

## Client compile gate

`./scripts/client.sh build` — Fable and esbuild succeeded.

## WORK.md mutations

- add [[plan/expression-language/reports/expr-eval-pull-enumerator-impl.md]] — HITL: Run `= root descendant …` with more than 50 hits; confirm 50 Children and unfold (owner: parent)
- add [[plan/expression-language/reports/expr-eval-pull-enumerator-impl.md]] — later: adapt Expression `Stream` into `SearchCursor` / `takeResults` so the Search dialog pages `=` queries; do not apply the Run cap of 50 (owner: parent)
