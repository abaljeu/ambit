# 29 — Graph change crosses Core loop

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

Only Core changes the graph. The parse thread and the persist thread add `InMsg`. They do not edit the graph. Core loop applies that `InMsg`. The architecture still has an open shared segment: a path that changes the graph crosses Core loop. This ticket owns that beat.

## What to build

A path that changes the graph crosses Core loop. Core loop applies that change on the live graph.

### Graph change

1. [ ] **Cross Core loop** — A path that changes the graph crosses Core loop.
2. [ ] **Core loop writes** — Core loop applies that graph change on the live graph.

### Obligations

1. [ ] **Mailbox claim** — The Mailbox claim says a path that changes the graph crosses Core loop.
2. [ ] **Graph-change test** — A test shows a graph change is applied by Core loop.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement](plan/core-refinement/project.md)
- [Mailbox](doc/current/mailbox.md)
