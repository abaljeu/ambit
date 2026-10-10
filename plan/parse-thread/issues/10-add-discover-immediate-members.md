# 10 — Add discoverImmediateMembers

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

Cold bootstrap reads every file under the data directory. `discoverArtifactRelatives` stays that recursive walk for `readAllDocuments`. git Load, Directory Load, and Workspace Load walk one level. They need the direct files and the child directories. That function does not exist yet. This ticket adds it and tests it. No Load reconcile calls it yet.

## What to build

1. [ ] **discoverImmediateMembers** — `discoverImmediateMembers` returns the direct files and the child directories of one directory.
2. [ ] **Recursive walk stays** — `discoverArtifactRelatives` stays recursive. Cold bootstrap through `readAllDocuments` still uses it.
3. [ ] **No Load caller** — git Load, Directory Load, and Workspace Load do not call `discoverImmediateMembers` in this ticket.
4. [ ] **No new positional argument** — Neither function gains a positional depth argument.

### Acceptance Criteria

1. [ ] **Direct files** — A test shows `discoverImmediateMembers` returns the files in that directory.
2. [ ] **Child directories** — A test shows it returns the child directories of that directory.
3. [ ] **Nested files stay out** — A file inside a child directory is absent from that result.
4. [ ] **Recursive function unchanged** — A test shows `discoverArtifactRelatives` still returns nested files.
5. [ ] **No caller** — No Load reconcile calls `discoverImmediateMembers`.

## Finish-up

1. [ ] **Piece answers** — Changes during implementation are written on this ticket, or the ticket records that none changed.
2. [ ] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [ ] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved.

## See also

1. [09 — Grill reconcile discovery depth](09-grill-reconcile-discovery-depth.md)
2. [Parse thread architecture — story path 18 Pull handoff stays the pull sentence](../arch.md)
3. [Parse thread spec — story 18 Pull handoff stays the pull sentence](../spec.md)
4. [11 — Load reconciles walk one level](11-load-reconciles-walk-one-level.md)
5. [Document persist path](../../../src/Server/DocumentPersistPath.fs)
