# Workspace File Model

Status: working draft
Authority: [[revising-workspace-file-model]] is the authoritative behavioral target.  This file describes design intent to achieve the target model and persistence rules for workspace, directory, and file identity.  Implementation plans may be changed as needed.
See also: [[doc/current/workspace-graph.md]], [[doc/current/workspace-local-mapping.md]],
[[doc/current/desktop-local-files.md]], [[doc/roadmap/reference-expressions.md]], [[doc/roadmap/postgres-roadmap.md]], [[doc/arch.md]]

This document defines the model concepts needed by reference expressions such as `@bobby:`.
It is about shared identity and persistence shape, not source-level implementation.

Scope note: this is a target-scope design document.
Current implemented behavior is summarized in [[doc/current/workspace-graph.md]].

Stage implementation scope and sequencing are tracked in [[doc/current/workspace-stage-plan.md]].  For each implementation stage, refer to this file for details.

## Purpose

The existing graph model already defines node identity and owner/ref semantics.
What it does not define is how **document membership** relates to workspace, directory, and file nodes, and how that maps to paths such as `@bobby:`.

The goal is to add those concepts while keeping refs and normal outline content flexible. Slice 1 of [[doc/roadmap/workspace-scale-import-slice1-plan]] tightens owned `Directory` / `File` placement so disk paths mirror `Workspace` ownership, including ROOT, and `Directory` ownership.

## Documents

Authority for the document partition concept: [[doc/roadmap/postgres-roadmap.md]] §5.

There is always one graph on the server. **Today** the whole graph is one document (monolithic snapshot). **Target:** one graph, many documents.

A **document** is a partition of the graph defined by a **document root** — a `Special Workspace`, `Directory`, or `File` node (including implicit ROOT). **Document membership** follows Owner-tree ancestry from that root; Ref edges do not confer membership. **Ownership** means `Owner` vs `Ref` on a child slot only — not which document a node is in.

Workspace, directory, and file special nodes are document roots. Each document is persisted under `DataDir` (Stages 7–8), loaded/unloaded on the client (Stage 9), and replicated as a unit. A document rooted at a `File` node usually persists as one file; workspace and directory roots persist as directory layouts (including `.amb` where applicable).

## Persistence Strategy

Workspace definitions and desktop `@label:` mappings remain part of the model, but file persistence is tiered:

1. **Primary — server `DataDir`.** Edits sync through normal client/server graph operations; the server live-saves on the existing snapshot write path (`Snapshot.write` / `FileAgent` / db backup). Today that is one monolithic outline file (one document); target is ROOT plus separate persisted documents for each workspace/directory/file root under `DataDir/{workspaceLabel}/...`, writing only documents whose serialization would change (Stages 7–8). Detail: [[doc/roadmap/workspace-file-persistence.md]].
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
- `[x]` Correction: current invariants allow `directory`, `file`, and `normal` nodes to be placed anywhere; only `workspaces`/`workspace` stay structurally restricted.
- `[x]` Stage 3: shared persistence stores canonical workspace-label -> workspace-root mapping only.
- `[x]` Correction: document target persistence split (workspace/directory/file documents separately) and the stop-at-nested-document-root rule.
- `[x]` Stage 4: desktop-local API resolves workspace label + relative path via readonly local mapping
  (interim `/_desktop/file` API — [[doc/current/desktop-local-files.md]]).
