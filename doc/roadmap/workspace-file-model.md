# Workspace File Model

Status: working draft
Authority: [[revising-workspace-file-model]] is the authoritative behavioral target.  This file describes design intent to achieve the target model and persistence rules for workspace, directory, and file identity.  Implementation plans may be changed as needed.
See also: [[doc/current/workspace-graph.md]], [[doc/current/workspace-local-mapping.md]],
[[doc/current/desktop-local-files.md]], [[doc/roadmap/reference-expressions.md]], [[doc/roadmap/postgres-roadmap.md]], [[doc/arch.md]]

This document defines the model concepts needed by reference expressions such as `@bobby:`.
It is about shared identity and persistence shape, not source-level implementation.

Scope note: this is a target-scope design document.
Current implemented behavior is summarized in [[doc/current/workspace-graph.md]].

Stage implementation scope and sequencing are tracked in [[doc/roadmap/workspace-stage-plan.md]].  For each implementation stage, refer to this file for details.

## Purpose

The existing graph model already defines node identity and owner/ref semantics.
What it does not define is how **document membership** relates to workspace, directory, and file nodes, and how that maps to paths such as `@bobby:`.

The goal is to add those concepts without changing the current graph ownership rules.

## Documents

Authority for the document partition concept: [[doc/roadmap/postgres-roadmap.md]] §5.

There is always one graph on the server. **Today** the whole graph is one document (monolithic snapshot). **Target:** one graph, many documents.

A **document** is a partition of the graph defined by a **document root** — a `Special Workspace`, `Directory`, or `File` node (including implicit ROOT). **Document membership** follows Owner-tree ancestry from that root; Ref edges do not confer membership. **Ownership** means `Owner` vs `Ref` on a child slot only — not which document a node is in.

Workspace, directory, and file special nodes are document roots. Each document is persisted under `DataDir` (Stages 7–8), loaded/unloaded on the client (Stage 9), and replicated as a unit. A document rooted at a `File` node usually persists as one file; workspace and directory roots persist as directory layouts (including `.amb` where applicable).

## Persistence Strategy

Workspace definitions and desktop `@label:` mappings remain part of the model, but file persistence is tiered:

1. **Primary — server `DataDir`.** Edits sync through normal client/server graph operations; the server live-saves on the existing snapshot write path (`Snapshot.write` / `FileAgent` / db backup). Today that is one monolithic outline file (one document); target is ROOT plus separate persisted documents for each workspace/directory/file root under `DataDir/@label/...`, writing only documents whose serialization would change (Stages 7–8). Detail: [[doc/roadmap/workspace-file-persistence.md]].
2. **Secondary — desktop workspace-mapped files.** A client may download or export server file content to a locally mapped workspace root. This does not replace server authority.
3. **Import — unchanged.** User reads a desktop-local file (via `/_desktop/file` Import); client edits apply to the graph and sync to the server; subsequent persistence follows the primary server path.

Desktop-local absolute root mapping is independent of server path layout. It is not the primary persistence mechanism.

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
- `[x]` Correction: document target persistence split (workspace/directory/file documents separately) and the stop-at-nested-document-root rule.
- `[x]` Stage 4: desktop-local API resolves workspace label + relative path via readonly local mapping
  (interim `/_desktop/file` API — [[doc/current/desktop-local-files.md]]).
