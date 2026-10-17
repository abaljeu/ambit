# 39 — Migrate upload, import, undo, and stored replay

**Status:** `defined`
**Type:** coding
**Blocked by:** [38 — Expand both parse-state outputs](38-expand-both-parse-state-outputs.md)
**Trackers:** architecture

## Context

[Graph change migrate — upload, import, undo, and stored replay](plan/core-refinement/arch.md#graph-change-migrate-upload). These callers rely on the new output. Today's write stays.

## What to build

Upload, import, undo, and stored replay rely on the mailbox writer output.

### Migrate

1. [ ] **Upload** — Upload relies on the mailbox writer. NoServerFile uses `InMsg.SetDocumentState`.
2. [ ] **Import** — Import relies on the mailbox writer. Parsed uses `InMsg.ParseFinished`.
3. [ ] **Undo** — Undo of a parse-state change relies on the mailbox writer.
4. [ ] **Stored replay** — Stored replay returns the stored Event. That Event carries `Op.SetDocumentState`.

### Obligations

Tests live in [Core loop InMsg tests](tests/Server.Tests/CoreLoopInMsgTests.fs).

1. [ ] **`upload relies on the mailbox writer`** — The file ends NoServerFile from the new output.
2. [ ] **`import relies on the mailbox writer`** — The document ends Parsed from the new output.
3. [ ] **`undo relies on the mailbox writer`** — The node returns to the prior value from the new output.
4. [ ] **`stored replay relies on the mailbox writer`** — The stored Event carries `Op.SetDocumentState`.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change migrate — upload, import, undo, and stored replay](plan/core-refinement/arch.md#graph-change-migrate-upload)
- [core-refinement](plan/core-refinement/project.md)
