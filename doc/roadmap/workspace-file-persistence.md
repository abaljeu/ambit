# Workspace File Persistence

Status: Draft
Authority: Target design for server-side workspace file storage.
See also: [[doc/roadmap/workspace-file-model.md]], [[doc/roadmap/workspace-text-outline-conversion.md]], [[doc/roadmap/workspace-stage-plan.md]], [[doc/current/workspace-local-mapping.md]], [[doc/current/desktop-local-files.md]], [[doc/arch.md]]

This document details the server persistence system for workspace file data. It is intentionally separate from desktop-local workspace mapping and from the shared graph model that assigns identity to workspace, directory, and file nodes.

## Scope

This spec covers the on-disk persistence of workspace directory and file artifacts under the server `DataDir`.

It does not define the shared graph model itself. The model and its identity rules live in [[doc/roadmap/workspace-file-model.md]].

## Goal

Persist workspace file content on the server in a path structure that is stable, predictable, and derived from shared workspace identity.

The server storage path is additive to graph identity. It does not replace the database projection or the desktop-local `@label:` mapping.

## Path Layout

The on-disk layout is:

`{DataDir}/@{workspaceLabel}/{canonicalRelativePath}`

Where:

- `DataDir` is the server storage root.
- `workspaceLabel` is the shared workspace label without the trailing `:`.
- `canonicalRelativePath` is the workspace-relative path in canonical form.

Examples:

- workspace `home`, file `src/lib.fs` -> `data/@home/src/lib.fs`
- workspace `home`, directory `docs/specs` -> `data/@home/docs/specs`

The `@` prefix is part of the directory name on disk.

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

## Persistence Modes

### `db` mode

PostgreSQL remains the authority for graph structure. Workspace file content under `DataDir` is a server artifact written from the accepted server state.

### `file` mode

The same path layout applies under `DataDir`. Read authority for this mode remains a follow-on implementation detail.

## Desktop Behavior

Desktop-local behavior stays unchanged:

- `@label:relative` continues to resolve against local mapped workspace roots
- manual Import and Export via `/_desktop/file` continue to work
- local workspace config remains separate from server `DataDir` storage

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
- whether writes happen per accepted change or on a batched schedule

## Verification Targets

- workspace files are written under `DataDir/@label/...`
- overwrites rotate prior files to `.bak.{date}`
- path validation prevents escape above `DataDir/@label/`
- desktop local mapping behavior remains unchanged
