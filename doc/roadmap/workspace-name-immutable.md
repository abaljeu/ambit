# Immutable workspace names

Category: Workspace scale
Status: Done
See also: [[doc/roadmap/workspaces]], [[doc/current/workspace-graph]], [[doc/history/workspaces/plans/lock_workspace_name_immutable_ea821a05.plan]], [[doc/roadmap/workspace-name-verbatim]]

## What it gives you

- After creation, a workspace node's name cannot change.
- Rename refuses when the target is `Special Workspace`: `Graph.setName` returns an error; `NodeRenameOps.isRenameAllowed` is false so the UI does not open a rename prompt.

## What it avoids for now

- Git sync, desktop mapping, remote labels, or teaching those paths to re-resolve after rename.
- Changes to [[doc/roadmap/workspace-name-verbatim]] (A/B only; separate work).
- Filename charset / allowing `@` in names.
- Path-move behavior for workspace renames (unreachable once rename is refused).

## Why (brief)

Live graph name drives document disk paths; git/desktop mapping caches a bare label. Locking the name at creation keeps those identities aligned without new state. History plan is ideas only; rewrite from this doc, do not cherry-pick discarded `db`.

## Minimal change

1. [[src/Shared/Model.fs]] `Graph.setName` — after node lookup, reject `kind = Special Workspace` (e.g. `"cannot rename a workspace"`). Existing `rootId` / `trashId` / `workspacesId` guards stay first; ROOT is already blocked by id.
2. [[src/Shared/DocumentPathMove.fs]] `NodeRenameOps.isRenameAllowed` — same kind check so F2 / rename never prompts for a workspace.
3. Callers already funnel through these (`Op.apply`/`undo` SetName → `setName`; [[src/Client/UpdateRename.fs]] → `isRenameAllowed` / `planRenameNode`). No Client-only guard.

## Tests

- [[tests/Shared.Tests/WorkspaceOpsTests.fs]]: retarget `` `SetName on special node updates name and text` `` off `Workspace` (e.g. `Directory`); add `` `SetName rejects renaming a workspace node` ``.
- [[tests/Shared.Tests/DocumentPathMoveTests.fs]]: `isRenameAllowed` false and `planRenameNode` Error for a workspace node.

## Docs after implement

- Record the rule in [[doc/current/workspace-graph]] when this ships.

## Non-goals

- Git gateway / desktop mapping code or docs beyond naming the divergence risk above.
- Verbatim `@` removal, import/export, format work.
- Replaying `db` commits.