- `[x]` Correction: align reference docs to namespace semantics (anchors, `DirStep`/`FileStep`, `^`) instead of path-only framing.
- `[~]` Stage 5: client UI shows unresolved-reference indicators; file-status uses desktop query surface for locally mapped paths (primary server live-save not wired; full unresolved `@label:` UI not done). **Deferred** — bypassed for Stage 6; server file-status waits on Stage 7.
- `[ ]` Correction: unresolved UI should cover namespace resolution failures across workspace, directory, and file scopes.
- `[ ]` Correction: file-status queries server persistence when Stage 7 is wired; desktop query remains for secondary mapped paths until then.
- `[ ]` Stage 6: **Insert…** and **Rename** (F2) for workspace, directory, and file structure; TRASH becomes `Special Directory` with `Node.name = TRASH`; shared `DocumentPathMove` planners (rename, reparent, move-to-TRASH) — graph ops and tests only, no server I/O.
- `[ ]` Correction: add command support for free-form special-node ownership (including under `normal` and `file` nodes) while keeping persistence ownership rules explicit.
- `[ ]` Stage 7: Step 1: ALL files regardless of extension will persist in .amb format.  server `DataDir` live-save of documents (workspace/directory/file roots); unified filesystem moves from `DocumentPathMove` (rename, reparent, soft delete to TRASH); path layout and backup rotation ([[doc/roadmap/workspace-file-persistence.md]]).
- `[ ]` Deferred: Support generic text file format.
- `[ ]` Deferred: Support markdown text file format.
- 
- `[ ]` Stage 8: snapshot integration — existing write path (`Snapshot.write` / `FileAgent` / db backup) emits ROOT plus per-document artifacts; incremental persist skips unchanged documents.
- `[ ]` Stage 9: document membership in model — `docId` (or equivalent), derivation from document roots, client document load/unload and replication unit ([[doc/roadmap/postgres-roadmap.md]] §5–6).

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
- `[x]` Correction: document persistence tiers — server `DataDir` primary; desktop mapping secondary (download/export) plus Import entry; mapping independent of server path shape.
- `[x]` RefExpr anchors, path steps, tag steps, and namespace search —  [[doc/current/workspace-graph.md]], [[doc/roadmap/reference-expression-interpretation.md]].
- `[x]` Correction: align RefExpr semantics with directory-first member lookup (`DirStep`/`FileStep`) and `^` structural-container lookup.
- `[ ]` RefExpr postfixes (`.text`, `[n]`, filters) and command/assignment syntax.
- `[ ]` Whole graph still one document; no `docId` / document membership in model yet (Stage 9).
- `[ ]` Snapshot write still monolithic; no per-document `DataDir` persist (Stages 7–8).
- `[ ]` No incremental per-document persist on snapshot pass (Stage 8).
- `[ ]` Full unresolved-reference indicator for unknown workspace labels.
- `[ ]` File-status uses desktop query only; server-side status not wired (Stage 5 correction / Stage 7).
- `[~]` Workspace create/rename via graph ops; **Insert…** / **Rename** command surface not done (Stage 6).
- `[ ]` TRASH as `Special Directory` with `Node.name = TRASH` (Stage 6 — today: `Special Trash`).
- `[ ]` Correction: add command coverage for special-node hierarchy edits in free-form outlines.

## Settled Decisions

1. Workspace labels such as `@bobby:` are shared, user-visible, stable identifiers.
2. A client may define a local filesystem mapping for a workspace label, or ignore that label. Local mapping is for secondary download/export and Import; server `DataDir` is primary for file content persistence.
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
17. Server `DataDir` is the primary file-content persistence layer; graph/DB identity remains authoritative. Desktop-local absolute root mapping is secondary and independent of server path layout.
18. One graph holds many **documents** (partitions by document root). Document membership follows Owner ancestry from a workspace, directory, or file root; it is not the same as Owner vs Ref.
19. **Rename** is F2 on the focused node; **Edit node** is Enter only.
20. Soft delete reparents a node's owner occurrence under canonical TRASH (`MoveToTrash`). TRASH is a permanent `Special Directory` with `Node.name = TRASH` (Stage 6); graph delete semantics are unchanged — only the kind and path resolution change. Stage 7 persists soft delete as a filesystem reparent into `@:/TRASH/...` via the same `DocumentPathMove` handler as rename and reparent.

## Structural Invariants

See [[doc/current/workspace-graph.md]] for enforced placement rules.

Placement restrictions apply only to `Workspaces` and named `Workspace` nodes.
`Directory`, `File`, and `Normal` nodes may be placed anywhere in the ownership tree.

**Context** (special-node ancestry used for reference resolution) is separate from placement.
Context traversal uses only `workspace`, `directory`, and `file` nodes along the owner chain;
`normal` nodes are ignored for context. See [[doc/roadmap/revising-workspace-file-model]].

No full **Insert…** / **Rename** command surface exists yet (Stage 6). Workspace create/rename uses general graph ops today — [[doc/current/workspace-graph.md]].

**Stage 6 target — Insert…:** under `Workspaces` focus, create `Special Workspace`; elsewhere create `Special Directory` or `Special File` as owner child of focus. Pick-existing insert (search result) unchanged.

**Stage 6 target — Rename (F2):** `Op.SetName` on workspace, directory, file; `Node.name` only on normal nodes. Reject ROOT, Workspaces, and canonical TRASH.

**Delete:** soft delete moves owner under TRASH (`MoveToTrash`); hard delete under TRASH removes subtree (Stage 7 artifact removal).

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

### TRASH (canonical delete container)

**Today:** canonical `trashId` is `Special Trash` — not a document root; no on-disk folder.

**Target (Stage 6):** `trashId` is **`Special Directory`** with `Node.name = TRASH` (display `text` may remain `Trash`). Retire `SpecialKind.Trash`.

| Concern | Treatment |
| --- | --- |
| Permanence | Fixed `trashId`, permanent owner child of ROOT — not renamable or removable |
| Soft delete | `MoveToTrash` appends owner under `trashId` (unchanged graph semantics) |
| Snapshot / `.amb` | Stable sid `#TRASH`; owner line includes name token `TRASH` |
| Path resolution | `@:/TRASH/` under nameless ROOT workspace (`NodeDesktopPath`) |
| UI styling | Trash row class/symbol by `trashId`, not by retired `Special Trash` kind |

