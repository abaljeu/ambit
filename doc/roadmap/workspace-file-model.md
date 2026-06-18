# Workspace File Model

Status: working draft
Authority: [[revising-workspace-file-model]] is the authoritative behavioral target.  This file describes design intent to achieve the target model and persistence rules for workspace, directory, and file identity.  Implementation plans may be changed as needed.
See also: [[doc/current/workspace-graph.md]], [[doc/current/workspace-local-mapping.md]],
[[doc/current/desktop-local-files.md]], [[doc/roadmap/reference-expressions.md]], [[doc/arch.md]]

This document defines the model concepts needed by reference expressions such as `@bobby:`.
It is about shared identity and persistence shape, not source-level implementation.

Scope note: this is a target-scope design document.
Current implemented behavior is summarized in [[doc/current/workspace-graph.md]].
Stage implementation scope is defined separately in [[doc/roadmap/workspace-stage-plan.md]], which is intentionally narrower.

## Purpose

The existing graph model already defines node identity and owner/ref semantics.
What it does not define is how a graph node belongs to a file, how a file belongs to a directory tree, and how that directory tree relates to a user-visible workspace label such as `@bobby:`.

The goal is to add those concepts without changing the current graph ownership rules.

## Status Tracking

This document describes the target model, but implementation is expected to land in stages.  To keep the document useful during that process:

- `[x]` means implemented in the current codebase
- `[~]` means partially implemented or represented in the model, but not yet wired through
- `[ ]` means target design only

When a section below describes the target end state, that does not by itself mean it is already implemented. The stage list below is the current implementation summary.
When a Correction is described below, the meaning is that the item previous is described wrongly, but implemented.  To implement the correction, the incorrect implementation must be corrected.

## Implementation Stages

- `[x]` Stage 1: model vocabulary exists for workspace, directory, and file node kinds in the
   shared model.
- `[~]` Stage 2: graph invariants and operations understand workspace, directory, and file nodes as
   distinct behavior-bearing concepts.
- `[x]` Correction: update invariants so `directory`, `file`, and `normal` nodes may be placed anywhere; only `workspaces`/`workspace` stay structurally restricted.
- `[x]` Stage 3: shared persistence stores canonical workspace-label -> workspace-root mapping only.
- `[x]` Correction: document target persistence split (workspace/directory/file separately) and the file-traversal stop-at-child-special-node rule.
- `[x]` Stage 4: desktop-local API resolves workspace label + relative path via readonly local mapping
  (interim `/_desktop/file` API — [[doc/current/desktop-local-files.md]]).
- `[x]` Correction: align reference docs to namespace semantics (anchors, `DirStep`/`FileStep`, `^`) instead of path-only framing.
- `[~]` Stage 5: client UI uses desktop query surface and shows unresolved-reference indicators
  (file-status indicator done; full unresolved `@label:` UI not done).
- `[ ]` Correction: unresolved UI should cover namespace resolution failures across workspace, directory, and file scopes.
- `[~]` Stage 6: explicit user commands create and modify workspace/file/directory structure
  (workspace create/rename via graph ops; directory/file commands not done).
- `[ ]` Correction: add command support for free-form special-node ownership (including under `normal` and `file` nodes) while keeping persistence ownership rules explicit.
- `[ ]` Stage 7: server `DataDir/@label/...` persistence for directory and file objects.

## Current Implementation Snapshot

Authority for implemented behavior: [[doc/current/workspace-graph.md]],
[[doc/current/workspace-local-mapping.md]], [[doc/current/desktop-local-files.md]].

- `[x]` `SpecialKind` includes `Workspace`, `Directory`, and `File` in the shared model.
- `[x]` Correction: treat these as context-defining special nodes for traversal and resolution
  (`RefExpr.refContext`, `RefExpr.match_`).
- `[x]` `workspacesId` canonical node exists with `kind = Special Workspaces`.
- `[ ]` Correction: clarify this is the only required top-level structural anchor.
- `[x]` `Workspaces` is permanent under root (cannot be removed or edited, like Trash).
- `[x]` Correction: document that restrictions apply to `workspaces`/`workspace`; below that, layout is free-form.
- `[x]` Graph invariants enforce structural placement rules — [[doc/current/workspace-graph.md]].
- `[x]` Correction: update placement rules so `directory` and `file` nodes may be placed anywhere.
- `[x]` Desktop-local workspace label → local root mapping and interim HTTP surface.
- `[ ]` Correction: clarify desktop mapping remains local and independent from server `DataDir` persistence shape.
- `[x]` RefExpr anchors, path steps, tag steps, and namespace search —
  [[doc/current/workspace-graph.md]], [[doc/roadmap/reference-expression-interpretation.md]].
