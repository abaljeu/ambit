# PR 157 Standards re-review

Range: `69930a91...dbb52d77`. Mechanical scan: no binding over 40 lines; no new F# line over 100 characters; no file over 800 lines.

## 1. Branch names as delivery status (hard)

[planning-docs.md](.agents/rules/planning-docs.md) says: plan text records what is implemented by ticket, section, or Point — not by git branch names. Do not discuss `dev`, `ready`, `master`, or other branches as delivery status in plan or arch docs. The new comment on [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md) uses `staging` and `origin/ready` as delivery status: `- 2026-09-28: staging includes origin/ready (fb83e3ce) via merge 69930a91. Ready added git Load after-step / directory-match helpers … Keep that Load slice on this branch.`

## 2. Duplicated special-node predicate (judgement)

Smell: Duplicated Code ([SMELLS.md](.agents/skills/code-review/SMELLS.md)). Judgement call, not a size-limit fail. [SpecialNodeState.fs](src/Shared/SpecialNodeState.fs) adds `isSpecialAxisNode` with the same File|Directory|Workspace test as `GraphQuery.isArtifact` / `NodeKind.artifact`:

```
let isSpecialAxisNode (node: Node) : bool =
    match node.kind with
    | Special (Workspace | Directory | File) -> true
    | _ -> false
```

[GraphQuery.fs](src/Shared/GraphQuery.fs) already has `let isArtifact (node: Node) : bool = NodeKind.artifact node.kind`. `enclosingSpecial` can pass that predicate to `GraphQuery.enclosing`. The new walk is not a custom scan; the predicate is the duplicate.