- `[x]` Correction: align reference docs to namespace semantics (anchors, `DirStep`/`FileStep`, `^`) instead of path-only framing.
- `[~]` Stage 5: client UI shows unresolved-reference indicators; file-status uses desktop query surface for locally mapped paths (server live-save wired — Stage 7; full unresolved `@label:` UI not done). **Deferred** — bypassed for Stage 6.
- `[ ]` Correction: unresolved UI should cover namespace resolution failures across workspace, directory, and file scopes.
- `[ ]` Correction: file-status queries server persistence; desktop query remains for secondary mapped paths.
- `[x]` Stage 6: **Insert…** and **Rename** (F2) for workspace, directory, and file structure; TRASH becomes `Special Directory` with `Node.name = TRASH`; shared `DocumentPathMove` planners (rename, reparent, move-to-TRASH) — graph ops and tests only, no server I/O.
- `[ ]` Slice 1 correction: restrict owned `directory` / `file` placement to `workspace` owners, including ROOT, or `directory` owners; keep refs unrestricted and keep `normal` ownership free-form.
- `[x]` Stage 7: Step 1: server `DataDir` live-save of `.amb` document artifacts for workspace, directory, and file roots regardless of logical extension; path layout per [[doc/roadmap/workspace-file-persistence.md]].
- `[x]` Stage 7: Step 2: unified filesystem moves from `DocumentPathMove` (rename, reparent, soft delete to TRASH).
- `[x]` Stage 7: Step 3: git persistence for per-document artifacts ([[doc/roadmap/git-sync-gateway.md]]).
- `[x]` Stage 7: Step 4: hard delete under TRASH removes on-disk artifacts.
- `[x]` Stage 7: Step 5: generic text read/write for `Special File` artifacts whose path is neither `.amb` nor `.md`; workspace and directory documents stay on `.amb`. Format spec: [[doc/roadmap/workspace-format-plain.md]] (reconciliation: § Reconciliation). Generic contract: [[doc/roadmap/workspace-text-outline-conversion.md]] § Settled. Dispatch plan: [[doc/roadmap/workspace-format-dispatch.md]]. Adds a document-format dispatch boundary in the read/write layer (`DocumentAssembly`, `DocumentPersistence`).
- `[ ]` Stage 7: Step 6: XML read/write for `File` artifacts whose persisted body is XML; plain text and `.amb` behavior unchanged. Format spec: [[doc/roadmap/workspace-format-xml.md]] (reconciliation: § Reconciliation). Generic contract: [[doc/roadmap/workspace-text-outline-conversion.md]] § Settled. Extends `DocumentFormat` dispatch with an `Xml` codec. Implementation plan: [[doc/reference/formats/xml-round-trip-plan.md]].
- `[ ]` Deferred: Support markdown text file format.
- `[x]` Stage 8: snapshot integration — existing write path (`Snapshot.write` / `FileAgent` / db backup) emits ROOT plus per-document artifacts; incremental persist skips unchanged documents.

## Current Implementation Snapshot

Authority for implemented behavior: [[doc/current/workspace-graph.md]],
[[doc/current/workspace-local-mapping.md]], [[doc/current/desktop-local-files.md]], [[doc/current/workspace-stage-plan.md]].

- `[x]` `SpecialKind` includes `Workspace`, `Directory`, and `File` in the shared model.
- `[x]` Correction: treat these as context-defining special nodes for traversal and resolution
  (`RefExpr.refContext`, `RefExpr.match_`).
- `[x]` `workspacesId` canonical node exists with `kind = Special Workspaces`.
- `[ ]` Correction: clarify this is the only required top-level structural anchor.
- `[x]` `Workspaces` is permanent under root (cannot be removed or edited, like Trash).
- `[x]` Correction: document current behavior that restrictions apply to `workspaces`/`workspace`; below that, layout is free-form.
- `[x]` Graph invariants enforce structural placement rules — [[doc/current/workspace-graph.md]].
- `[ ]` Slice 1 correction: update placement rules so owned `directory` and `file` nodes may only be placed under `workspace` owners, including ROOT, or `directory` owners; refs remain free-form.
- `[x]` Desktop-local workspace label → local root mapping and interim HTTP surface.
- `[x]` Correction: document persistence tiers — server `DataDir` primary; desktop mapping secondary (download/export) plus Import entry; mapping independent of server path shape.
- `[x]` RefExpr anchors, path steps, tag steps, and namespace search —  [[doc/current/workspace-graph.md]], [[doc/roadmap/reference-expression-interpretation.md]].
- `[x]` Correction: align RefExpr semantics with directory-first member lookup (`DirStep`/`FileStep`) and `^` structural-container lookup.
- `[ ]` Surrounding language functions (`text Ref`, `children Ref`, `name Ref`) and command/assignment syntax.
- `[ ]` Whole graph still one document; no `docId` / document membership in model yet (Stage 9).
- `[x]` Per-document `DataDir` live-save and snapshot persist are implemented (Stages 7–8).
- `[x]` Incremental persist skips unchanged documents on snapshot pass (Stage 8).
- `[ ]` Full unresolved-reference indicator for unknown workspace labels.
- `[ ]` File-status uses desktop query only; server-side status not wired (Stage 5 correction / Stage 7).
- `[x]` **Insert…** / **Rename** command surface is implemented for workspace, directory, and file structure (Stage 6).
- `[x]` TRASH is `Special Directory` with `Node.name = TRASH` (Stage 6).
- `[x]` Correction: current command coverage supports special-node hierarchy edits in free-form outlines.

