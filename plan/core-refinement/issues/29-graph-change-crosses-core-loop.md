# 29 — Graph change crosses Core loop

**Status:** `coded`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture
Actual: 3h15m

## Context

Only Core changes the graph. The parse thread and the persist thread add `InMsg` through the private function they receive from the mailbox. They do not edit the graph directly. Core loop applies `InMsg`.

Two paths still bypass that rule:

1. `CoreMailboxLoad.makeMailBox` returns `pushReporting`, which calls `markUnparsedThenPush` (`writeParseState` → `persist.replaceGraph`) on the caller's thread outside the mailbox processor. `markUnparsedThenPush` must be invokable only inside the mailbox. Any secondary API for parse state must enqueue on the mailbox processor (`InMsg` through the private add function).
2. Parse and reconcile callers (`ImportDocument.planParseFile`, `DirectoryReconcile.planDirectoryReconcile`, `LazyLoadReconciliationApply`) still write the parsed axis through `Op.SetDocumentState` on the public post API (`postGraphOnly` / `postEvents`). They have authority to make parse-state changes, and those changes must go through the private `InMsg` queue they receive from the mailbox (`InMsg.ParseFinished`, `InMsg.MarkUnparsed`), not the public `Op` / event API.

## What to build

Every graph change and parse-state write crosses the `MailboxProcessor` in Core loop.

### 1. Mailbox-only `markUnparsedThenPush`

1. [x] **Mailbox-internal `markUnparsedThenPush`** — `CoreMailboxLoad.markUnparsedThenPush` is private to the mailbox loop (`dispatchLoad` on the `MailboxProcessor`). It is not returned to external callers and cannot be invoked off the mailbox thread.
2. [x] **Secondary parse-state API queues through mailbox** — Any secondary handle handed to `ParseThread` (or other authorized callers) to mark a node Unparsed or enqueue a parse target posts `InMsg` (`InMsg.MarkUnparsed` / `InMsg.ParseFinished`) through `CoreMailboxApi.addInMsg` onto the `MailboxProcessor<QueueSum>` queue instead of calling `writeParseState` or `replaceGraph` directly.

### 2. Parse-state changes use the private `InMsg` queue, not public `Op`

1. [x] **Parse and reconcile stop emitting `Op.SetDocumentState` for the parsed axis** — `ImportDocument.planParseFile`, `DirectoryReconcile.planDirectoryReconcile`, and `LazyLoadReconciliationServer` / `LazyLoadReconciliationApply` do not emit `Op.SetDocumentState` to flip `Unparsed` ↔ `Current` (`Parsed`) on the public event door.
2. [x] **Authorized callers use the private `InMsg` queue** — Authorized parse and reconcile workers receive the private `InMsg` add function from the mailbox and send `InMsg.ParseFinished` or `InMsg.MarkUnparsed` through that queue, which Core loop applies via `GraphMutate.setParseState`.
3. [x] **`Op.SetDocumentState` is not the writer of the parsed axis** — Closes [core-refinement architecture](plan/core-refinement/arch.md) §10.1.2 item 9 (`Private axes`): `PersistState` and the parsed axis are written by Core loop via `InMsg`, not via `Op.SetPersistState` or `Op.SetDocumentState`.

### Obligations

1. [x] **Mailbox claim** — [Mailbox](doc/current/mailbox.md) claims that `markUnparsedThenPush` runs only inside the mailbox loop and that parse-state writes from authorized workers cross the mailbox via the private `InMsg` queue rather than public `Op` posts.
2. [x] **Op claim** — [Op](doc/current/op.md) reflects that `Op.SetDocumentState` is not the writer of the parsed axis.
3. [x] **Graph-change test** — Tests verify that `markUnparsedThenPush` is not exposed off-loop, that secondary parse-state calls enqueue `InMsg` on the mailbox processor, and that file parse / directory reconcile update `ParseState` via `InMsg` on Core loop rather than `Op.SetDocumentState`.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

Amendments:

- `Api.postParseFile` enqueues `InMsg.ParseFinished` before content and `InMsg.MarkUnparsed` if that content post fails. The HTTP parse door uses the same queue order as the parse thread.
- `LazyLoadReconciliationApply` still constructs `Op.SetDocumentState` so the planner can apply the transition on its local graph. `LazyLoadReconciliationServer` strips those ops and enqueues `InMsg` instead. They do not reach the public post. The user plan routed lazy-load through `InMsg` at that boundary. The ticket names Apply as a producer that must not emit the op on the public door. That door is clean. The constructor remains so the planner's local apply stays valid.
- A successful parse is two logged events: the parse-axis Ev, then the content Ev. A failed content post logs the forward transition and then `MarkUnparsed`.
- `NoServerFile` to Current is `MarkUnparsed` then `ParseFinished`. The intermediate Unparsed state is observable.
- A logged `SetDocumentState` cannot be undone through the public post, because the inverse is the same op and the public post refuses it.
- Creating a special node while it is Unparsed does not write its artifact. Persist skips an Unparsed owner, and the new node stays `PersistState.Persisted`, so parse catch-up does not rewrite it.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement](plan/core-refinement/project.md)
- [Mailbox](doc/current/mailbox.md)
- [Op](doc/current/op.md)

## Time

- 2026-10-16 15m — investigate and define concrete scope for mailbox-only `markUnparsedThenPush` and private `InMsg` parse-state queue `(from chat)`
- 2026-10-17 3h — mailbox refuses public `SetDocumentState`, logs the op from `InMsg`, and file parse / directory reconcile stop emitting it `(from chat)`
