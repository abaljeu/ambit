# 39 — Upload, import, undo, and stored replay use the mailbox writer

**Status:** `defined`
**Type:** coding
**Blocked by:** [38 — Mailbox parse-state writer logs the Event](38-mailbox-parse-state-writer-logs-the-event.md)
**Trackers:** architecture

## Context

[Graph change migrate — upload, import, undo, and stored replay](plan/core-refinement/arch.md#graph-change-migrate-upload) moves those four onto the mailbox writer from [38 — Mailbox parse-state writer logs the Event](38-mailbox-parse-state-writer-logs-the-event.md). That writer already logs `Op.SetDocumentState`. The client still receives that Event. The public post still accepts the Op.

## What to build

Upload, import, undo, and stored replay apply the three-way parse state through the mailbox writer. The client still receives the parse-state Event. Those four stay working.

### Migrate

1. [ ] **Upload** — [Workspace upload structure](src/Shared/WorkspaceUploadStructure.fs) reaches the mailbox writer. NoServerFile is `InMsg.SetDocumentState`. Unparsed is `InMsg.MarkUnparsed`. Parsed is `InMsg.ParseFinished`. A file planned as NoServerFile ends NoServerFile.
2. [ ] **Import** — [Import text](src/Shared/ImportText.fs) `markDocumentCurrentBeforeParse` reaches the mailbox writer as `InMsg.ParseFinished`. Import still attaches the imported nodes. The document ends Parsed.
3. [ ] **Undo** — An Undo whose inverse includes `Op.SetDocumentState` applies that inverse through the mailbox writer. The node returns to the prior value. The Undo Event is stored. The client receives the parse-state Event.
4. [ ] **Stored replay** — The same `submissionId` returns the stored Event. A stored Event that carries `Op.SetDocumentState` is returned. The client receives that Event.
5. [ ] **Public body** — These four do not put `Op.SetDocumentState` on the public post. The logged Event from the mailbox writer is the Event the client displays.
6. [ ] **Public post** — A direct public post of `Op.SetDocumentState` still succeeds.

### Obligations

1. [ ] **Upload test** — `upload through the mailbox writer ends NoServerFile` in [Core loop InMsg tests](tests/Server.Tests/CoreLoopInMsgTests.fs). The file is NoServerFile. The client receives the parse-state Event. Upload succeeds.
2. [ ] **Import test** — `import through the mailbox writer ends Parsed` in the same file. The document is Parsed. The imported nodes are attached. The client receives the parse-state Event.
3. [ ] **Undo test** — `undo of SetDocumentState restores the prior parse state` in the same file. The client receives the parse-state Event.
4. [ ] **Replay test** — `stored replay returns the stored Event` in the same file. The stored body carries `Op.SetDocumentState`.
5. [ ] **Door test** — `public post of SetDocumentState is accepted` still passes.
6. [ ] **Existing tests stay green** — `planServerFilePresentOps transitions only matching absent files`, `build import marks unparsed file current before tree operations`, `valid parse batch can replay undo and redo`, and `File recover applies Ops Ev ahead of Graph checkpoint`.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change migrate — upload, import, undo, and stored replay](plan/core-refinement/arch.md#graph-change-migrate-upload)
- [core-refinement](plan/core-refinement/project.md)
- [38 — Mailbox parse-state writer logs the Event](38-mailbox-parse-state-writer-logs-the-event.md)
- [40 — Parse and reconcile use the mailbox writer](40-parse-and-reconcile-use-the-mailbox-writer.md)