## Settled Decisions

1. Workspace labels such as `@bobby:` are shared, user-visible, stable identifiers.
2. A client may define a local filesystem mapping for a workspace label, or ignore that label. Local mapping is for secondary download/export and Import; server `DataDir` is primary for file content persistence.
3. The ownership tree remains the source of containment identity.
4. Context traversal uses ancestry of special nodes (`workspace`, `directory`, `file`) only.
5. `Directory` nodes are first-class nodes.
6. `File` nodes are first-class nodes.
7. Target owned `directory` and `file` placement mirrors disk: only `workspace` nodes, including ROOT, and `directory` nodes may own them.
8. `workspace`, `directory`, `file`, and `normal` nodes may own `normal` nodes.
9. `file` nodes may own normal parsed/content children for document membership, but not owned `directory` or `file` nodes.
10. Refs to `directory` and `file` nodes may be placed freely, including under `normal` or `file` nodes.
11. Workspace, directory, and file nodes change only through explicit user commands, explicit sync-tree commands, and normal
    cross-client graph synchronization.
12. There is no automatic background alignment between the graph and any client's local filesystem.
13. Workspace, directory, and file nodes are represented as `Special Workspace`,
    `Special Directory`, and `Special File`.
14. Unresolved references are surfaced to the user with a visual indicator.
15. Existing graphs do not require automatic migration to introduce this model.
16. `Workspaces` is a permanent canonical node under root, identified by a fixed `NodeId`, with the
    same permanence semantics as `Trash`.
17. `Workspace` nodes exist as direct children of `Workspaces`.
18. Server `DataDir` is the primary file-content persistence layer; graph/DB identity remains authoritative. Desktop-local absolute root mapping is secondary and independent of server path layout.
19. One graph holds many **documents** (partitions by document root). Document membership follows Owner ancestry from a workspace, directory, or file root; it is not the same as Owner vs Ref.
20. **Rename** is F2 on the focused node; **Edit node** is Enter only.
21. Soft delete reparents a node's owner occurrence under canonical TRASH (`MoveToTrash`). TRASH is a permanent `Special Directory` with `Node.name = TRASH` (Stage 6); graph delete semantics are unchanged — only the kind and path resolution change. Stage 7 persists soft delete as a filesystem reparent into `TRASH/...` via the same `DocumentPathMove` handler as rename and reparent.

## Structural Invariants

See [[doc/current/workspace-graph.md]] for enforced placement rules.

Current placement restrictions apply only to `Workspaces` and named `Workspace` nodes. Slice 1 target placement restricts owned `Directory` and `File` nodes to `Workspace` owners, including ROOT, or `Directory` owners; `Normal` ownership and refs remain free-form.

**Context** (special-node ancestry used for reference resolution) is separate from placement.
Context traversal uses only `workspace`, `directory`, and `file` nodes along the owner chain;
`normal` nodes are ignored for context. See [[doc/roadmap/revising-workspace-file-model]].

The full **Insert…** / **Rename** command surface exists (Stage 6). Slice 1 updates placement validation for owned `Directory` / `File` nodes — [[doc/current/workspace-graph.md]].

**Stage 6 implemented — Insert…:** under `Workspaces` focus, create `Special Workspace`; elsewhere current code can create `Special Directory` or `Special File` as owner child of focus. Slice 1 target restricts those owned creates to `Workspace` owners, including ROOT, or `Directory` owners; elsewhere, use refs.

**Stage 6 implemented — Rename (F2):** `Op.SetName` on workspace, directory, file; `Node.name` only on normal nodes. Reject ROOT, Workspaces, and canonical TRASH.

**Delete:** soft delete moves owner under TRASH (`MoveToTrash`); hard delete under TRASH removes subtree (Stage 7 artifact removal).

