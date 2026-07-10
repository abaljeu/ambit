---
name: Lock workspace name immutable
overview: Make a workspace's name permanent from creation so the live-graph-name identity used by file/document persistence and the cached-label-string identity used by git/desktop mapping can never diverge.
todos:
  - id: setname-guard
    content: Add Special Workspace rename guard to Graph.setName in Model.fs
    status: pending
  - id: isrenameallowed-guard
    content: Add same guard to NodeRenameOps.isRenameAllowed
    status: pending
  - id: update-existing-test
    content: Switch WorkspaceOpsTests special-node rename test off Workspace kind
    status: pending
  - id: add-new-tests
    content: Add tests proving workspace rename is rejected (setName + isRenameAllowed/planRenameNode)
    status: pending
  - id: docs-update
    content: Record immutable-workspace-name rule in workspace-graph.md and git-sync-gateway.md
    status: pending
isProject: false
---

## Problem

Two identity mechanisms exist for "which workspace":

- **Document persistence** ([src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs), [src/Shared/NodeDesktopPath.fs](src/Shared/NodeDesktopPath.fs)) resolves the disk path fresh from the graph node's **live `name`** every time. Rename-safe by construction ([src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs) relocates the on-disk tree on rename).
- **Git / desktop mapping** ([src/Server/WorkspaceGit.fs](src/Server/WorkspaceGit.fs), [src/Shared/WorkspaceGitRemote.fs](src/Shared/WorkspaceGitRemote.fs), desktop `workspaceMappings` config, `/ambit/workspace-git-commit?label=`) takes a **bare label string**, captured once and never re-derived from the graph.

If a workspace is renamed, mechanism 1 moves the files; mechanism 2's cached label goes stale, silently pointing at a folder that no longer exists. Decision: **lock the name permanently at creation** rather than teaching the git/mapping side to re-resolve live — simplest fix, no new state.

## Code changes

1. **[src/Shared/Model.fs](src/Shared/Model.fs) — `Graph.setName`** (single authoritative choke point; `Op.apply`/`Op.undo` for `SetName` both call it): add a guard rejecting the rename when the target node's `kind = Special Workspace`, alongside the existing root/trash/workspaces-id guards.

```397:403:src/Shared/Model.fs
        if nodeId = rootId then
            Error "cannot modify canonical root name"
        elif nodeId = trashId then
            Error "cannot modify trash node name"
        elif nodeId = workspacesId then
            Error "cannot modify workspaces node name"
        else
```

   New branch (after node lookup, since kind requires the node) rejects when `node.kind = Special Workspace` with e.g. `"cannot rename a workspace"`.

   **Note on ROOT:** `Graph.rootId` is itself `kind = Special Workspace` (nameless, self-owned — see `Graph.rootPlaceholder` in [src/Shared/Model.fs](src/Shared/Model.fs)), distinct from named workspaces owned by `Graph.workspacesId`. The existing `nodeId = rootId` branch fires first and already unconditionally blocks it, so the new kind-based branch is only ever reached for named (non-root) workspaces — no root-specific carve-out needed, but call this out in the diff comment so it's not mistaken for an oversight.

2. **[src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs) — `NodeRenameOps.isRenameAllowed`**: add the same kind check, consistent with how the three canonical ids are already pre-blocked, so the `Rename` command (F2) never opens the prompt for a workspace node instead of silently failing on submit.

```170:174:src/Shared/DocumentPathMove.fs
    let isRenameAllowed (graph: Graph) (nodeId: NodeId) : bool =
        nodeId <> Graph.rootId
        && nodeId <> Graph.trashId
        && nodeId <> Graph.workspacesId
        && Map.containsKey nodeId graph.nodes
```

## Tests

- [tests/Shared.Tests/WorkspaceOpsTests.fs](tests/Shared.Tests/WorkspaceOpsTests.fs) — `` `SetName on special node updates name and text` `` currently renames a `Workspace`-kind node and expects success; switch it to a `Directory` (or `File`) kind node to preserve that coverage for kinds that remain renamable. Add a new fact `` `SetName rejects renaming a workspace node` `` asserting `Result.isError` for a `Workspace`-kind node.
- [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) — add a fact proving `NodeRenameOps.isRenameAllowed` is `false` and `planRenameNode` returns `Error` for a workspace node.

## Docs

- [doc/current/workspace-graph.md](doc/current/workspace-graph.md) — record "workspace name is immutable after creation" as a locked identity rule.
- [doc/roadmap/git-sync-gateway.md](doc/roadmap/git-sync-gateway.md) — note that this permanence is what keeps the git label and graph identity aligned (closes the divergence risk this plan was written to resolve).

## Verification

- `dotnet build tests/Shared.Tests -c Debug`
- `dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~WorkspaceOpsTests|FullyQualifiedName~DocumentPathMoveTests"`