- `[x]` Correction: align RefExpr semantics with directory-first member lookup (`DirStep`/`FileStep`)
  and `^` structural-container lookup.
- `[ ]` RefExpr postfixes (`.text`, `[n]`, filters) and command/assignment syntax.
- `[ ]` No directory/file path mapping is persisted in shared graph yet.
- `[ ]` No server `DataDir/@label/...` persistence for directory/file objects yet.
- `[ ]` Full unresolved-reference indicator for unknown workspace labels.
- `[~]` Workspace create/rename via graph ops; no directory/file command surface yet.
- `[ ]` Correction: add command coverage for special-node hierarchy edits in free-form outlines.

## Settled Decisions

1. Workspace labels such as `@bobby:` are shared, user-visible, stable identifiers.
2. A client may define a local filesystem mapping for a workspace label, or ignore that label.
3. The ownership tree remains the source of containment identity.
4. Context traversal uses ancestry of special nodes (`workspace`, `directory`, `file`) only.
5. `Directory` nodes are first-class nodes.
6. `File` nodes are first-class nodes.
7. Below `workspaces`/`workspace` structural rules, `directory`, `file`, and `normal` nodes may be owned by any parent.
8. `normal` nodes may own `directory`, `file`, and `normal` nodes.
9. `file` nodes may own `directory`, `file`, and `normal` nodes for outline structure.
10. Workspace, directory, and file nodes change only through explicit user commands and normal
    cross-client graph synchronization.
11. There is no automatic background alignment between the graph and any client's local filesystem.
12. Workspace, directory, and file nodes are represented as `Special Workspace`,
    `Special Directory`, and `Special File`.
13. Unresolved references are surfaced to the user with a visual indicator.
14. Existing graphs do not require automatic migration to introduce this model.
15. `Workspaces` is a permanent canonical node under root, identified by a fixed `NodeId`, with the
    same permanence semantics as `Trash`.
16. `Workspace` nodes exist as direct children of `Workspaces`.
17. Server filesystem persistence remains additive to DB identity and independent from desktop-local
    absolute root mapping.

## Structural Invariants

See [[doc/current/workspace-graph.md]] for enforced placement rules.

Placement restrictions apply only to `Workspaces` and named `Workspace` nodes.
`Directory`, `File`, and `Normal` nodes may be placed anywhere in the ownership tree.

**Context** (special-node ancestry used for reference resolution) is separate from placement.
Context traversal uses only `workspace`, `directory`, and `file` nodes along the owner chain;
`normal` nodes are ignored for context. See [[doc/roadmap/revising-workspace-file-model]].

No command surface for creating `Directory` or `File` nodes exists yet (Stage 6). Workspace
create/rename uses general graph ops — [[doc/current/workspace-graph.md]].

The target placement model is free-form below `workspaces`/`workspace` structural rules:

- `directory`, `file`, and `normal` nodes may be owned by any parent, including `normal` nodes
- `normal` nodes may own `directory`, `file`, and `normal` nodes
- `file` may own `directory`, `file`, and `normal` nodes for structural outlining
- disk placement for special nodes is still determined by nearest owning `directory` ancestor

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

A directory node is a special structural node in the ownership tree.
Directory nodes use `NodeKind = Special Directory`.

### File Node

A file node is a special structural node in the ownership tree.
A file may own `directory`, `file`, and `normal` nodes for outline structure.
File nodes use `NodeKind = Special File`.

### Ordinary Outline Node

An ordinary outline node is any non-workspace, non-directory, non-file node in the existing
graph.

Ordinary outline nodes belong to exactly one owner-chain location in the graph.
A normal node may own `directory`, `file`, and other `normal` nodes.
Their nearest special-node ancestry defines context for resolution.
Ref edges may point at the node from other places, but they do not change ownership.

## Shared Versus Local Data

### Shared Data

The shared model and shared database are responsible for:

- workspace labels
- workspace root nodes
- directory nodes
- file nodes
- ownership relations among special and normal nodes

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
3. Each node has exactly one owner in the ownership tree.
4. Special-node ancestry (`workspace`, `directory`, `file`) defines context.
5. Ref edges never change ownership or containment membership.
6. The model is not changed automatically by filesystem observation alone.

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

