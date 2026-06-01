# Workspace File Model

Status: Draft staged design
Authority: Design intent only; this document defines target model and persistence rules for workspace, directory, and file identity.
See also: [[doc/roadmap/reference-expressions.md]], [[doc/roadmap/persistence-vs-domain-model.md]], [[doc/arch.md]]

This document defines the model concepts needed by reference expressions such as `@bobby:`.
It is about shared identity and persistence shape, not source-level implementation.

Scope note: this is a target-scope design document.
Current implemented behavior is summarized in [[doc/current/workspace-graph.md]].
Stage implementation scope is defined separately in
[[doc/roadmap/workspace-stage-plan.md]], which is intentionally narrower.

## Purpose

The existing graph model already defines node identity and owner/ref semantics.
What it does not define is how a graph node belongs to a file, how a file belongs to a
directory tree, and how that directory tree relates to a user-visible workspace label such as
`@bobby:`.

The goal is to add those concepts without changing the current graph ownership rules.

## Status Tracking

This document describes the target model, but implementation is expected to land in stages.
To keep the document useful during that process:

- `[x]` means implemented in the current codebase
- `[~]` means partially implemented or represented in the model, but not yet wired through
- `[ ]` means target design only

When a section below describes the target end state, that does not by itself mean it is already
implemented. The stage list below is the current implementation summary.

## Implementation Stages

- `[x]` Stage 1: model vocabulary exists for workspace, directory, and file node kinds in the
   shared model.
- `[~]` Stage 2: graph invariants and operations understand workspace, directory, and file nodes as
   distinct behavior-bearing concepts.
- `[~]` Stage 3: shared persistence stores canonical workspace-label -> workspace-root mapping only.
- `[~]` Stage 4: desktop-local API resolves workspace label + relative path via readonly local mapping.
- `[ ]` Stage 5: client UI uses desktop query surface and shows unresolved-reference indicators.
- `[ ]` Stage 6: explicit user commands create and modify workspace/file/directory structure.

## Current Implementation Snapshot

- `[x]` `SpecialKind` includes `Workspace`, `Directory`, and `File` in the shared model.
- `[x]` `workspacesId` canonical node exists with `kind = Special Workspaces`.
- `[x]` `Workspaces` is permanent under root (cannot be removed or edited, like Trash).
- `[x]` Graph invariants enforce structural placement rules (see Structural Invariants below).
- `[~]` Desktop-local workspace label → local root mapping: JSON format defined, encode/decode/load/save/resolvePath
  implemented (`src/Shared/WorkspaceLocalMapping.fs`). HTTP endpoint surface not yet wired.
- `[ ]` No directory/file path mapping is persisted yet.
- `[ ]` No reference-expression resolver uses these concepts yet.
- `[ ]` No visual unresolved-reference indicator is implemented yet.
- `[ ]` No user command surface exists yet for workspace/file/directory creation or maintenance.

## Settled Decisions

1. Workspace labels such as `@bobby:` are shared, user-visible, stable, and not intended to be
   renamed casually.
2. A client may define a local filesystem mapping for a workspace label, or ignore that label.
3. Shared persistence stores workspace label plus workspace-relative path.
4. Desktop-local configuration stores workspace label to absolute local root mappings.
5. Directory nodes are first-class unique nodes.
6. File nodes are first-class unique nodes.
7. Every ordinary outline node belongs to exactly one file node through the owner chain.
8. Existing owner/ref semantics remain the containment model for the graph.
9. Canonical workspace-relative path identity is case-insensitive.
10. Path resolution follows normal operating-system-style path matching, including `?` and `*`
   wildcards.
11. Workspace, directory, and file nodes change only through explicit user commands and normal
   cross-client graph synchronization.
12. There is no automatic background alignment between the graph and any client's local
   filesystem.
13. Workspace, directory, and file nodes are represented as `Special Workspace`,
   `Special Directory`, and `Special File`.
14. Canonical path normalization follows standard operating-system normalization rules.
15. Unresolved workspace or path references are surfaced to the user with a visual indicator.
16. Existing graphs do not require automatic migration to introduce this model.
17. `Workspaces` is a permanent canonical node under root, identified by a fixed `NodeId`, with the
   same permanence semantics as `Trash`.
18. `Workspace` nodes may only exist as direct children of the `Workspaces` node (drive semantics —
   one level deep).
19. `Directory` and `File` nodes may only exist as children of a `Workspace` node or another
   `Directory` node (standard filesystem directory semantics).
20. Normal nodes may be placed anywhere in the graph without structural restriction.

## Structural Invariants

These placement rules are enforced by `Graph.replace` at the graph layer:

| Node kind | Allowed owner parent |
|---|---|
| `Workspaces` | root only (permanent, canonical) |
| `Workspace` | `Workspaces` only |
| `Directory` | `Workspace` or `Directory` |
| `File` | `Workspace` or `Directory` |
| Normal | anywhere |

