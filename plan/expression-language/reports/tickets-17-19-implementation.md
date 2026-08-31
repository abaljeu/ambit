# Tickets 17–19 implementation

Branch: `w/expr`. Tree left dirty. No commit.

## Ticket 17 — Structural path-step evaluation

Required: evaluate `/` (Owned descent; do not enter Directory or Workspace Children), `**`/`tree` (Owned closure, no Ref), `^`/`.`/`wsroot` (Owned up-walks), `:`/`!` (Children and Owned siblings), glob `*` only.

Implemented:

- [[src/Shared/ExprWalk.fs]] — glob (`*` only), structural search, tree, descendant, up-walks, content search, `containing`
- [[src/Shared/ExprPrimitive.fs]] — catalog rows for `root`, `/`, `tree`/`**`, `^`, `.`, `wsroot`, `:`, `!`, later `#`/`child`/`descendant`/`containing`
- [[src/Shared/ExprCompile.fs]] — parse, bind juxtaposition, eval against the primitive catalog
- Tests: [[tests/Shared.Tests/ExprStructuralEvalTests.fs]] (6 facts)

Behavior: `root / "ws" / "x"` equals `//ws/x`; `/` does not enter Directory/Workspace; `**` matches `tree` and does not follow Ref; `?` in a glob is literal.

### SiteNav realignment (`:` / `!`)

`childAt` and `siblingAt` no longer nest `tryGraphNode` / `Map.tryFind` / `List.tryFindIndex`. They are thin callers over [[src/Shared/Model.fs]] `Node` nav, the Graph counterpart of [[src/Shared/ViewModel.fs]] `Site`:

- `NodeNav` + `step` already existed (`owner`, `firstChild`, `lastChild`)
- Added `childNth`, `childIds`, `childIndex` (Owned appearance), `siblingNth`, `next`, `prev`
- `:*` is `childIds`; `:n` is `childNth n`; `!*` is `owner >> childIds`; `!n` is `siblingNth n`
- `!0` stays identity; ROOT `!` stays a miss

## Ticket 18 — Content search `#`

Required: `#` searches strictly below each input through Owned and Ref Children; DFS; first-reach dedupe; named Normal is a wall; unnamed Normal is transparent; do not enter File/Directory/Workspace Children; chained `#` searches below prior Answers.

Implemented in `ExprWalk.contentSearch` and catalog row `#`. Tests: [[tests/Shared.Tests/ExprContentSearchTests.fs]] (4 facts). All passed.

## Ticket 19 — Pipeline lexer, walk words, juxtaposition

Required: layer-one segments; left-associative bind; words `root`, `child`, `descendant`, `tree`, `containing`, `wsroot`; unknown Name is a parse error; reserve `AND`/`OR`/`NOT` in capitals; `// ws` parse error vs `//ws` valid.

Layer-one parse was already in [[src/Shared/ExprParse.fs]] (ticket 16). This ticket registered the walk words, bound `containing` on Header text (not the name), reserved capital combinators at compile, and evaluated juxtaposition via `ExprEval.bind`. Tests: [[tests/Shared.Tests/ExprPipelineTests.fs]] (4 facts). All passed.

Not in this slice: `named`/`ws`/`dir`/`file`/`normal`/`class` (ticket 20); `AND`/`OR`/`NOT` evaluation (ticket 23).

## Tests

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~Expr"
```

Result: **Passed — 87/87** (Expr* plus RefExpr modules matched by the filter). New facts: 6 structural, 4 content, 4 pipeline.

## Incomplete / blocked

Nothing blocked. Ticket 20 is the next catalog slice (pure filters). Combinators stay ticket 23.

## WORK.md mutations (for the root)

- `remove` [[plan/expression-language/issues/17-structural-path-step-evaluation-realignment.md]] — implemented; tests green
- `remove` [[plan/expression-language/issues/18-content-search-path-step-evaluation.md]] — implemented; tests green
- `remove` [[plan/expression-language/issues/19-spaced-pipeline-lexer-walk-words-and-juxtaposition.md]] — implemented; tests green
- `add` [[plan/expression-language/issues/20-pure-filter-catalog-rows.md]] — Pending; `named`, `ws`/`dir`/`file`/`normal`, `class` (blocked by 19, now clear)
