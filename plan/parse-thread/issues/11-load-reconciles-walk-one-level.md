# 11 — Load reconciles walk one level

**Status:** `defined`
**Type:** coding
**Blocked by:** 10 — Add discoverImmediateMembers
**Trackers:** architecture, project spec

## Context

A person runs git Load, Directory Load, or Workspace Load. Each reconcile walks one level. Immediate members are the files and the child directories in that directory. A newly discovered child Directory Node is Unparsed. Its contents stay unread until that Directory Node is Loaded. A child Directory Node the Graph already holds as Current is left alone. Cold bootstrap stays the recursive walk.

## What to build

1. [ ] **git Load** — git Load reconcile calls `discoverImmediateMembers`. It does not walk the whole tree.
2. [ ] **Directory Load** — Directory Load reconcile calls `discoverImmediateMembers`.
3. [ ] **Workspace Load** — Workspace Load reconcile calls `discoverImmediateMembers`.
4. [ ] **New child Directory Node** — A newly discovered child Directory Node is Unparsed. Its contents stay unread until Load.
5. [ ] **Held Current** — A child Directory Node the Graph already holds as Current stays Current. The parent reconcile does not mark it Unparsed.
6. [ ] **Cold bootstrap stays recursive** — `readAllDocuments` still uses `discoverArtifactRelatives`.

### Acceptance Criteria

1. [ ] **One-level files** — After git Load, Directory Load, or Workspace Load, a file in that directory is discovered. A file inside a child directory is not discovered by that walk.
2. [ ] **One-level directories** — The same walk discovers a child directory. It does not discover a directory nested under that child.
3. [ ] **New Directory Node** — A child directory the Graph lacks gets a Directory Node. That Directory Node is Unparsed. Its contents stay unread until Load.
4. [ ] **Current stays Current** — A child Directory Node the Graph already holds as Current stays Current after the parent reconcile.
5. [ ] **Three Loads** — git Load, Directory Load, and Workspace Load each use `discoverImmediateMembers`.
6. [ ] **Cold bootstrap** — `readAllDocuments` still returns nested files.

## Finish-up

1. [ ] **Piece answers** — Changes during implementation are written on this ticket, or the ticket records that none changed.
2. [ ] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [ ] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved.

## See also

1. [09 — Grill reconcile discovery depth](09-grill-reconcile-discovery-depth.md)
2. [10 — Add discoverImmediateMembers](10-add-discover-immediate-members.md)
3. [Parse thread architecture — story path 18 Pull handoff stays the pull sentence](../arch.md)
4. [Parse thread spec — story 18 Pull handoff stays the pull sentence](../spec.md)
5. [core-refinement architecture — §5 item 4 git pull finish](../../core-refinement/arch.md)
6. [Lazy load reconciliation server](../../../src/Server/LazyLoadReconciliationServer.fs)
