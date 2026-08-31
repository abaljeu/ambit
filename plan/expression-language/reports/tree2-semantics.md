# tree2 semantics — ancestor-pruning selection

Design only. No product code. Home is [[plan/expression-language/]] because Expression eval, `tree`, and after-filters live there. Written on project branch `w/tree2-semantics`, cut from `selective-client-sync`. See [[plan/expression-language/spec.md]], [[src/Shared/ExprWalk.fs]], [[src/Shared/ExprEval.fs]], [[src/Shared/ExprCompile.fs]], [[src/Shared/ExprPrimitive.fs]].

Working name in this report: `tree2` (history only). Catalog spelling is locked: `OUTER` (capitals, same class as `NOT`). Do not replace the `tree` / `**` row.

## Canonical algorithm

This algorithm is a design requirement, not a preference.

```
foreach Owned child N of the current Node, depth-first in Children order {
  if acceptable(N) then yield N and do not visit descendants of N
  else recurse on the Owned Children of N
}
```

Implications:

- The predicate `acceptable` must be available during the walk. The walk asks `acceptable(N)` at each visited Node.
- Accepting a Node prunes that subtree at once. Descendants of an accepted Node are never considered. That is both the efficiency and the meaning.
- A Node that is not acceptable does not yield. Search continues in its Owned Children. A matching descendant under a non-matching ancestor is found.
- A post-pass that first materializes `tree`, then filters, then drops descendants of remaining Answers is the wrong shape. It does extra work. It does not match this algorithm.

`tree` starts strictly below the input (it does not yield the input). `OUTER` must do the same. The walk is Owned only. It does not follow Ref. An Unloaded Node is a miss and is never Loaded, same as [[plan/expression-language/spec.md]] chapter 6.

## Current semantics

Live Run uses [[src/Shared/ExprRun.fs]], not the old `>` path in [[src/Shared/AmbleParse.fs]] / [[src/Shared/AmbleEval.fs]]. [[src/Shared/AmbleRun.fs]] calls `ExprRun.run` first.

Every term is a function from one input Answer to a sequence of Answers ([[plan/expression-language/spec.md]] chapters 2 and 6). Juxtaposition is monadic bind:

```
E⟦e1 e2⟧ x = concat [ E⟦e2⟧ y | y ← E⟦e1⟧ x ]
```

`tree` / `**` is a generator with no slot. [[src/Shared/ExprWalk.fs]] `treeAnswers` pulls Owned Children depth-first and yields every reachable Node. It does not test a predicate. It does not skip a subtree.

`containing`, `named`, `class`, `section`, and the Kind filters are pure filters. Each sees one input Answer and keeps or drops that Answer only. [[src/Shared/ExprWalk.fs]] `containing` tests Header text. It does not know other Answers.

`AND` / `OR` / `NOT` are combinators. `NOT` already takes a predicate operand and treats that operand as succeed-or-fail by emptiness ([[src/Shared/ExprEval.fs]] `notEval`). Juxtaposition never shares state across Answers.

`(tree containing "blue")` therefore means: generate every Owned descendant, then keep those whose Header contains `blue`. Nested matches stay. That is accepted behavior for `tree`.

The nearest existing wall is subsection search (`#`). A matching section yields and does not enter Children. A non-matching named section is also a wall. `OUTER` is different: a non-acceptable Node is transparent, not a wall.

## Why a `tree` replacement fails

`tree2` as a second generator with the same signature as `tree` (`Node ⇒ Node`, no slot) cannot encode the algorithm.

Bind applies the next word to each yielded Answer independently. A generator that yields every Node, or that yields some Nodes without a predicate, cannot ask `acceptable(N)` at visit time. The after-function (`containing`) runs only after that Node has already been yielded into the bind stream. By then the walk has already entered, or will enter, the descendants.

There is no current semantic for “drop this Node because another Node was already accepted.” Catalog rows do not see the sibling or ancestor Answers. `AND` intersects two predicates on the same input. It does not prune a sequence by ancestry.

Swapping the spelling `tree` for `tree2` in `(tree2 containing "blue")` still parses as bind. The algorithm never runs.

## Ancestry on Answers

[[src/Shared/ExprAnswer.fs]] is `Node` or `Text`. A Node Answer carries Node identity, not an occurrence path and not a parent id.

The Graph does have Owned ancestry: `ownerParentByChild` on [[src/Shared/Model.fs]] Graph, walked by [[src/Shared/GraphQuery.fs]] `enclosing`. A later pass could recover the owner chain from the Graph.

That fact does not save a post-pass design. Recovering ancestry after `tree` plus filters still materializes the full walk and still considers descendants of accepted Nodes. The canonical algorithm forbids both.

## Alternative designs

### A — Prefix combinator that fuses the predicate into the walk (recommended)

Interface: a unary combinator with the same parse family as `NOT`. Locked spelling `OUTER`. Working name in this report: `tree2`.

```
OUTER : (Node ⇒ τ) → (Node ⇒ Node)
```

`acceptable(N)` is `E⟦inner⟧ N` nonempty. That is the `NOT` emptiness test, inverted. The operand may be any predicate. The common case is a pure filter.

Usage:

- `OUTER containing "blue"`
- `root OUTER containing "blue"`
- `OUTER (containing "blue" AND named "x")` — prune on the conjunction; parentheses match the `NOT` rule
- `OUTER containing "blue" named "x"` — walk with `containing` only; `named` then filters the yielded (already pruned) set

What it hides: the Owned depth-first walk, the Unloaded miss, and the decision to skip Children after a hit. The caller writes a predicate, not a walk.

This is the only design that is the canonical algorithm.

