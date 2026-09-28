# PR 157 Ticket 20 independent re-review

Range: three-dot `69930a91...dbb52d77` (`staging` tip `69930a91` Merge origin/ready into staging; PR tip `dbb52d77` on `cursor/state-axes-on-special-nodes-889e`). Spec: [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md). Axis reports: [pr-157-rereview-standards.md](pr-157-rereview-standards.md), [pr-157-rereview-spec.md](pr-157-rereview-spec.md). Ticket Status left unchanged.

## Standards

### 1. Branch names as delivery status (hard)

[planning-docs.md](.agents/rules/planning-docs.md) says: plan text records what is implemented by ticket, section, or Point — not by git branch names. Do not discuss `dev`, `ready`, `master`, or other branches as delivery status in plan or arch docs. The new comment on [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md) uses `staging` and `origin/ready` as delivery status: `- 2026-09-28: staging includes origin/ready (fb83e3ce) via merge 69930a91. Ready added git Load after-step / directory-match helpers … Keep that Load slice on this branch.`

### 2. Duplicated special-node predicate (judgement)

Smell: Duplicated Code ([SMELLS.md](.agents/skills/code-review/SMELLS.md)). Judgement call, not a size-limit fail. [SpecialNodeState.fs](src/Shared/SpecialNodeState.fs) adds `isSpecialAxisNode` with the same File|Directory|Workspace test as `GraphQuery.isArtifact` / `NodeKind.artifact`:

```
let isSpecialAxisNode (node: Node) : bool =
    match node.kind with
    | Special (Workspace | Directory | File) -> true
    | _ -> false
```

[GraphQuery.fs](src/Shared/GraphQuery.fs) already has `let isArtifact (node: Node) : bool = NodeKind.artifact node.kind`. `enclosingSpecial` can pass that predicate to `GraphQuery.enclosing`. The new walk is not a custom scan; the predicate is the duplicate.

## Spec

### 1. Client Load does not mark Unparsed

The spec says: "Client Load on Directory — Mark the Directory Node **Unparsed** (re-process)." ([Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4 item 8) The spec also says: "Client Load on File — Mark the File Node **Unparsed**." ([Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4 item 9) [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md) Notes items 8 and 9 record the same writes. Mikado: "Set both the new axes and the old `DocumentState` wherever state changes." ([Here→There — step 1 locked](plan/github-transport/here-to-there.md) §3)

[gitLoadAfterOp](src/Client/UpdateWorkspaceLoad.fs) starts File Parse or Workspace directory-match. [runLoad](src/Server/GithubTransportActor.fs) pulls, then [reconcileWorkspace](src/Server/LazyLoadReconciliationServer.fs), then [parseFocusFile](src/Server/GithubTransportActor.fs). Those Load sites do not set `ParseState.Unparsed` or `DocumentState` Unparsed on the Directory Node or File Node that Load selected. The diff does not change those files. Ticket 20 axes are missing on top of existing Load.

### 2. Discovery does not mark Directory Unparsed for a new or deleted member

The spec says: "A new member or a deleted member marks the Directory Node **Unparsed**. A modified file marks the File Node **Unparsed**." ([Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4 item 3) Map decision 23 Axis-write mechanics locked uses the same rule. ([map.md](plan/github-transport/map.md))

Modified files call `markUnparsed`; [setDocumentState](src/Shared/GraphMutate.fs) dual-writes parse Unparsed. New members: [markAddedDocumentsUnparsed](src/Shared/dotnet/LazyLoadReconciliationApply.fs) marks stubs Unparsed and the parent Directory or Workspace Current (parse Parsed). Deleted members: [planDeletedInfo](src/Shared/dotnet/LazyLoadReconciliation.fs) moves the node to trash and does not mark the Directory Unparsed. [currentDiscoveredAsModified](src/Shared/dotnet/LazyLoadReconciliation.fs) reads `documentState = Current` only. The diff does not add parse Unparsed on those Directory discovery writes.

## Summary

Standards: 2 findings (worst: branch names as delivery status). Spec: 2 findings (worst: Client Load does not mark Unparsed). Spec did not flag create dual-write (`Op.NewSpecialNode` DocumentState Unparsed + parse Unparsed + persist Persisted).
