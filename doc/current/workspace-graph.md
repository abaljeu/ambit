# Workspace Graph – implemented baseline

Category: Graph model
See also: [[doc/current/workspace-local-mapping.md]], [[doc/current/desktop-local-files.md]],
[[doc/roadmap/workspace-file-model.md]], [[doc/roadmap/workspace-stage-plan.md]], [[doc/arch.md]]

The shared graph model includes vocabulary and structural rules for workspace-related special
nodes, enforced at the graph layer.

## Canonical special nodes

Stable `NodeId` values (see `Graph` in `src/Shared/Model.fs`):

| Node | `NodeId` suffix | Text | Kind |
|------|-----------------|------|------|
| Root | `00000000-0000-0000-0000-000000000000` | `ROOT` | `Special Workspace` (nameless, `@:`) |
| Trash | `…000000000001` | `Trash` | `Special Trash` |
| Workspaces | `…000000000002` | `Workspaces` | `Special Workspaces` |

Every graph built via `Graph.fromNodes` or `Graph.create` has:

- exactly one Owner child `Workspaces` under root
- exactly one Owner child `Trash` under root
- order: user root children first, then `Workspaces`, then `Trash`

`Workspaces` and `Trash` cannot be edited (`setText`, `setClasses`) or removed from root
(`replace` on root rejects their removal or duplication).

## Context

A node's **context** is its ancestry along the ownership tree, considering only
`workspace`, `directory`, and `file` special nodes (`normal` nodes are skipped).
Context drives reference resolution; it does not restrict where nodes may be placed.

Authority: [[doc/roadmap/revising-workspace-file-model]].

## Structural invariants

Enforced in `Graph.replace` (not by a separate command layer).

Placement restrictions apply only to canonical workspace structure:

| Node kind | Allowed owner parent |
|-----------|----------------------|
| `Workspaces` | root only (permanent, canonical) |
| `Workspace` | `Workspaces` only |
| `Directory` | anywhere |
| `File` | anywhere |
| `Normal` | anywhere |

**ROOT** is the implicit nameless workspace (`@:`): `Special Workspace` with no filename.
Named `Workspace` nodes remain under `Workspaces` only. Ref links are unrestricted.

`Workspaces` and `Trash` may not appear as children of any non-root parent.

Below `Workspaces`/`Workspace` structural rules, the outline is free-form.
`Directory`, `File`, and `Normal` nodes may be owned by any parent.

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

## Workspace lifecycle (graph ops)

Workspace nodes are created and renamed through the general change op surface:

- **Create** — `Op.NewSpecialNode(nodeId, Special Workspace, name)` then `Op.Replace` to attach
  under `Graph.workspacesId`.
- **Rename** — `Op.SetName(nodeId, oldName, newName)` with `Graph.setName` validation (case-insensitive
  sibling uniqueness, invalid filename chars rejected).

Canonical `Workspaces`, `Trash`, and `ROOT` ids cannot be renamed. No dedicated workspace-removal
op in this stage.

Tests: `tests/Shared.Tests/WorkspaceOpsTests.fs`.

## Reference expressions (baseline)

Implementation in `RefExprTypes.fs`, `RefExprParse.fs`, `RefExprMatch.fs` (facade: `RefExpr.fs`).
Target grammar: [[doc/roadmap/reference-expression-interpretation.md]].

Implemented now:

- Anchors: context (no prefix), `/`, `//`, `.`, `^`, `#`, `@label:`.
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

Workspace nodes expose `@label:` as their desktop file path via `NodeDesktopPath` (used by the
file-status indicator). See [[doc/current/desktop-local-files.md]].

## Related desktop behavior

Local filesystem mapping for `@label:relative` paths:
[[doc/current/workspace-local-mapping.md]].