The target placement model mirrors disk for owned file/directory specials while preserving free-form notes and refs:

- `workspace` nodes, including ROOT, and `directory` nodes may own `directory`, `file`, and `normal` nodes
- `file` nodes may own `normal` parsed/content children
- `normal` nodes may own `normal` children
- `file` and `normal` nodes may not own `directory` or `file` nodes
- refs to `directory` and `file` nodes may be placed freely
- disk placement for owned `directory` and `file` nodes is the `workspace` owner chain, including ROOT, plus directory ownership

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
A file may own normal parsed/content children for document membership. It does not own `directory` or `file` nodes in the Slice 1 target; use refs to place those specials under a file occurrence.
File nodes use `NodeKind = Special File`.

### TRASH (canonical delete container)

**Today:** canonical `trashId` is `Special Directory` — not a document root; no on-disk folder.

**Target (Stage 6):** `trashId` is **`Special Directory`** with `Node.name = TRASH` (display `text` may remain `Trash`). Retire `SpecialKind.Trash`.

| Concern | Treatment |
| --- | --- |
| Permanence | Fixed `trashId`, permanent owner child of ROOT — not renamable or removable |
| Soft delete | `MoveToTrash` appends owner under `trashId` (unchanged graph semantics) |
| Snapshot / `.amb` | Stable sid `#TRASH`; owner line includes name token `TRASH` |
| Path resolution | `//TRASH/` (`NodeDesktopPath`) |
| UI styling | Trash row class/symbol by `trashId`, not by retired `Special Directory` kind |

**On disk (Stage 7):** TRASH is a persisted directory document — `TRASH/` folder with `TRASH.amb` under the ROOT workspace path in `DataDir` — [[doc/roadmap/workspace-file-persistence.md]].

### Ordinary Outline Node

An ordinary outline node is any non-workspace, non-directory, non-file node in the existing
graph.

Ordinary outline nodes belong to exactly one owner-chain location in the graph.
A normal node may own other `normal` nodes. It does not own `directory` or `file` nodes in the Slice 1 target; use refs to place those specials under a normal occurrence.
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

Soft delete is reparent under TRASH, not a separate persist primitive — Stage 7 maps it to a filesystem reparent into `TRASH/...` alongside rename and reparent moves.

Slice 1 special case: deleting an owned `Directory` or `File` node does not promote another ref to owner. It removes the owned node and all refs to it, preserving the invariant that every node has exactly one owner and avoiding silent disk-path moves. Future improvement should preserve dangling user intent more gracefully, for example with explicit link/placeholder nodes or a retarget/restore-owner flow.

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
semantics — anchors (`//`, `/`, `.`, `^`, `#`), `DirStep` (`name/`), `FileStep` (`name`) — see [[doc/roadmap/reference-expressions.md]] and [[doc/roadmap/reference-expression-interpretation.md]]. Named workspace lookup uses ROOT-relative paths such as `//workspacename/...` (reference syntax) or `//workspacename` for the workspace root.
Absolute machine-local paths are never part of shared identity.

Path text does not replace node ownership identity. Where persistence or desktop mapping uses path
text, canonical normalization rules apply consistently.

## Resolution Semantics

These rules support the reference-expression design. Authority:
[[doc/roadmap/reference-expression-interpretation.md]] (interpretation),
[[doc/roadmap/reference-expressions.md]] (surface syntax).

Implemented in `RefExprParse`, `RefExprMatch` (facade: `RefExpr`). See [[doc/current/workspace-graph.md]].

### Not implemented yet

Surrounding language functions (`text Ref`, `children Ref`, `name Ref`), command/assignment syntax, and view-root behavior.

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

`Snapshot.write` may emit `//workspacename` path text for workspace nodes (write-only hint). Directory and
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

A File node's tree persists by writing the members of to a document according to the file format. Path classification selects the codec — `.amb` for workspace and directory documents and for `.amb` file paths; generic text ([[doc/roadmap/workspace-format-plain.md]]) for non-XML `File` paths that are neither `.amb` nor `.md`; XML ([[doc/roadmap/workspace-format-xml.md]]) for XML-shaped `File` artifacts (planned Stage 7 Step 6); `.md` remains deferred ([[doc/roadmap/workspace-format-md.md]]).

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
