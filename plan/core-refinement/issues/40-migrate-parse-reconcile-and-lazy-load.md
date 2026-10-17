# 40 — Migrate parse, reconcile, and lazy load

**Status:** `defined`
**Type:** coding
**Blocked by:** [38 — Expand both parse-state outputs](38-expand-both-parse-state-outputs.md)
**Trackers:** architecture

## Context

[Graph change migrate — file parse, directory reconcile, and lazy load](plan/core-refinement/arch.md#graph-change-migrate-parse). These callers rely on the new output. Today's write stays.

## What to build

File parse, directory reconcile, and lazy load rely on the mailbox writer output.

### Migrate

1. [ ] **File parse** — File parse relies on the mailbox writer. Parsed uses `InMsg.ParseFinished`.
2. [ ] **Directory reconcile** — Directory reconcile relies on the mailbox writer. Parsed uses `InMsg.ParseFinished`. Unparsed uses `InMsg.MarkUnparsed`.
3. [ ] **Lazy load** — Lazy load relies on the mailbox writer. NoServerFile uses `InMsg.SetDocumentState`.

### Obligations

Tests live in [Core loop InMsg tests](tests/Server.Tests/CoreLoopInMsgTests.fs).

1. [ ] **`file parse relies on the mailbox writer`** — The node ends Parsed from the new output.
2. [ ] **`directory reconcile relies on the mailbox writer`** — The node ends at the planned parse state from the new output.
3. [ ] **`lazy load relies on the mailbox writer`** — The file ends NoServerFile from the new output.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change migrate — file parse, directory reconcile, and lazy load](plan/core-refinement/arch.md#graph-change-migrate-parse)
- [core-refinement](plan/core-refinement/project.md)
