# 64 — Merge PostEvent and PostGraphOnly into Change

**Status:** `defined`
**Type:** coding
**Blocked by:** 60 — Add Mailbox beside CoreMsg
**Trackers:** architecture

## Context

`CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly` both carry a caller, an `Ev`, and a reply. `CoreMailboxActors.dispatchPostEvent` sends `EventBody.Change` through `CoreEventDispatch.postEvent` with `graphOnly` false. `CoreMailboxEvents.dispatchPostGraphOnly` calls the same function with `graphOnly` true. Every `PostGraphOnly` caller mints `commandName` `"Parse"`: `ParseThread.postParseOps`, `Api.applyParseFile`, and `LazyLoadReconciliationServer` through `GraphOnlyChangePost.postChunks`. Decision 4 **One Change** and decision 12 **NodeUnpersisted** in [Message shape](../cqrs-messages.md): the two doors become `Order.Change`, and after an ordinary edit the server's `Change` handler posts `Notice.NodeUnpersisted`. `PostEvent` still carries `Undo`, `Redo`, `ActorStart`, and `Cancel` until [65 — Log Undo and Redo as the target](65-log-undo-and-redo-as-the-target.md) and [66 — Move Actor orders onto Order and Notice](66-move-actor-orders-onto-order-and-notice.md). Upload, import, parse, Load, undo, and persist stay working.

## What to build

`Api.postEvents` and the Parse `postGraphOnly` callers place `Order.Change`. The `Change` handler posts `Notice.NodeUnpersisted` after an ordinary edit. A `Parse` change keeps today's file skip.

### 1. Change

1. [ ] **Door** — `CoreMailboxApi.postEvents` and `postGraphOnly` place `Mailbox.PlaceOrder` of `Order.Change` of the op list. `Api.postEvents`, `RunAgentActor`, and `TestActor` use `postEvents`. `ParseThread.postParseOps`, `Api.applyParseFile`, and `LazyLoadReconciliationServer` use `postGraphOnly`. Remove `CoreMsg.PostGraphOnly`. `CoreMsg.PostEvent` stays for `EventBody.Undo`, `EventBody.Redo`, `EventBody.ActorStart`, and `EventBody.Cancel`.
2. [ ] **Reply** — The `PlaceOrder` reply is `Result<EventId, string>`. `Ok` carries the assigned `EventId`. `Api.postEvents` builds the HTTP change response from that id and from the state read it already does. The mailbox reply is not `CoreChangesAccepted`.
3. [ ] **Files** — `Order.Change` has no graph-only flag. `CoreEventDispatch` writes document files when `commandName` is not `Some "Parse"`. A `Change` whose `commandName` is `Some "Parse"` keeps the file skip `PostGraphOnly` uses today.
4. [ ] **NodeUnpersisted** — Add `Notice.NodeUnpersisted of nodeId: NodeId`. After a successful `Change` whose `commandName` is not `Some "Parse"`, `CoreEventDispatch` posts one `Notice.NodeUnpersisted` for each document root of the ops, using `DocumentPartition.documentRootForNode`. `CoreMailboxLoad.applyNotice` sets that node `PersistState.Unpersisted` and appends one `Ev` whose body is that notice. A `Parse` change posts no `Notice.NodeUnpersisted`.

### Acceptance Criteria

1. [ ] **One order** — `postEvents` of a `Change` and `postGraphOnly` of a `Parse` change both place `Order.Change`. Each reply is `Ok` of the new `EventId`. `CoreMsg` has no `PostGraphOnly`.
2. [ ] **Parse** — `ParseThread.postParseOps` completes and does not write the document file. The log for that change has no `Notice.NodeUnpersisted`.
3. [ ] **Edit** — A `Change` whose `commandName` is not `Parse` writes the document file the way `PostEvent` does today. The log then has `Notice.NodeUnpersisted` of the document root, and that node is `PersistState.Unpersisted`.
4. [ ] **Other bodies** — `PostEvent` of `Undo`, `Redo`, `ActorStart`, and `Cancel` still uses `CoreMsg.PostEvent`.
5. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [65 — Log Undo and Redo as the target](65-log-undo-and-redo-as-the-target.md)
- [62 — Admit at the entry](62-admit-at-the-entry.md)
- [GLOSSARY](../../../GLOSSARY.md)
