# Text operations implementation (issue 30)

Issue [[../issues/30-text-operations.md]] is implemented and marked done. Branch `w/tree2-semantics`, no commit. Focused Expr tests are green (164), Amble and RefExpr tests are green (85), and the Client compile gate (`./scripts/client.sh build`, Fable plus esbuild) passed.

## Adjacent quoted juxtaposition (reversal)

`"d" "e"` is a parse error again: a Seq of two quoted-string terms. Message: `cannot juxtapose two quoted strings`. Quoted strings remain Text Expressions (`"rapid"` is still a term). Combinator operands and catalog slots stay legal: `left 5 IS "rapid"`, `containing "blue"`, `text containing "x"`, `IF (name right 4 IS ".txt")`, and `(text IF ("b" IS left 1)) OR "isn't a b word"` (OR of a string-producing side and a quoted string, not two literals glued by bind). Numbers stay as they were: bare `3` is not a term.

The user form is `(text IF ("b" IS left 1)) OR "isn't a b word"`. The ungrouped `(text IF "b" IS left 1)` parses as `(text IF "b") IS (left 1)` because `IF` binds tighter than `IS`. That parse is not the intended grouping. The precedence of `IF` and `IS` does not change.

## What landed

`text`, `name`, `left n`, `right n`, the infix combinator `IS`, quoted strings as Text Expressions, and dual `containing` / `re` / `rei`. No implicit Node to Text coerce.

| Change | Where |
| --- | --- |
| `ExprSignature` is now a DU: `Fixed(input, output)` or `Same` (the dual `τ ⇒ τ` shape) | [[src/Shared/ExprAnswer.fs]] |
| `ExprTerm.Text` (quoted string as an Expression) and `Expr.Is` | [[src/Shared/ExprPathClusterTypes.fs]] |
| `IS` token, `IS` in the `AND` attach loop, a Number slot for `left` / `right`, a quoted string in term position | [[src/Shared/ExprParse.fs]] |
| `isEval`; `andEval` and `isEval` now share one `intersectEval` that differs only in the at-most-once rule | [[src/Shared/ExprEval.fs]] |
| `ExprSlotKind.Int` / `ExprBoundSlot.Int` | [[src/Shared/ExprCatalog.fs]] |
| `dualFilter` behind `containing` / `re` / `rei`; `nameText`, `leftText`, `rightText` | [[src/Shared/ExprWalk.fs]] |
| Rows `name`, `left`, `right`; `text` moved to the shared row helper; the three dual rows carry `ExprSignature.Same` | [[src/Shared/ExprPrimitive.fs]] |
| `IS` compile and type rules, the `Int` slot bind, and type inference rewritten to flow left to right | [[src/Shared/ExprCompile.fs]] |

## The one structural decision

Type inference used to compose two full signatures and ask whether the middle types met. A dual row has no single input type, so that shape cannot express `containing`. Inference now threads the input type left to right: each term is offered the type of the Answer that reaches it and reports the type it yields. `Same` returns whatever arrives. `ExprCompile.inferType` starts at Node, because every consumer applies the Expression to a Node Answer (spec chapter 8), and it now returns the output `ExprAnswerType` instead of a signature record.

Two useful facts fall out. A top-level `left 5` is now a type error, which the issue asks for. `OUTER` no longer needs to inspect its operand's input type; it demands a Node input directly.

## Behavior notes worth knowing

`"d" "e"` is a dedicated parse error: two quoted-string terms next to each other in juxtaposition. Quoted strings remain Text Expressions; combinator operands and catalog slots stay legal. Numbers and symbols keep the original rule: bare `3` is still a parse error, with the amended message `a number is only valid as the slot of : ! left or right`.

The Node overload of the three dual filters is unchanged. `containing "blue"`, `root descendant containing "the"`, `OUTER containing "blue"`, `re`, and `rei` all still type `Node ⇒ Node` and yield Nodes; there is a test that asserts those four inferred types.

`tree`, `OUTER`, `IF`, `NOT`, `AND`, `OR`, and the Run consumer `=` are untouched.

## Known gap, not fixed here

A path cluster still takes a following quoted string greedily, so `#todo "x"` is `unexpected literal` while `named "a" "x"` now parses as a filter and then a Text Expression. Making the cluster refuse a literal it has no slot for is a small change in `parseClusterSegment`, but no example or test needs it, so it stayed out of this diff.

## Tests

- [[tests/Shared.Tests/ExprTextOpsTests.fs]] (new, registered in [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]]): `text` and the empty Header; `name` and Filename Empty; `left` / `right` clamping in both directions; the no-coerce type errors; missing-argument parse errors; a quoted string as an Expression; adjacent quoted juxtaposition as a parse error; combinator and slot uses of quoted strings stay legal; `IS` same-input matching and both empty-operand cases; `IS` parse attach and lowercase `is`; `IF` and `OUTER` pullback of a text predicate.
- [[tests/Shared.Tests/ExprFilterTests.fs]]: the Text overload of the three dual filters, and an inferred-type guard for the Node overload.
- [[tests/Shared.Tests/ExprChapter11Tests.fs]]: the two changed rows, plus `"d" "e"` as the dedicated juxtaposition parse error.
- [[tests/Shared.Tests/ExprEvalTests.fs]]: the stub row now builds `ExprSignature.Fixed`.

## Documents

[[../spec.md]] chapters 1, 3, 4, 5, 6, 7, 9, 10, and 11 now match the code: the amended literal rule, `IS` in the grammar and precedence, `QuotedString` as a Term, the dual-row and quoted-string typing rules, the `IS` and quoted-string evaluation rules, four new catalog rows plus the `IS` combinator row, and nine chapter 11 example rows. Spec edits stayed inside the text-operations material; the sibling `ref` / `owned` row in chapter 7 is preserved. [[CONTEXT.md]] gains an `IS` entry beside `OUTER` and `IF`.

## Board mutations for the parent

- `remove` [[plan/expression-language/issues/30-text-operations.md]] from Pending.
- `add` to Pending: [[plan/expression-language/reports/text-ops-impl.md]] — HITL: Run `= … IF (text left 5 IS "rapid")` and `= … IF (name right 4 IS ".txt")` on `/ambit` or `/ambit?debug=1`; confirm the Answers are Nodes, that a bare `left 5` reports a type error, that lowercase `is` is not the combinator, and that `"d" "e"` is a parse error.

The project Stage in [[../project.md]] stays `active` (issue 32 is open), so it is unchanged and [[plan/index.md]] needs no regeneration for this work.
