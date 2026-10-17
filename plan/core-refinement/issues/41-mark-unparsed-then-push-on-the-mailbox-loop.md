# 41 — markUnparsedThenPush stays on the mailbox loop

**Status:** `defined`
**Type:** coding
**Blocked by:** [40 — Parse and reconcile use the mailbox writer](40-parse-and-reconcile-use-the-mailbox-writer.md)
**Trackers:** architecture

## Context

[Graph change migrate — markUnparsedThenPush](plan/core-refinement/arch.md#graph-change-migrate-push) keeps that function on the mailbox loop. The mailbox writer from [38 — Mailbox parse-state writer logs the Event](38-mailbox-parse-state-writer-logs-the-event.md) already logs `Op.SetDocumentState`. The client still receives that Event. Upload, import, undo, and stored replay stay working. The public post still accepts the Op.

## What to build

`markUnparsedThenPush` runs only inside the mailbox loop. A caller outside that loop enqueues `InMsg.MarkUnparsed`. The client still receives the parse-state Event.

### Migrate

1. [ ] **Private** — `CoreMailboxLoad.markUnparsedThenPush` is private to the mailbox loop. `makeMailBox` does not return it.
2. [ ] **Host** — [Core mailbox](src/Server/Core/CoreMailbox.fs) `hostPublishing` and [Core runtime](src/Server/Core/CoreRuntime.fs) `createPublishing` do not return an off-loop push.
3. [ ] **Callers** — A caller that marked a node Unparsed off the loop calls `CoreMailboxApi.addInMsg` with `InMsg.MarkUnparsed`. The mailbox writer sets Unparsed and logs `Op.SetDocumentState`. The client receives that Event.
4. [ ] **Public post** — A direct public post of `Op.SetDocumentState` still succeeds.

### Obligations

1. [ ] **Queue test** — `MarkUnparsed on the mailbox loop sets Unparsed and logs the Event` in [Core loop InMsg tests](tests/Server.Tests/CoreLoopInMsgTests.fs). The node is Unparsed. The client receives the parse-state Event. The test does not call `markUnparsedThenPush` from outside the mailbox.
2. [ ] **Door test** — `public post of SetDocumentState is accepted` still passes.
3. [ ] **Four functions** — `upload through the mailbox writer ends NoServerFile`, `import through the mailbox writer ends Parsed`, `undo of SetDocumentState restores the prior parse state`, and `stored replay returns the stored Event` still pass. The client still receives the parse-state Event on each.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change migrate — markUnparsedThenPush](plan/core-refinement/arch.md#graph-change-migrate-push)
- [core-refinement](plan/core-refinement/project.md)
- [40 — Parse and reconcile use the mailbox writer](40-parse-and-reconcile-use-the-mailbox-writer.md)
- [42 — Public post refuses SetDocumentState](42-public-post-refuses-set-document-state.md)
