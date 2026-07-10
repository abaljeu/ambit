# Workspace File Persistence

Status: Draft
Authority: Target design for server-side workspace file storage.
See also: [[doc/roadmap/workspace-file-model.md]], [[doc/roadmap/git-sync-gateway.md]], [[doc/roadmap/workspace-text-outline-conversion.md]], [[doc/current/workspace-stage-plan.md]], [[doc/roadmap/postgres-roadmap.md]], [[doc/current/workspace-local-mapping.md]], [[doc/current/desktop-local-files.md]], [[doc/arch.md]]

This document details the server persistence system for workspace file data. It is intentionally separate from desktop-local workspace mapping and from the shared graph model that assigns identity to workspace, directory, and file nodes.

## Documents

Authority: [[doc/roadmap/postgres-roadmap.md]] §5, [[doc/roadmap/workspace-file-model.md]] § Documents.

One graph holds many **documents**. A document root is a `Special Workspace`, `Directory`, or `File` node. **Document membership** (Owner ancestry from that root) defines what each persisted artifact serializes. This spec covers how those documents map to paths under `DataDir`.

## Scope

This spec covers the on-disk persistence of workspace directory and file artifacts under the server `DataDir`.

It does not define the shared graph model itself. The model and its identity rules live in [[doc/roadmap/workspace-file-model.md]].

## Goal

Persist workspace file content on the server in a path structure that is stable, predictable, and derived from shared workspace identity. This is the **primary** file persistence mechanism: edits that sync through normal client/server operations are live-saved here.

Graph/DB identity remains authoritative. Desktop label mapping is secondary (download/export to local paths) and the Import entry point — it does not replace server storage.

## Path Layout

The on-disk layout is:

`{DataDir}/{workspaceLabel}/{canonicalRelativePath}`

Where:

- `DataDir` is the server storage root.
- `workspaceLabel` is the shared workspace label (verbatim folder name).
- `canonicalRelativePath` is the workspace-relative path in canonical form.

Examples:

- workspace `home`, file `src/lib.fs` -> `data/home/src/lib.fs`
- workspace `home`, directory `docs/specs` -> `data/home/docs/specs/.amb`
- nameless ROOT workspace, Special Directory document (directory persistence) -> `data/TRASH/.amb`
  (Stage 7)

## TRASH on disk

Canonical `trashId` remains **`Special Directory`** — not a distinct `Special Directory` kind. Stage 7
materializes it under the nameless ROOT workspace using **directory persistence semantics** (same
folder and `.amb` artifact layout as `Special Directory` document roots):

- **Folder:** `{DataDir}/TRASH/`
- **Artifact:** `.amb` (directory persisted data; same filename as other `Special Directory` roots)
- **Graph:** same fixed `trashId`, `Special Directory` kind, permanent owner child of ROOT; soft delete
  still reparents owner under `trashId`

Path resolution for TRASH is `//TRASH/` (`NodeDesktopPath`).

## Canonical Paths

Shared persistence uses canonical relative paths, not absolute machine-local paths.

Rules:

- normalize before comparison
- use one separator convention in storage
- reject absolute paths
- reject upward traversal above the workspace root
- compare canonical path identity case-insensitively

Wildcard handling follows the path-matching rules defined in the file model.

## Write Pattern

Server writes follow the same basic snapshot-backup pattern used elsewhere in the server:

- create parent directories as needed
- write the new content
- rotate any previous file to a `.bak.{date}` backup on overwrite

The implementation should favor the smallest possible storage mechanism that preserves these semantics.

## Snapshot integration

Primary persistence extends the existing snapshot write path — `Snapshot.write` in [[src/Shared/Snapshot.fs]], triggered after accepted changes by `FileAgent` (file mode) and `startDbBackupIfNeeded` (db mode). See [[doc/current/persistence-model.md]].

**Today:** one monolithic outline snapshot serializes the whole graph (one document).

**Target:** each snapshot pass still runs on that same trigger, but emits multiple persisted documents:

- **ROOT** (nameless) — serialized by the same pipeline, following workspace saving rules for members of the ROOT document that are not delegated to a nested workspace, directory, or file document.
- **Workspace, directory, file roots** — each document also written to its `DataDir/{label}/...` path when the pass runs. Serialization includes only nodes with document membership in that root (stop-at-nested-document-root applies).

