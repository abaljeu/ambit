# 40 — Parse and reconcile use the mailbox writer

**Status:** `defined`
**Type:** coding
**Blocked by:** [39 — Upload, import, undo, and stored replay use the mailbox writer](39-upload-import-undo-replay-use-the-mailbox-writer.md)
**Trackers:** architecture

## Context

[Graph change migrate — file parse, directory reconcile, and lazy load](plan/core-refinement/arch.md#graph-change-migrate-parse) moves those callers onto the mailbox writer. The writer already logs `Op.SetDocumentState`. The client still receives that Event. Upload, import, undo, and stored replay already use the writer and stay working. The public post still accepts the Op.

## What to build

File parse, directory reconcile, and lazy load apply the three-way parse state through the mailbox writer. Content ops still use `postGraphOnly`. The client still receives the parse-state Event.

### Migrate

1. [ ] **File parse** — [Import document](src/Shared/dotnet/ImportDocument.fs) `planParseFile` reaches the mailbox writer. Unparsed to Parsed is `InMsg.ParseFinished`. Content ops still post. The client receives the parse-state Event from the mailbox writer.
2. [ ] **Directory reconcile** — [Directory reconcile](src/Shared/dotnet/DirectoryReconcile.fs) `planDirectoryReconcile` reaches the mailbox writer. Parsed is `InMsg.ParseFinished`. Unparsed is `InMsg.MarkUnparsed`. Content ops still post.
3. [ ] **Lazy load** — [Lazy load reconciliation server](src/Server/LazyLoadReconciliationServer.fs) sends a planned parse-state transition to the mailbox writer. NoServerFile is `InMsg.SetDocumentState`. Content ops still post through `postGraphOnly`. The client receives the parse-state Event.
4. [ ] **Public body** — These callers do not put `Op.SetDocumentState` on the public post. The logged Event is the Event the client displays.
5. [ ] **Public post** — A direct public post of `Op.SetDocumentState` still succeeds.

### Obligations

1. [ ] **Reconcile test** — `reconcile reaches Parsed through the mailbox writer` in [Core loop InMsg tests](tests/Server.Tests/CoreLoopInMsgTests.fs). The node is Parsed. The content ops are posted. The client receives the parse-state Event.
2. [ ] **Door test** — `public post of SetDocumentState is accepted` still passes.
3. [ ] **Four functions** — `upload through the mailbox writer ends NoServerFile`, `import through the mailbox writer ends Parsed`, `undo of SetDocumentState restores the prior parse state`, and `stored replay returns the stored Event` still pass. The client still receives the parse-state Event on each.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change migrate — file parse, directory reconcile, and lazy load](plan/core-refinement/arch.md#graph-change-migrate-parse)
- [core-refinement](plan/core-refinement/project.md)
- [39 — Upload, import, undo, and stored replay use the mailbox writer](39-upload-import-undo-replay-use-the-mailbox-writer.md)
- [41 — markUnparsedThenPush stays on the mailbox loop](41-mark-unparsed-then-push-on-the-mailbox-loop.md)
