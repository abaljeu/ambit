# 10 — Add discoverImmediateMembers

**Status:** `coded`
**Type:** coding
Actual: 20m
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

Cold bootstrap reads every file under the data directory. `discoverArtifactRelatives` stays that recursive walk for `readAllDocuments`. git Load, Directory Load, and Workspace Load walk one level. They need the direct files and the child directories. That function does not exist yet. This ticket adds it and tests it. No Load reconcile calls it yet.

## What to build

1. [x] **discoverImmediateMembers** — `discoverImmediateMembers` returns the direct files and the child directories of one directory.
2. [x] **Recursive walk stays** — `discoverArtifactRelatives` stays recursive. Cold bootstrap through `readAllDocuments` still uses it.
3. [x] **No Load caller** — git Load, Directory Load, and Workspace Load do not call `discoverImmediateMembers` in this ticket.
4. [x] **No new positional argument** — Neither function gains a positional depth argument.

### Acceptance Criteria

1. [x] **Direct files** — A test shows `discoverImmediateMembers` returns the files in that directory.
2. [x] **Child directories** — A test shows it returns the child directories of that directory.
3. [x] **Nested files stay out** — A file inside a child directory is absent from that result.
4. [x] **Recursive function unchanged** — A test shows `discoverArtifactRelatives` still returns nested files.
5. [x] **No caller** — No Load reconcile calls `discoverImmediateMembers`.

## Finish-up

1. [x] **Piece answers** — Changes during implementation are written on this ticket, or the ticket records that none changed.
2. [x] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [x] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved. doc/current and doc/user do not state this discovery depth.

## See also

1. [09 — Grill reconcile discovery depth](09-grill-reconcile-discovery-depth.md)
2. [Parse thread architecture — story path 18 Pull handoff stays the pull sentence](../arch.md)
3. [Parse thread spec — story 18 Pull handoff stays the pull sentence](../spec.md)
4. [11 — Load reconciles walk one level](11-load-reconciles-walk-one-level.md)
5. [Document persist path](../../../src/Server/DocumentPersistPath.fs)

## Implementation

1. **Return shape** — `discoverImmediateMembers` returns `Result<string list, string>`. Each path is relative to that directory, with `/` separators and no trailing slash. A direct file is that file's path. A child directory is that directory's path, not a `/.amb` path.
2. **Skips** — A direct file uses the same skips as `discoverArtifactRelatives`: a reserved name, a `.meta` suffix, a `.tmp` suffix, a `.bak.` segment, and a path inside `.git`. A child directory is skipped only when the path is inside `.git`.
3. **Missing directory** — A directory that is not on disk returns `Ok []`.
4. **Callers** — `discoverArtifactRelatives` is unchanged. `readAllDocuments` still calls it. No Load reconcile calls `discoverImmediateMembers`.
5. **Docs** — [Parse thread architecture](../arch.md) story path 18 **Pull handoff stays the pull sentence**, **Two discovery functions**, is checked. [Parse thread spec](../spec.md) story 18 **Pull handoff stays the pull sentence** already states the depth. No sentence there became false. doc/current and doc/user do not state this discovery depth.

## Time

- 2026-10-10 20m — discoverImmediateMembers, tests, and finish-up (from chat)
