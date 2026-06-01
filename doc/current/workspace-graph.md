# Workspace Graph – implemented baseline

Category: Graph model
See also: [[doc/roadmap/workspace-file-model.md]], [[doc/roadmap/workspace-stage-plan.md]], [[doc/arch.md]]

The shared graph model includes vocabulary and structural rules for workspace-related special
nodes, enforced at the graph layer.

## Canonical special nodes

Stable `NodeId` values (see `Graph` in `src/Shared/Model.fs`):

| Node | `NodeId` suffix | Text | Kind |
|------|-----------------|------|------|
| Root | `00000000-0000-0000-0000-000000000000` | `ROOT` | `Normal` |
| Trash | `…000000000001` | `Trash` | `Special Trash` |
| Workspaces | `…000000000002` | `Workspaces` | `Special Workspaces` |

Every graph built via `Graph.fromNodes` or `Graph.create` has:

- exactly one Owner child `Workspaces` under root
- exactly one Owner child `Trash` under root
- order: user root children first, then `Workspaces`, then `Trash`

`Workspaces` and `Trash` cannot be edited (`setText`, `setClasses`) or removed from root
(`replace` on root rejects their removal or duplication).

## Structural invariants

Enforced in `Graph.replace` (not by a separate command layer):

| Node kind | Allowed owner parent |
|-----------|----------------------|
| `Workspaces` | root only (permanent, canonical) |
| `Workspace` | `Workspaces` only |
| `Directory` | `Workspace` or `Directory` |
| `File` | `Workspace` or `Directory` |
| `Normal` | anywhere |

`Workspaces` and `Trash` may not appear as children of any non-root parent.

Tests: `tests/Shared.Tests/ModelTests.fs` (workspaces bootstrap and placement cases).

## Bootstrap and round-trip

- `Graph.fromNodes` calls `ensureWorkspacesNode` then `ensureTrashNode` before rebuilding parent
  maps.
- `Snapshot.write` / `Snapshot.read` use canonical sid `#WORKSPACES` (parallel to `#TRASH`).
- `GraphProjection.graphFromPersistence` assigns `Special Workspaces` when node id matches
  `Graph.workspacesId`.

Existing graphs and empty outlines pick up the `Workspaces` node on load; no migration step is
required.

## Serialization

`Serialization.encodeNodeKind` / `decodeNodeKind` support all `SpecialKind` discriminators
(`workspaces`, `workspace`, `directory`, `file`, `trash`). Kind is stored on each node in the
graph JSON payload.

