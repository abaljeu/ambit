# 32 — Catalog `ref` and `owned` (partition of `child`)

**Context:** Two generator rows, same walk start as `child`: immediate Children of the input Node, in Children order. `ref` keeps appearances whose Graph edge is Ref. `owned` keeps appearances whose Graph edge is Owned. Together they partition `child` (Owned ∪ Ref = Children). Not OUTER. Not text operations. Not descendants. Work on branch `w/tree2-semantics`.

**What to build:** Catalog rows `ref` and `owned` per [[../spec.md]] chapter 7 reserved pointer and this issue. Filter the input Node's Children list ([[src/Shared/Model.fs]] `ChildNode.ref`: `Ownership.Ref` vs `Ownership.Owner`). Unloaded is a miss and never Loads, same as spec chapter 6 and existing `child`. Do not implement [[30-text-operations.md]]. Do not change `OUTER`, `tree`, or `descendant`.

**Blocked by:** none. [[28-outer-prefix-combinator.md]] and [[31-if-pullback.md]] are done. Not blocked by [[30-text-operations.md]].

**See also:** [[../spec.md]] chapters 6 and 7 (`child`, Unloaded rule; reserved `ref` / `owned`); [[12-owned-versus-ref-walk-for-descendant.md]]; [[CONTEXT.md]] Children, Owned, Ref; [[src/Shared/ExprWalk.fs]] `childAnswers` / `ownedChildren`; [[src/Shared/ExprPrimitive.fs]] `childRow`; tests next to existing child/walk facts in [[tests/Shared.Tests/ExprEvalTests.fs]] and [[tests/Shared.Tests/ExprPipelineTests.fs]]. Notes: [[../reports/ref-owned-children.md]].

**Status:** ready-for-agent

- [ ] `owned` yields only Owned Children of the input, in Children order; immediate only (not descendants, not `tree`).
- [ ] `ref` yields only Ref Children of the input, in Children order; immediate only (not descendants, not `descendant`).
- [ ] On a mixed parent, `child` equals the Children-order merge of `owned` and `ref` (every Child appearance is in exactly one of the two). `owned OR ref` is not `child` (OR concatenates whole sequences).
- [ ] Unloaded input: `child`, `owned`, and `ref` all miss; evaluation does not Load.
- [ ] Spellings are exactly `ref` and `owned` (lowercase Name tokens, same class as `child` / `tree`). No slot. Text input is a miss.

## Comments

The Graph child list is the source of truth: each `ChildNode` is one appearance with `ref: Ownership`. Owned is the structural placement. Ref links to a Node Owned elsewhere. The catalog words select those appearances; they do not introduce a Kind.

[[CONTEXT.md]] already uses **Ref** and **Owned** as spoken domain words for those roles. The user locked the catalog spellings as lowercase `ref` and `owned`. Keep the spoken terms for the roles. The catalog words are walk generators that filter Children by those roles.

`child` is not a trivial copy for product F#. `childRow` calls `ExprWalk.childAt graph None`, and `Node.childIds` drops `ChildNode.ref`. Filter the Children list the same way `childAnswers` walks it (Unloaded → empty; Loaded → `Map.tryFind` each id). `ownedChildren` in [[src/Shared/ExprWalk.fs]] is the Owned filter used by `tree` / `OUTER` for recursion; `owned` is that filter at depth one only.

Existing fixture in [[tests/Shared.Tests/ExprPipelineTests.fs]] (`child equals colon-star; descendant follows Ref; tree matches star-star`): File Node has three Owned Children plus one Ref to `outside`. From that File Node: `owned` is the three Owned; `ref` is `outside`; `child` is those four in Children order.

Examples from that parent (Owned A, B, C then Ref D):

- `child` → A, B, C, D
- `owned` → A, B, C
- `ref` → D
- `owned child` walks Children of each Owned Answer (descendants through Owned), not the partition of the original input