### B — Postfix sequence prune after any left Expression (rejected)

Interface: `outermost` wraps a left predicate, materializes its Answers, then drops Nodes whose Owned ancestor is also in that set.

```
tree containing "blue" outermost
```

This matches the motivating Answer set when the left side is `tree` plus pure filters only. It is the wrong shape. It visits descendants of accepted Nodes. It does extra work. The requirement forbids it. Do not add this combinator as the `tree2` meaning.

### C — String-slot walk row (too narrow)

Interface: a generator with a quoted slot, `OUTER "blue"`, meaning the algorithm with `containing` fused and no other predicate.

This can implement the algorithm for one filter. It does not generalize to `named`, `class`, `AND`, or `NOT`. A later sugar `OUTER "blue"` for `OUTER containing "blue"` is optional. It is not the catalog row.

### D — Consumer post-pass or path-carrying Answers (rejected)

Interface: keep `(tree containing "blue")` as bind; Run or Find drops descendant Answers; or extend `ExprAnswer` with an Owned path so a consumer can prune.

Prune then sits outside the Expression. Nested matches still exist as Answers. The walk still visits pruned subtrees. Wrong layer and wrong algorithm.

## Comparison

The catalog core says every term is a function of one input Answer ([[plan/expression-language/spec.md]] chapter 1). Designs B and D operate on a finished sequence. They need ancestry after the fact. Design C is a shallow row that hides a walk but cannot take a general `acceptable` test. Design A is a combinator, like `NOT`: the operand is the predicate, and the walk is internal.

Interface simplicity favors A: one reserved word, one operand, no new Answer type. Depth favors A: a small surface hides the prune-during-walk. Ease of correct use favors A if `OUTER` is reserved and bare `OUTER` is a missing-operand parse error, same idea as bare `NOT`. Ease of misuse: `OUTER child` treats “has a Child” as acceptable. That is defined (nonempty inner stream) and is a poor default; the spec examples must show pure filters.

Design B is easier to bolt onto today’s bind without a new parse form, and that ease is exactly why it looks attractive. It is still the wrong algorithm.

## Recommendation

Lock Design A. Treat `OUTER` as a prefix combinator that takes one predicate operand and runs the canonical Owned walk. Do not add an `OUTER` generator row. Do not replace `tree`. Do not plan a sequence prune as the implementation.

Fuse versus after-combinator: fuse. A separate after-combinator on a full Answer list cannot ask `acceptable?` per Node during the walk. The requirement chooses fusion.

Locked spelling (user 2026-08-29: first "Let's go with outer" and "plan A", then capitals for consistency with `NOT`): `OUTER`. `tree2` is not a catalog name. `**` stays `tree`. No cluster spelling.

Recommended evaluation rule (spec chapter 6 shape):

```
E⟦OUTER e⟧ x = Owned depth-first walk strictly below x.
At each visited Node N:
  if E⟦e⟧ N is nonempty then yield N and do not enter Owned Children of N
  else enter Owned Children of N (Unloaded is a miss)
```

Recommended parse (spec chapter 4 shape): same attach as `NOT`.

```
NotExpr ::= "NOT" NotExpr | "OUTER" NotExpr | Seq (("NOT" | "OUTER") NotExpr)?
```

Then `root OUTER containing "blue"` is `Seq[root]` then `OUTER` of `containing "blue"`, not bind of a generator named `OUTER`. Compound operands need parentheses, same as `NOT`.

### Motivating example

`root OUTER containing "blue"`:

- A Node whose Header contains `blue` yields. Its Owned descendants are not visited.
- A Node whose Header does not contain `blue` does not yield. The walk continues in its Owned Children.
- Two sibling matches both yield.
- A match under a non-match yields.
- ROOT is not in the walk (strictly below), same as `root tree`.

### Walk order and siblings

Order is the `tree` order: depth-first, Owned Children order ([[plan/expression-language/spec.md]] chapter 7 `tree` row). Sibling matches are independent. Nested match under a match never runs because the parent already pruned.

### Generalization beyond `containing`

The algorithm’s `acceptable` is any inner predicate, not a special case of `containing`. All after-functions that can be an operand of `NOT` can be an operand of `OUTER`. Pure filters are the intended use. Generators are allowed by the emptiness rule and are usually a mistake.

### Name

`tree2` is not domain-accurate. The operation is outermost acceptable Nodes under Owned descent, with transparent non-matches. Spelling is locked `OUTER`. Do not reopen fusion.

## Locked 2026-08-29

User confirmed Design A ("plan A"). First spelling lock was lowercase `outer`; later the same day the catalog word became `OUTER` (capitals, same class as `NOT`). Spec lock: [[plan/expression-language/spec.md]] chapters 4, 6, 7, and 11; implementation issue [[plan/expression-language/issues/28-outer-prefix-combinator.md]]. Worker report: [[plan/expression-language/reports/outer-spec-lock.md]].

- Spelling: catalog word `OUTER`, reserved so it is not bind. Not `cut`. Not `tree2` as a catalog name. Not lowercase `outer`.
- Fusion: fuse the predicate into the Owned walk. Do not implement a post-pass prune.
- Sugar `OUTER "blue"` for `OUTER containing "blue"` is out of this slice ([[plan/expression-language/spec.md]] chapter 10).
- Ref analog (descendant-shaped wall) is out of this slice. Owned only.

## Planning artifacts

Spec lock is done. [[doc/]] is unchanged. Implementation is [[plan/expression-language/issues/28-outer-prefix-combinator.md]]. Tests belong next to existing Expr facts.

[[plan/expression-language/project.md]] stays `active`. This report does not change project stage.
