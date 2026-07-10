# Workspace Graph – implemented baseline

Category: Graph model
See also: [[doc/current/workspace-local-mapping.md]], [[doc/current/desktop-local-files.md]],
[[doc/roadmap/workspace-file-model.md]], [[doc/current/workspace-stage-plan.md]], [[doc/arch.md]]

The shared graph model includes vocabulary and structural rules for workspace-related special
nodes, enforced at the graph layer.

## Canonical special nodes

Stable `NodeId` values (see `Graph` in `src/Shared/Model.fs`):

| Node | `NodeId` suffix | Text | Kind |
|------|-----------------|------|------|
| Root | `00000000-0000-0000-0000-000000000000` | `ROOT` | `Special Workspace` (nameless) |
| Trash | `…000000000001` | `Trash` | `Special Directory` *(today)* → `Special Directory` with `Node.name = TRASH` *(Stage 6 target)* |
| Workspaces | `…000000000002` | `Workspaces` | `Special Workspaces` |

Every graph built via `Graph.fromNodes` or `Graph.create` has:

- exactly one Owner child `Workspaces` under root
- exactly one Owner child `Trash` under root
- order: user root children first, then `Workspaces`, then `Trash`

`Workspaces` and `Trash` cannot be edited (`setText`, `setClasses`) or removed from root
(`replace` on root rejects their removal or duplication). TRASH cannot be renamed (`setName` rejects `trashId`).

**Stage 6 target:** retire `SpecialKind.Trash`; TRASH becomes `Special Directory` with `Node.name = TRASH`. Same permanence and delete semantics (`MoveToTrash` reparents owner under `trashId`). Path: `//TRASH/`. UI trash styling maps by `trashId`, not kind. See [[doc/roadmap/workspace-file-model.md]] § TRASH.

## Context

A node's **context** is its ancestry along the ownership tree, considering only
`workspace`, `directory`, and `file` special nodes (`normal` nodes are skipped).
Context drives reference resolution; it does not restrict where nodes may be placed.

Authority: [[doc/roadmap/revising-workspace-file-model]].

## Structural invariants

Enforced in `Graph.replace` (not by a separate command layer).

Placement restrictions for owned children:

| Node kind | Allowed owner parent |
|-----------|----------------------|
| `Workspaces` | root only (permanent, canonical) |
| `Workspace` | `Workspaces` only |
| `Directory` | `Workspace` (ROOT or named) or `Directory` (including TRASH) |
| `File` | same as `Directory` |
| `Normal` | anywhere |

**ROOT** is the implicit nameless workspace: `Special Workspace` with no filename.
Named `Workspace` nodes remain under `Workspaces` only. Ref links are unrestricted.

`Workspaces` and `Trash` may not appear as children of any non-root parent.

Owned `File` / `Directory` may not sit under `Normal`, `File`, or the `Workspaces` container. See [[doc/roadmap/workspace-file-directory-placement]].

Tests: `tests/Shared.Tests/ModelTests.fs` (workspaces bootstrap and placement cases).

## Bootstrap and round-trip

- `Graph.fromNodes` calls `ensureWorkspacesNode` then `ensureTrashNode` before rebuilding parent
  maps.
- `Snapshot.write` / `Snapshot.read` use canonical sid `#WORKSPACES` (parallel to `#TRASH`).
- `#TRASH` owner lines include name token `TRASH` when Stage 6 lands (today: no name token on Trash owner line).
- `GraphProjection.graphFromPersistence` assigns `Special Workspaces` when node id matches
  `Graph.workspacesId`.

Existing graphs and empty outlines pick up the `Workspaces` node on load; no migration step is
required.

## Serialization

`Serialization.encodeNodeKind` / `decodeNodeKind` support all `SpecialKind` discriminators
(`workspaces`, `workspace`, `directory`, `file`, `trash`). Kind is stored on each node in the
graph JSON payload. Stage 6 retires the `trash` discriminator; TRASH persists as `directory` with `Node.name = TRASH`.

## Workspace lifecycle (graph ops)

Workspace nodes are created through the general change op surface; names are fixed at creation:

- **Create** — `Op.NewSpecialNode(nodeId, Special Workspace, name)` then `Op.Replace` to attach
  under `Graph.workspacesId`.
- **Rename** — workspace names are immutable after creation. `Graph.setName` rejects
  `Special Workspace` (`"cannot rename a workspace"`); `NodeRenameOps.isRenameAllowed` is false
  so F2 / Rename does not open a prompt. Directory, File, and Normal nodes still rename via
  `Op.SetName` with `Graph.setName` validation (case-insensitive sibling uniqueness, invalid
  filename chars rejected). Named workspaces and Root-owned Files/Directories also share one
  case-insensitive DataDir top-level namespace (`Graph.replace` / `Graph.setName` reject;
  create planners auto-rename via `Graph.takenOwnedNamesLower`). Nested File/Directory names
  are not in that namespace.

**Stage 6 target — Insert…:** create workspace under `Workspaces`, or `Special Directory` / `Special File` as owner child of focus; pick-existing insert via search unchanged.

**Stage 6 target — Rename (F2):** `Op.SetName` for directory, file; normal nodes rename `Node.name` only. Workspace rename remains refused. Edit node keeps Enter only.

Canonical `Workspaces`, `Trash`, and `ROOT` ids cannot be renamed. No dedicated workspace-removal
op in this stage. Soft delete reparents owner under `trashId` (`MoveToTrash`).

Tests: `tests/Shared.Tests/WorkspaceOpsTests.fs`.

## Reference expressions (baseline)

Implementation in `RefExprTypes.fs`, `RefExprParse.fs`, `RefExprMatch.fs` (facade: `RefExpr.fs`).
Target grammar: [[doc/roadmap/reference-expression-interpretation.md]].

Implemented now:

- Anchors: context (no prefix), `/`, `//`, `.`, `^`, `#`.
- Path steps: `DirStep` (`name/`), `FileStep` (`name`), `**`, glob patterns in names.
- Tag steps: `#name` matches named `normal` nodes by `Node.name` within content scope.
- `refContext` walks the owner chain for workspace root, current directory, structural container,
  and tagged normal ancestor. Workspace root falls back to **ROOT** when none is found.
- `match_` resolves expressions against the graph for search.

Not implemented: postfixes (`.text`, `[n]`, filters), command/assignment syntax, view-root anchor.

## Reference search

Search dialog merges two result sources (`src/Shared/ViewModelSearch.fs`):

1. **RefExpr matches** — namespace-style queries parsed and matched first.
2. **Text search** — existing node text matching.

Workspace nodes expose `//label` as their desktop file path via `NodeDesktopPath` (used by the
file-status indicator). See [[doc/current/desktop-local-files.md]].

## Related desktop behavior

Local filesystem mapping for `//label/relative` paths:
[[doc/current/workspace-local-mapping.md]].

