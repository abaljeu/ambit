# 11 — Load reconciles walk one level

**Status:** `coded`
Actual: 1h 5m
**Type:** coding
**Blocked by:** 10 — Add discoverImmediateMembers
**Trackers:** architecture, project spec

## Context

A person runs git Load, Directory Load, or Workspace Load. Each reconcile walks one level. Immediate members are the files and the child directories in that directory. A newly discovered child Directory Node is Unparsed. Its contents stay unread until that Directory Node is Loaded. A child Directory Node the Graph already holds as Current is left alone. Cold bootstrap stays the recursive walk.

## What to build

1. [x] **git Load** — git Load reconcile calls `discoverImmediateMembers`. It does not walk the whole tree.
2. [x] **Directory Load** — Directory Load reconcile calls `discoverImmediateMembers`.
3. [x] **Workspace Load** — Workspace Load reconcile calls `discoverImmediateMembers`.
4. [x] **New child Directory Node** — A newly discovered child Directory Node is Unparsed. Its contents stay unread until Load.
5. [x] **Held Current** — A child Directory Node the Graph already holds as Current stays Current. The parent reconcile does not mark it Unparsed.
6. [x] **Cold bootstrap stays recursive** — `readAllDocuments` still uses `discoverArtifactRelatives`.

### Acceptance Criteria

1. [x] **One-level files** — After git Load, Directory Load, or Workspace Load, a file in that directory is discovered. A file inside a child directory is not discovered by that walk.
2. [x] **One-level directories** — The same walk discovers a child directory. It does not discover a directory nested under that child.
3. [x] **New Directory Node** — A child directory the Graph lacks gets a Directory Node. That Directory Node is Unparsed. Its contents stay unread until Load.
4. [x] **Current stays Current** — A child Directory Node the Graph already holds as Current stays Current after the parent reconcile.
5. [x] **Three Loads** — git Load, Directory Load, and Workspace Load each use `discoverImmediateMembers`.
6. [x] **Cold bootstrap** — `readAllDocuments` still returns nested files.

## Finish-up

1. [x] **Piece answers** — Changes during implementation are written on this ticket, or the ticket records that none changed.
2. [x] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [x] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved. doc/current and doc/user do not state this discovery depth.

## See also

1. [09 — Grill reconcile discovery depth](09-grill-reconcile-discovery-depth.md)
2. [10 — Add discoverImmediateMembers](10-add-discover-immediate-members.md)
3. [Parse thread architecture — story path 18 Pull handoff stays the pull sentence](../arch.md)
4. [Parse thread spec — story 18 Pull handoff stays the pull sentence](../spec.md)
5. [core-refinement architecture — §5 item 4 git pull finish](../../core-refinement/arch.md)
6. [Lazy load reconciliation server](../../../src/Server/LazyLoadReconciliationServer.fs)

## Implementation

1. **Parse thread** — git Load, Directory Load, and Workspace Load each hand one Directory Node or Workspace Node to the parse thread. The parse thread calls `discoverImmediateMembers` for that directory. [Directory reconcile](../../../src/Shared/dotnet/DirectoryReconcile.fs) `Input.discover` is that call. There is no depth argument.
2. **Lazy load doors** — `reconcileDirectory` and `reconcileWorkspace` call `discoverImmediateMembers`. A new child Directory is an unread Directory File path. Its artifact text is not read. The Directory Node is Unparsed. A child Directory the Graph holds as Current is omitted, so it stays Current.
3. **Disk-newer child** — On the parse thread, a disk-newer child Directory is still named for push. That stays [07 — Directory Unparsed during reconcile](07-directory-unparsed-during-reconcile.md).
4. **Changed-path reconcile** — `reconcileChangedPaths` still calls `discoverArtifactRelatives`. That union is not a Load reconcile. [12 — Git pull marks each received file Unparsed](12-git-pull-marks-each-received-file-unparsed.md) stays undone. `requeueOnUnparsed` stays false.
5. **Cold bootstrap** — `readAllDocuments` still calls `discoverArtifactRelatives`.
6. **Docs** — [Parse thread architecture](../arch.md) story path 18 **Pull handoff stays the pull sentence**, **One level**, is checked. The Inputs sentence names `discoverImmediateMembers`. [Parse thread spec](../spec.md) story 18 **Pull handoff stays the pull sentence** already stated this depth. No sentence there became false. doc/current and doc/user do not state this discovery depth.
7. **WorkspaceReconcile** — `reconcileChangedPaths`, `reconcileChangedPathsWithDiscovery`, `reconcileDirectory`, `reconcileWorkspace`, and `reconcileAddedPaths` take [WorkspaceReconcile](../../../src/Server/LazyLoadReconciliationServer.fs). That record is the core handle, the data directory, and the workspace label. Private helpers that take the data directory and the workspace label take the same record: the discovery helpers and `readDirInfoArtifacts`.

## Time

- 2026-10-11 45m — one-level Load reconcile, tests, and finish-up (from chat)
- 2026-10-11 20m — WorkspaceReconcile replaces the shared positional arguments (from chat)