Persistence may use workspace-relative path text on disk. Reference expressions use namespace
semantics — anchors (`/`, `.`, `^`, `#`, `@label:`), `DirStep` (`name/`), `FileStep` (`name`) — see
[[doc/roadmap/reference-expressions.md]] and [[doc/roadmap/reference-expression-interpretation.md]].
Absolute machine-local paths are never part of shared identity.

Path text does not replace node ownership identity. Where persistence or desktop mapping uses path
text, canonical normalization rules apply consistently.

## Resolution Semantics

These rules support the reference-expression design. Authority:
[[doc/roadmap/reference-expression-interpretation.md]] (interpretation),
[[doc/roadmap/reference-expressions.md]] (surface syntax).

Implemented in `RefExprParse`, `RefExprMatch` (facade: `RefExpr`). See [[doc/current/workspace-graph.md]].

### `@workspace:`

`@bobby:` resolves to the unique workspace root node for workspace label `bobby`.

### Anchors (from context node)

Interpretation walks the ownership chain (including self) unless noted:

- **Context** (no prefix) — the context node itself.
- **`/`** — nearest `workspace` ancestor (falls back to **ROOT** when none).
- **`//`** — always **ROOT**.
- **`.`** — nearest `directory` or `workspace` ancestor (current directory).
- **`^`** — nearest `file`, `directory`, or `workspace` ancestor (structural container).
- **`#`** — nearest named `normal` ancestor (current tagged node). `#name` is a tag search step, not
  this anchor.

### Path steps

From each matched base node:

- **`name/`** (`DirStep`) — a `directory` whose name matches `name` (glob semantics).
- **`name`** (`FileStep`) — a `file` whose name matches `name`.
- **`**`** — multi-level wildcard within path scope.

Search uses recursive descent through owned children; recursion does not enter children of
`directory` or `workspace` nodes. Each step uses the flat list of all matches from the prior step.

### Tagged steps

- **`#name`** — a named `normal` node whose `Node.name` matches `name`.
- Search is within **content** only (owned `normal` nodes under the relevant structural container).

### Not implemented yet

Postfixes (`.text`, `.name`, `.children`, `[n]`, filters), command/assignment syntax, view-root
anchor.

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

Authority for server file persistence rules: [[doc/roadmap/revising-workspace-file-model]].
On-disk layout detail: [[doc/roadmap/workspace-file-persistence.md]].

Persistence is split by layer and by special-node kind. Workspace, directory, and file are
separate concerns — not one monolithic path map.

### Shared graph persistence

The graph projection (`GraphProjection`, change-log ops) stores ownership-tree identity:

- **Workspace** — label → workspace root node (`Special Workspace` under `Workspaces`,
  `Node.name` = label). Implemented (Stage 3). Lookup: `RefExpr.refContext`, `RefExpr.match_`,
  `FilePathResolve.findOwnerChild`.
- **Directory** — node identity (`kind`, `name`, owner link). No server `DataDir` path materialization
  yet.
- **File** — node identity (`kind`, `name`, owner link). No server `DataDir` path materialization
  yet.

`Snapshot.write` may emit `@label:` path text for workspace nodes (write-only hint). Directory and
file path bodies in snapshot text are not shared persistence authority; round-trip and server
`DataDir` layout are Stage 7.

Desktop-local persistence maps workspace label to absolute local root path — fully separate from
shared graph and server storage ([[doc/current/workspace-local-mapping.md]]).

### Server file persistence (target — Stage 7)

Not fully implemented. Three kinds persist separately on disk under `DataDir`:

#### Workspace

A workspace persists like a special directory. Label `wsname` maps to directory `@wsname` under the
server data root.

#### Directory

Every directory persists under its owning directory on disk. Root workspace content persists
directly in the workspace directory. `normal` nodes directly owned by a directory persist in
`.amb` in that directory.

#### File

A file persists by writing the subtree it owns according to the file format.

**Stop at child special node.** When traversing a file's owned tree for persistence, descent
stops at each child `workspace`, `directory`, or `file` node:

- do not recurse into that child's subtree as part of the parent file payload
- continue traversing siblings and other branches of the current file tree
- persist that child special node (and its descendants) as its own file or directory artifact
- the parent file persists a reference to the child special node, not its nested content

"Stops descending" means skip recursion into that child tree only — not halt the whole file
traversal.

## Migration

Existing graphs remain valid as they are.
There is no automatic conversion of pre-existing normal nodes into workspace, directory, or file
nodes.
Those model elements appear only when explicit user commands create them and sync propagates them.
