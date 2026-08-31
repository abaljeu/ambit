# `ref` / `owned` implementation (issue 32)

Issue [[../issues/32-ref-and-owned-children.md]] is implemented and marked done. Branch `w/tree2-semantics`, no commit. Focused Expr tests are green (58), and the Client compile gate (`bash ./scripts/client.sh build`, Fable plus esbuild) passed.

## What landed

Catalog rows `ref` and `owned` as a partition of `child`. Same walk start as `child`: immediate Children of the input Node, in Children order. `owned` keeps `Ownership.Owner`. `ref` keeps `Ownership.Ref`. Unloaded is a miss and never Loads. Spellings are lowercase Name tokens with no slot. Text input is a miss.

| Change | Where |
| --- | --- |
| `childrenWhere` shared Unloaded/Loaded walk; `ownedAnswers` / `refAnswers` | [[src/Shared/ExprWalk.fs]] |
| Rows `owned` and `ref` next to `child` | [[src/Shared/ExprPrimitive.fs]] |
| Catalog facts on the pipeline File Node; interleaved `owned OR ref`; Unloaded; spellings | [[tests/Shared.Tests/ExprPipelineTests.fs]] |
| Walk-level partition, Text miss, Unloaded | [[tests/Shared.Tests/ExprEvalTests.fs]] |

`ownedChildren` (used by `tree` / `OUTER`) now uses `childrenWhere`. Catalog `owned` is that filter at depth one. `childRow` still calls `ExprWalk.childAt`. `OUTER`, `tree`, and `descendant` are unchanged.

On the pipeline File Node (three Owned, then Ref to `outside`): `child` is those four; `owned` is the three Owned; `ref` is `outside`. `owned child` is empty (the Owned Answers are leaves), not the partition. On an interleaved parent (Owned, Ref, Owned), `owned OR ref` concatenates and is not `child`.

## Spec

[[../spec.md]] chapter 7 moves `ref` / `owned` from the reserved table into the catalog. Chapter 9 item 15 and chapter 11 gain the two rows.

Spoken **Ref** and **Owned** stay in [[CONTEXT.md]]. Catalog spellings stay `ref` and `owned`. `Ref` / `Owned` as Name tokens remain unknown words.

## Tests

- `dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ExprPipelineTests|FullyQualifiedName~ExprEvalTests|FullyQualifiedName~ExprCombinatorTests|FullyQualifiedName~ExprChapter11Tests"` — 58 passed.
- `bash ./scripts/client.sh build` — passed.

## Leftover risks

- `owned "x"` is juxtaposition with a Text Expression (issue 30), not a missing-slot parse error. No-slot is locked by `owned 3` (a number with no operator wanting it). Same shape as `child "x"`.
- HITL is not done. Unloaded-with-empty-children is an invariant ([[src/Shared/Model.fs]]), so the Unloaded miss equals empty Children for `child` through `childAt` as well.
- No commit. Project Stage stays `active`.

## Board mutations for the parent

- `remove` [[../issues/32-ref-and-owned-children.md]] from Pending — catalog `ref` / `owned` implemented.
- `add` to Pending: [[ref-owned-children-impl.md]] — HITL: Run `= owned` and `= ref` on `/ambit` or `/ambit?debug=1` on a Node with mixed Owned and Ref Children; confirm `child` is the Children-order merge, that `owned OR ref` concatenates (not that merge) when the roles interleave, and that `Ref` / `Owned` are unknown words.