This is not a separate persistence mechanism; it splits today's single `Snapshot.write` output into ROOT plus per-document files on disk.

## Incremental writes

Only write a persisted document when its serialized content would change compared to what is already on disk.

The write scope for each document is the nodes with membership in that document root (workspace, directory, or file). That member set is what gets serialized and diffed; unchanged documents are skipped. See also export delta semantics in [[doc/roadmap/workspace-text-outline-conversion.md]].

## Path moves

Stage 7 extends live-save with a **single move handler** for any graph change that alters the canonical on-disk location of a workspace, directory, or file document root.

### Triggers (all → same handler)

| Graph change | Persisted effect |
| --- | --- |
| **Rename** (`Op.SetName` on workspace/directory/file) | Final path segment changes |
| **Reparent / move** (`Op.Replace` changes owner parent) | Path prefix changes |
| **Soft delete** (`MoveToTrash`) | Reparent owner under `trashId` → move into `//TRASH/...` — no separate delete persist path |

Hard delete under TRASH (subtree removal) is a separate Stage 7 slice (remove artifacts). Soft delete is covered by move-to-TRASH.

Cross-workspace reparent needs no special case — `oldPath` and `newPath` differ by workspace prefix and the same handler applies.

For nodes whose **subtree** contains persisted document roots, the handler walks the moved subtree and may move **multiple** artifacts (directory tree move).

### Shared descriptor (Stage 6 planner, Stage 7 consumer)

```fsharp
type DocumentPathMove = {
    nodeId: NodeId
    oldPath: string
    newPath: string
}

planPathMoveForSetName : graph -> nodeId -> newName -> DocumentPathMove option
planPathMoveForReparent : graph -> nodeId -> newParentId -> DocumentPathMove option
// MoveToTrash: planPathMoveForReparent graph nodeId trashId
```

Stage 6: shared planners compute `DocumentPathMove` values; tests prove path computation (no I/O). Stage 7: server executes filesystem moves under `DataDir/{label}/...`; git persistence records per-document artifact history.

| Layer | Stage 6 | Stage 7 |
| --- | --- | --- |
| Graph | Insert…, Rename; TRASH stays `Special Directory` (directory persistence semantics) | — |
| Shared | Emit `DocumentPathMove` from planners; tests | — |
| Server | No-op / discard effect | Execute moves; materialize `TRASH/.amb` |

## Persistence Modes

### `db` mode

PostgreSQL remains the authority for graph structure. Workspace file content under `DataDir` is written from accepted server state on each relevant change (live save).

### `file` mode
DEPRECATED.

The same path layout applies under `DataDir`. Read authority for this mode remains a follow-on implementation detail.

## Desktop Behavior

Desktop-local mapping is **secondary** persistence and the Import entry point:

- **Import (unchanged):** read local file via `/_desktop/file`; client applies edits; sync to server; server live-save persists under `DataDir`.
- **Download/export:** write server file content to `//label/relative` paths under locally mapped workspace roots via `/_desktop/file`.
- `//label/relative` continues to resolve against local mapped workspace roots for those operations.
- local workspace config remains separate from server `DataDir` storage

There is no automatic background sync between server `DataDir` and desktop-mapped files.

The text-to-outline conversion rules are documented separately in
[[doc/roadmap/workspace-text-outline-conversion.md]].

## Non-Goals

- automatic filesystem to graph reconciliation
- changing the desktop config mapping format
- replacing manual Import/Export with server-pushed sync
- automatic migration of existing graphs

## Open Points

- directory representation on disk: empty directory versus marker file
- whether file payload is outline text, raw bytes, or both

## Verification Targets

- workspace files are written under `DataDir/{label}/...`
- rename, reparent, and move-to-TRASH apply filesystem moves from `DocumentPathMove` descriptors
- subtree moves cover nested workspace/directory/file document roots where needed
- overwrites rotate prior files to `.bak.{date}`
- path validation prevents escape above `DataDir/{label}/`
- Special Directory document materialized with directory layout (`TRASH/.amb`)
- desktop local mapping behavior remains unchanged
