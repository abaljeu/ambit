# 64 — Merge PostEvent and PostGraphOnly into Change

**Status:** `defined`
**Type:** coding
Actual: 2h
**Blocked by:** 60 — Add Mailbox beside CoreMsg
**Trackers:** architecture

## Context

`CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly` both carry a caller, an `Ev`, and a reply. `CoreMailboxActors.dispatchPostEvent` sends `EventBody.Change` through `CoreEventDispatch.postEvent` with `graphOnly` false. `CoreMailboxEvents.dispatchPostGraphOnly` calls the same function with `graphOnly` true. The parse posters are `ParseThread.postParseOps`, `Api.applyParseFile`, and `LazyLoadReconciliationServer` through `GraphOnlyChangePost.postChunks`. Decision 4 **One Change**, decision 12 **NodeUnpersisted**, and decision 18 **ChangeOrigin** in [Message shape](../cqrs-messages.md): the two doors become `Order.Change` of `ops: Op list * origin: ChangeOrigin`. Those parse posters place origin `Parse`. Every other `Change` poster places origin `Edit`. `origin` exists only on the placed order. `CoreEventDispatch` reads that `origin`, writes document files, and posts `Notice.NodeUnpersisted` for origin `Edit`. Origin `Parse` keeps the file skip `PostGraphOnly` uses today. The logged Change holds the op list. `PostEvent` still carries `Undo`, `Redo`, `ActorStart`, and `Cancel` until [65 — Log Undo and Redo as the target](65-log-undo-and-redo-as-the-target.md) and [66 — Move Actor orders onto Order and Notice](66-move-actor-orders-onto-order-and-notice.md). Upload, import, parse, Load, undo, and persist stay working.

## What to build

`ParseThread.postParseOps`, `Api.applyParseFile`, and `LazyLoadReconciliationServer` through `GraphOnlyChangePost.postChunks` place `Order.Change` with origin `Parse`. Every other `Change` poster places origin `Edit`. `origin` exists only on the placed order. `CoreEventDispatch` reads that `origin`, writes document files, and posts `Notice.NodeUnpersisted` for origin `Edit`. Origin `Parse` keeps the file skip `PostGraphOnly` uses today. The logged Change holds the op list.

### 1. Change

1. [ ] **Door** — `ChangeOrigin` is `Edit | Parse` in `src/Shared/MailboxLog.fs` beside `Order`. `Order.Change` is `ops: Op list * origin: ChangeOrigin`. `origin` exists only on the placed order. `CoreEventDispatch` reads that `origin`. The logged Change holds the op list. `CoreMailboxApi.postEvents` and `postGraphOnly` place `Mailbox.PlaceOrder` of that `Order.Change`. `ParseThread.postParseOps`, `Api.applyParseFile`, and `LazyLoadReconciliationServer` through `GraphOnlyChangePost.postChunks` place origin `Parse`. Every other `Change` poster places origin `Edit`. `Api.postEvents`, `RunAgentActor`, and `TestActor` use `postEvents`. `CoreMsg.PostGraphOnly` leaves. `CoreMsg.PostEvent` stays for `EventBody.Undo`, `EventBody.Redo`, `EventBody.ActorStart`, and `EventBody.Cancel`.
2. [ ] **Reply** — The `PlaceOrder` reply is `Result<EventId, string>`. `Ok` carries the assigned `EventId`. `Api.postEvents` builds the HTTP change response from that id and from the state read it already does. The mailbox reply is not `CoreChangesAccepted`.
3. [ ] **Files** — `CoreEventDispatch` writes document files for a placed `Order.Change` of origin `Edit`. Origin `Parse` keeps the file skip `PostGraphOnly` uses today.
4. [ ] **NodeUnpersisted** — Add `Notice.NodeUnpersisted of nodeId: NodeId`. After a successful placed `Change` of origin `Edit`, `CoreEventDispatch` posts one `Notice.NodeUnpersisted` for each document root of the ops, using `DocumentPartition.documentRootForNode`. `CoreMailboxLoad.applyNotice` sets that node `PersistState.Unpersisted` and appends one `Ev` whose body is that notice. For origin `Parse`, `CoreEventDispatch` keeps the file skip `PostGraphOnly` uses today, and that post's log entry is the `Change`.

### Acceptance Criteria

1. [ ] **One order** — An origin `Edit` change through `postEvents` and an origin `Parse` change through `postGraphOnly` both place `Order.Change` with that origin. Each reply is `Ok` of the new `EventId`. `CoreMsg.PostGraphOnly` leaves.
2. [ ] **Parse** — `ParseThread.postParseOps` places `Order.Change` with origin `Parse` and completes. `CoreEventDispatch` keeps the file skip `PostGraphOnly` uses today. The logged Change holds the op list.
3. [ ] **Edit** — A placed `Change` of origin `Edit` writes the document file the way `PostEvent` does today. The log then has `Notice.NodeUnpersisted` of the document root, and that node is `PersistState.Unpersisted`.
4. [ ] **Other bodies** — `PostEvent` of `Undo`, `Redo`, `ActorStart`, and `Cancel` still uses `CoreMsg.PostEvent`.
5. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## Amendments

1. **Logged notice** — `CoreMailboxLoad.applyNotice` appends one `Ev` whose body is `History.EventBody.NodeUnpersisted`. The queue message is `Notice.NodeUnpersisted`. The JSON kind is `nodeUnpersisted`.
2. **Origin stays on the order** — `MailboxLog.toEv` logs `EventBody.Change` of the op list. `MailboxLog.ofEv` of that log returns Error `origin is not logged`.
3. **HTTP ack** — `Api.postEvents` sets `eventId` from the `PlaceOrder` `EventId`. The `events` array is the request's stored events plus later events that are not that request and are not `NodeUnpersisted`, so a stored `ActorStop` stays on the ack. Poll returns the notice. `externalChanges` is true when the stored ops are not a prefix of the request ops.
4. **Persist message** — `CoreEventDispatch` remembers the persist status string by the assigned `EventId`. `Query.PersistMessage` reads it. `Api` copies that string onto the HTTP `message`. The `PlaceOrder` reply stays `Result<EventId, string>`.
5. **Mailbox log copy** — `applyNotice` appends through `persist.appendEvent`, using the next id after `persist.getEventId`. The notice puller copies that `Ev` onto the mailbox `EventLog` with `EventLog.restore`.
6. **Change is PlaceOrder** — `CoreMailboxActors.dispatchPostEvent` refuses `EventBody.Change`. `CoreMailboxApi.postEvent` places `Mailbox.PlaceOrder` for a Change, then reads the stored `Ev`.
7. **Draft id** — `CoreMailboxApi.placeOne` and `postGraphOnly` refuse a client `EventId` other than zero before `PlaceOrder`.
8. **commandName leftover** — `ChangeAmendment.marksOwningPersist` still keys off `commandName`. The file skip and `Notice.NodeUnpersisted` use `ChangeOrigin` only.
9. **Revision** — `graph.revision` stays the Change id. The notice is a later event row and advances the live event id.
10. **Notice post** — `CoreEventDispatch` posts `Notice.NodeUnpersisted` on the mailbox processor. That post does not take `host.gate`, because the Change handler is already running on the queue.

## Time

- 2026-10-20 2h — placed `Order.Change`, origin `Edit` posts `Notice.NodeUnpersisted` `(from chat)`

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [65 — Log Undo and Redo as the target](65-log-undo-and-redo-as-the-target.md)
- [62 — Admit at the entry](62-admit-at-the-entry.md)
- [GLOSSARY](../../../GLOSSARY.md)