No command surface for creating `Workspace`, `Directory`, or `File` nodes exists yet (Stage 6).

## Model Entities

### Workspace

A workspace is identified by a stable label such as `bobby` and is referenced in expressions as
`@bobby:`.

A workspace has:

- a workspace label
- a unique workspace root node in the graph
- zero or more client-local absolute root mappings

The workspace label is shared data. The absolute root mappings are not.
Workspace nodes use `NodeKind = Special Workspace`.

### Directory Node

A directory node represents one canonical directory within a workspace.

A directory node is identified by:

- workspace label
- canonical workspace-relative directory path

There is at most one directory node for a given `(workspace label, relative directory path)`.
Directory nodes use `NodeKind = Special Directory`.

### File Node

A file node represents one canonical file within a workspace.

A file node is identified by:

- workspace label
- canonical workspace-relative file path

There is at most one file node for a given `(workspace label, relative file path)`.

Each file node owns the outline subtree for that file.
File nodes use `NodeKind = Special File`.

### Ordinary Outline Node

An ordinary outline node is any non-workspace, non-directory, non-file node in the existing
graph.

Each ordinary outline node must have exactly one owning file node by following owner links upward.
Ref edges may point at the node from other places, but they do not change file membership.

## Shared Versus Local Data

### Shared Data

The shared model and shared database are responsible for:

- workspace labels
- workspace root nodes
- directory nodes
- file nodes
- canonical workspace-relative paths
- the ownership relation from workspace to directory/file metadata nodes
- the ownership relation from file nodes to ordinary outline nodes

### Client-Local Data

Each desktop client is responsible for:

- choosing whether to handle a workspace label
- mapping a handled workspace label to an absolute local filesystem root
- deciding that a workspace label is currently unavailable on that machine

Two clients may map the same shared workspace label to different absolute roots.

## Identity And Invariants

The following invariants define the model:

1. Workspace labels are unique within the shared graph.
2. Each workspace label has exactly one workspace root node.
3. Each canonical workspace-relative directory path has exactly one directory node within its
   workspace.
4. Each canonical workspace-relative file path has exactly one file node within its workspace.
5. Each ordinary outline node belongs to exactly one file node through the owner chain.
6. Renaming or moving a file or directory changes path metadata but does not change node identity.
7. Ref edges never change workspace membership, directory membership, or file membership.
8. The model is not changed automatically by filesystem observation alone.

## Materialization And Change Rules

Workspace, directory, and file nodes are created, renamed, moved, or removed only by:

- explicit user commands
- normal cross-client graph synchronization of those user commands

The model is not reconciled automatically against any local filesystem view.
In particular:

- there is no background import of new files or directories
- there is no background deletion because a file disappeared locally
- there is no automatic repair to make the graph match the filesystem
- there is no automatic repair to make the filesystem match the graph

Clients may observe that a local mapping is missing or stale, but that observation alone does not
mutate the shared graph.

## Canonical Paths

Shared persistence uses canonical paths relative to the workspace root.
Absolute machine-local paths are never part of shared identity.

Canonical path normalization follows standard operating-system normalization rules.
The model requires:

- standard operating-system normalization before canonical comparison
- one canonical separator convention in shared storage
- no absolute paths in shared storage
- file and directory identity based on canonical relative path, not display text
- case-insensitive comparison for canonical path identity

Path resolution is matched case-insensitively.
Wildcard matching follows standard operating-system-style path matching:

- `*` matches any sequence of characters within the path-matching position
- `?` matches any single character within the path-matching position

## Resolution Semantics

These rules support the reference-expression design.

### `@workspace:`

`@bobby:` resolves to the unique workspace root node for workspace label `bobby`.

### `/`

From file context, `/` resolves to the root node of the current file's workspace.

### `.`

From file context, `.` resolves to the unique directory node that owns the current file node.

### `^`

From node context, `^` resolves to the owning file node.

### Unresolved References

If a workspace, directory, or file reference does not resolve, the client should surface that state
with a visual indicator.
An unresolved reference does not mutate the graph and does not trigger any automatic repair.
Unresolved handling must be explicit and user-visible; it must not silently behave as a successful
empty result.

## Unmapped Workspace Labels

If a client has no local mapping for `@bobby:`:

- the workspace label still exists in the shared model
- graph-level references using that workspace label remain meaningful
- local filesystem operations for that workspace cannot run on that client

This keeps shared references stable while allowing partial local support.

## Persistence Shape

This document does not lock the final SQL schema, but it does require shared persistence to store
enough information to enforce the uniqueness rules above.

At minimum, shared persistence must be able to map:

- workspace label to workspace root node
- `(workspace label, relative directory path)` to directory node
- `(workspace label, relative file path)` to file node

Desktop-local persistence must be able to map:

- workspace label to absolute local root path

## Migration

Existing graphs remain valid as they are.
There is no automatic conversion of pre-existing normal nodes into workspace, directory, or file
nodes.
Those model elements appear only when explicit user commands create them and sync propagates them.
