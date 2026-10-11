# 12 — Git pull marks each received file Unparsed

**Status:** `coded`
Actual: 1h 30m
**Type:** coding
**Blocked by:** 11 — Load reconciles walk one level
**Trackers:** architecture, project spec

## Context

A git pull receives files. A received file can sit at any depth. The pull marks each of those files Unparsed. The git changed list is that file list. The changed list does not discover members. git Load, Directory Load, and Workspace Load keep the one-level walk.

## What to build

1. [x] **Each received file** — A git pull marks each file in the git changed list Unparsed.
2. [x] **Any depth** — A nested file in that list is Unparsed. The mark does not stop at the top directory.
3. [x] **Not discovery** — The changed list does not add discovered members. Discovery for the Load reconciles stays the one-level walk.
4. [x] **Files only** — The mark is for each received file. The changed list does not mark a Directory Node.

### Acceptance Criteria

1. [x] **Top-level file** — A received file in the top directory is Unparsed after the pull.
2. [x] **Nested file** — A received file inside a child directory is Unparsed after the pull.
3. [x] **No discovery from the list** — A path that is only on the changed list does not become a discovered member of the one-level walk.
4. [x] **Directory Node stays** — A directory path on the changed list does not mark that Directory Node Unparsed through this list.
5. [x] **One-level walk stays** — git Load, Directory Load, and Workspace Load still use `discoverImmediateMembers`.

## Finish-up

1. [x] **Piece answers** — Changes during implementation are written on this ticket, or the ticket records that none changed.
2. [x] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [x] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved.

## See also

1. [09 — Grill reconcile discovery depth](09-grill-reconcile-discovery-depth.md)
2. [11 — Load reconciles walk one level](11-load-reconciles-walk-one-level.md)
3. [Parse thread architecture — story path 18 Pull handoff stays the pull sentence](../arch.md)
4. [Parse thread spec — story 18 Pull handoff stays the pull sentence](../spec.md)
5. [core-refinement architecture — §5 item 4 git pull finish](../../core-refinement/arch.md)
6. [07 — Git changed list sets Unparsed](../../core-refinement/issues/07-git-changed-list-unparsed.md)
7. [Workspace git](../../../src/Server/WorkspaceGit.fs)

## Implementation

1. **Received files** — Git pull calls [Lazy load reconciliation server](../../../src/Server/LazyLoadReconciliationServer.fs) `reconcileReceivedFiles`. That door plans the git changed list. It omits `discoverArtifactRelatives`.
2. **Any depth** — [Lazy load reconciliation](../../../src/Shared/dotnet/LazyLoadReconciliation.fs) `receivedFileChanges` keeps each file path, including a nested path. The existing changed-path plan marks that File Node Unparsed.
3. **Not discovery** — A disk file absent from the changed list stays out of the pull plan. git Load, Directory Load, and Workspace Load still call `discoverImmediateMembers`.
4. **Files only** — `receivedFileChanges` drops a directory path. The pull leaves that Directory Node unmarked by this list. A parent Directory Node that a nested file creates still becomes Current when it gains that file. That parent update is the existing member plan.
5. **Piece answers** — None of the piece answers changed.
6. **Docs** — [Parse thread architecture](../arch.md) story path 18 **Pull handoff stays the pull sentence**, **Received files**, is checked. [Parse thread spec](../spec.md) story 18 **Pull handoff stays the pull sentence** already stated this mark. Further note 10 **Dual Unparsed approaches** now states the received-file mark. [core-refinement architecture](../../core-refinement/arch.md) §5 **git pull finish** already stated this lock. doc/current and doc/user do not state this mark.

## Time

- 2026-10-11 1h 30m — received-file pull mark, tests, and finish-up (from chat)
