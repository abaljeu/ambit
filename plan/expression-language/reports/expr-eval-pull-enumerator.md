# Expression eval is not a pull enumerator

Stay on `w/expr`. Did not change `src/**`. Did not edit [[WORK.md]]. Did not create an issue.

## Answer

The Run/Search Expression algorithm is **not** a pullable result enumerator (one Answer plus a continuation). It is an **eager `ExprAnswer list`**: each predicate walks and collects every hit, then returns the full list. There is no remainder after the first hit.

## 1. Runtime representation of “sequence of Answers”

The type is a strict F# `list`. The alias is in [[src/Shared/ExprEval.fs]]:

- `ExprEval.Predicate = ExprAnswer -> ExprAnswer list`

Catalog rows and invoke use that same type:

- `ExprCatalogRow.evaluate : ExprBoundSlot -> ExprEval.Predicate` ([[src/Shared/ExprCatalog.fs]])
- `ExprCatalog.invoke` returns `ExprAnswer list`

Walk producers also return `ExprAnswer list`: `ExprWalk.childAnswers`, `structuralSearch`, `treeAnswers`, `descendantAnswers`, `contentSearch`, `childAt`, `siblingAt`, and the pure filters (`named`, `containing`, `ws`, `dir`, `file`, `normal`, `section`, `classMember`). Compile outcomes carry the same list: `ExprCompile.Outcome.Hits of ExprAnswerType * ExprAnswer list`.

There is no `seq`, `IEnumerator`, `Lazy`, thunk, or explicit `(option * rest)` continuation in these modules.

## 2. Pull versus eager

Evaluation is **eager collect, then return**. The consumer does not ask for the next Answer. A call of `pred input` finishes the whole walk before the caller sees any result.

Combinators in [[src/Shared/ExprEval.fs]]:

- `bind` — `left input |> List.collect right`. The left list is fully built, then `right` runs on every element.
- `orEval` — `left input @ right input`. Both sides run to completion, then concatenate.
- `andEval` — binds `rights = right input` first, then walks the whole left list with a seen-list. Intersection needs the full right sequence.
- `notEval` — `match inner input with | [] -> [ input ] | _ -> []`. The inner predicate already built its full list.

Walks in [[src/Shared/ExprWalk.fs]] accumulate with `::` and reverse once:

- `collectStructural` / `structuralSearch` — Owned recursive descent, stop at Directory Node and Workspace Node, then `List.rev`
- `collectOwned` / `treeAnswers` — every Owned descendant, then `List.rev`
- `collectDesc` / `descendantAnswers` — depth-first through Children (Owned and Ref), visited set, then `List.rev`
- `walkContentChildren` / `contentSearch` — subsection search, then `List.rev`

`ExprCompile.eval` and `evalOutcome` apply `pred input` and hand the complete list to the consumer.

## 3. Continuation after one hit

There is none. After `pred input` returns, the only structure is the finished list. Run and Search cannot take the first N Answers from eval without computing the rest. Paging or early stop would need a new representation.

## 4. Where a pull enumerator would matter

Eager collection is the cost of `root descendant`, `tree` / `**`, `/` structural search, and `#` / `subsection`. Those walks visit every reachable Loaded Node that the row’s rule allows, then reverse. An Unloaded Node is already a miss (`childAnswers`, `ownedChildren`, `collectDesc`, `walkContentChildren`); pull would not Load, but it could stop the walk after N hits.

Run always materialises the full list: `ExprRun.planExpr` matches `Hits(_, answers)` and `materialise` folds every Answer into one `Op.Replace` of Children ([[src/Shared/ExprRun.fs]]). There is no first-N Run.

Search chapter 8 wants the dialog to fetch Answers as the user scrolls. Word Search already has a pull cursor: `ViewModelSearch.SearchCursor` and `takeResults` ([[src/Shared/ViewModelSearch.fs]]), used by [[src/Client/SearchDialog.fs]] at page size 12. Expression Search does not use that cursor. `ExprDialog.tryHits` calls `evalOutcome` and maps the full `ExprAnswer list` to `NodeSearchResult list`. `searchNodes` returns that whole list (or word Search with `takeResults Int32.MaxValue`). A pull enumerator at eval would let Expression Search page like word Search; today it cannot.

`AND` and `NOT` still force work even under pull: `AND` needs the right-hand Answers (as a set or full sequence) to filter the left; `NOT` needs to know whether the inner sequence is empty.

## 5. Spec: chapter 6 does not require pull

Chapter 6 of [[spec.md]] fixes only the sequence and its order. The explicit freedom is: “Lazy, eager, or backtracking evaluation is an implementation freedom, because the rules fix only the sequence and its order.” The same lock is on [[.scratch/expression-language/issues/05-how-multiple-answers-surface.md]]. Chapter 2 says a term denotes an ordered sequence (Prolog-style fail/succeed/backtrack as the sequence), not a machine representation.

Chapter 8 Search “fetches Answers as the user scrolls” is a consumer display rule, not an eval-layer requirement. Run disposes the whole sequence as Children in Answer order; that consumer can stay eager.

## Eager collection site (one sentence)

Pull is absent: the collection site is `ExprEval.Predicate` returning `ExprAnswer list`, filled by the collect-then-`List.rev` walks in [[src/Shared/ExprWalk.fs]] and forced by `ExprCompile.evalOutcome` (`Hits(_, pred input)`).

## WORK.md mutations

None. This is an investigation report. A later ticket to change `Predicate` to a pull enumerator would be new work; do not add it unless the user asks.
