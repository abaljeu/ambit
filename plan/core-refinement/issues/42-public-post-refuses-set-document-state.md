# 42 — Public post refuses SetDocumentState

**Status:** `defined`
**Type:** coding
**Blocked by:** [41 — markUnparsedThenPush stays on the mailbox loop](41-mark-unparsed-then-push-on-the-mailbox-loop.md)
**Trackers:** architecture

## Context

[Graph change contract](plan/core-refinement/arch.md#graph-change-contract) closes the public post. The mailbox writer already logs `Op.SetDocumentState`, and every caller already uses that writer. The client displays document state from that Event. The Op stays. Upload, import, undo, and stored replay stay working.

## What to build

A new public Change, and `postGraphOnly`, that contain `Op.SetDocumentState` are refused. Nothing in that post is stored. The client still receives the parse-state Event from the mailbox writer.

### Contract

1. [ ] **Refuse the new write** — `CoreEventDispatch.postEvent` refuses a new Change whose ops contain `Op.SetDocumentState`. `postGraphOnly` uses that same refusal. The refusal runs before any Event in that post is applied. The graph is unchanged. No earlier Event in that post is stored.
2. [ ] **Client Event** — The mailbox writer still appends `Op.SetDocumentState` when it changes the three-way parse state. The client receives that Event.
3. [ ] **Undo and Redo stay** — An incoming Undo or Redo is not this refusal. The inverse still applies through the mailbox writer. The Undo or Redo Event is stored. The client receives the parse-state Event.
4. [ ] **Stored replay stays** — The same `submissionId` returns the stored Event, including when that Event carries `Op.SetDocumentState`. `EventLog.recover` still applies a stored Event that carries the Op. Recover is not the public post.
5. [ ] **Op stays** — `Op.SetDocumentState` stays in [History](src/Shared/History.fs) and in the JSON codec. It carries the three-way parse state.
6. [ ] **Four functions** — Upload, import, undo, and stored replay succeed. Their public submission does not contain the Op.

### Obligations

1. [ ] **Refusal test** — `public post of SetDocumentState is refused` in [Core loop InMsg tests](tests/Server.Tests/CoreLoopInMsgTests.fs). The post returns an error. The graph is unchanged.
2. [ ] **List test** — `a posted list with SetDocumentState applies nothing` in [Ordered event stream tests](tests/Server.Tests/OrderedEventStreamTests.fs). An earlier Event in that post is not stored.
3. [ ] **Four functions** — `upload through the mailbox writer ends NoServerFile`, `import through the mailbox writer ends Parsed`, `undo of SetDocumentState restores the prior parse state`, and `stored replay returns the stored Event` still pass. The client still receives the parse-state Event on each.
4. [ ] **Recover test** — `File recover applies Ops Ev ahead of Graph checkpoint` still passes.
5. [ ] **Op claim** — [Op](doc/current/op.md) says the public post refuses a new `Op.SetDocumentState`, and the Op stays as the Event the client displays. The writer of the axis is the mailbox writer on Core loop.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change contract](plan/core-refinement/arch.md#graph-change-contract)
- [core-refinement](plan/core-refinement/project.md)
- [41 — markUnparsedThenPush stays on the mailbox loop](41-mark-unparsed-then-push-on-the-mailbox-loop.md)
- [Op](doc/current/op.md)
- [Mailbox](doc/current/mailbox.md)