**On disk (Stage 7):** TRASH is a persisted directory document — `TRASH/` folder with `TRASH.amb` under the ROOT workspace path in `DataDir` — [[doc/roadmap/workspace-file-persistence.md]].

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

- choosing whether to handle a workspace label locally
- mapping a handled workspace label to an absolute local filesystem root (secondary storage and Import)
- deciding that a workspace label is currently unavailable on that machine

File content authority lives on the server under `DataDir`. Local mapping does not replace it.
Two clients may map the same shared workspace label to different absolute roots.

## Identity And Invariants

The following invariants define the model:

1. Workspace labels are unique within the shared graph.
2. Each workspace label has exactly one workspace root node.
3. Each node has exactly one owner in the ownership tree.
4. Special-node ancestry (`workspace`, `directory`, `file`) defines context.
5. Ref edges never change ownership or containment. Document membership follows Owner ancestry from document roots; Ref edges do not transfer document membership.
6. The model is not changed automatically by filesystem observation alone.

## Materialization And Change Rules

Workspace, directory, and file nodes are created, renamed, moved, or removed only by:

- explicit user commands (**Insert…**, **Rename**, delete/move-to-TRASH, etc.)
- normal cross-client graph synchronization of those user commands

Soft delete is reparent under TRASH, not a separate persist primitive — Stage 7 maps it to a filesystem reparent into `@:/TRASH/...` alongside rename and reparent moves.

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
semantics — anchors (`/`, `.`, `^`, `#`, `@label:`), `DirStep` (`name/`), `FileStep` (`name`) — see [[doc/roadmap/reference-expressions.md]] and [[doc/roadmap/reference-expression-interpretation.md]].
Absolute machine-local paths are never part of shared identity.

Path text does not replace node ownership identity. Where persistence or desktop mapping uses path
text, canonical normalization rules apply consistently.

## Resolution Semantics

These rules support the reference-expression design. Authority:
[[doc/roadmap/reference-expression-interpretation.md]] (interpretation),
[[doc/roadmap/reference-expressions.md]] (surface syntax).

Implemented in `RefExprParse`, `RefExprMatch` (facade: `RefExpr`). See [[doc/current/workspace-graph.md]].

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
  and owner-name scans in search helpers.
- **Directory** — node identity (`kind`, `name`, owner link). No server `DataDir` path materialization
  yet.
- **File** — node identity (`kind`, `name`, owner link). No server `DataDir` path materialization
  yet.

`Snapshot.write` may emit `@label:` path text for workspace nodes (write-only hint). Directory and
file path bodies in snapshot text are not shared persistence authority; round-trip and server
`DataDir` layout are Stages 7–8.

Desktop-local persistence maps workspace label to absolute local root path — fully separate from shared graph and server storage ([[doc/current/workspace-local-mapping.md]]). Used for Import, download/export to mapped paths, and local file-status queries — not as primary file authority.

### Server file persistence (primary — Stages 7–8)

Not fully implemented. Extends the existing snapshot write path; live-save on accepted server state. Each pass may emit ROOT plus one persisted form per workspace, directory, and file **document** under `DataDir`. Only documents whose member-subtree serialization would change are written.

**Unified path moves (Stage 7):** graph changes that alter canonical on-disk location of document roots — rename (`Op.SetName`), reparent (`Op.Replace` owner parent), soft delete (`MoveToTrash` → reparent under `trashId`) — share one server handler driven by shared `DocumentPathMove` descriptors (planned in Stage 6). Subtrees with nested document roots may require moving multiple artifacts. Hard delete under TRASH (artifact removal) is a separate concern. Detail: [[doc/roadmap/workspace-file-persistence.md]] § Path moves.

#### Workspace document

A workspace document persists like a special directory. Label `wsname` maps to directory `@wsname` under the server data root.

#### Directory document

Every directory document persists under its owning directory on disk. Root workspace content persists directly in the workspace directory. Normal nodes with document membership in a directory-rooted document (direct members, not nested document roots) persist in `.amb` in that directory.

#### File document

A file document persists by writing the members of that document according to the file format.

**Stop at nested document root.** When serializing a document for persistence, descent stops at each child `workspace`, `directory`, or `file` node that is itself a document root:

- do not recurse into that nested document's members as part of the parent document's payload
- continue traversing siblings and other branches of the current document
- persist the nested document (and its members) as its own artifact
- the parent document persists a reference to the nested document root, not its nested content

"Stops descending" means skip recursion into that nested document only — not halt serialization of the whole parent document.

## Migration

Existing graphs remain valid as they are.
There is no automatic conversion of pre-existing normal nodes into workspace, directory, or file
nodes.
Those model elements appear only when explicit user commands create them and sync propagates them.
