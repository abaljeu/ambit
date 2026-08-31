# `ref` / `owned` — notes for issue 32

Issue: [[../issues/32-ref-and-owned-children.md]]. Spec pointer: [[../spec.md]] chapter 7 reserved row. Product F# is not in this slice: `child` is not a trivial copy.

## Why not copy `childRow`

[[src/Shared/ExprPrimitive.fs]] `childRow` calls `ExprWalk.childAt graph None`. [[src/Shared/Model.fs]] `Node.childIds` maps `ChildNode` to ids and drops `Ownership`. `ref` and `owned` need `ChildNode.ref`.

Walk like [[src/Shared/ExprWalk.fs]] `childAnswers`: Unloaded → empty; Loaded → resolve each Child in order with `Map.tryFind`. Filter `Ownership.Ref` vs `Ownership.Owner`. Same miss if the id is absent from `graph.nodes`. Do not Load. Do not walk descendants.

`ownedChildren` already filters Owned for `tree` / `OUTER` recursion. Catalog `owned` is that filter at depth one. Catalog `ref` is the complement on the same immediate list. Neither is `descendant` or `OUTER`.

## Partition

Every Child appearance is exactly one of Owned or Ref. `child` is those appearances in Children order. `owned` and `ref` are the two filtered subsequences. Merging them in original order equals `child`. `owned OR ref` concatenates the two sequences and is not `child` when the list interleaves the roles.

## Spoken vs catalog

[[CONTEXT.md]] **Ref** and **Owned** remain the appearance-role words. Catalog spellings are `ref` and `owned` as locked. `Ref` / `Owned` as Name tokens are unknown words until some later row registers them.
