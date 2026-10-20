# 64 — Merge PostEvent and PostGraphOnly into Change

**Status:** `defined`
**Type:** coding
**Blocked by:** 60 — Add Mailbox beside CoreMsg; 44 — Apply document state from Node events; 51 — Stand the persist stack
**Trackers:** architecture

## Context

`CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly` both carry a caller, an `Ev`, and a reply. Both send `EventBody.Change` through `CoreEventDispatch.postEvent`.

- **Change** — `CoreMailboxActors.dispatchPostEvent` calls that function with `graphOnly` false.
- **Graph only** — `CoreMailboxEvents.dispatchPostGraphOnly` calls that function with `graphOnly` true.

The parse posters place origin `Parse`:

- **Parse ops** — `ParseThread.postParseOps`
- **Parse file** — `Api.applyParseFile`
- **Load** — `LazyLoadReconciliationServer` through `GraphOnlyChangePost.postChunks`

Decision 4 **One Change**, decision 12 **NodeUnpersisted**, and decision 18 **ChangeOrigin** in [Message shape](../cqrs-messages.md): the two doors become `Order.Change` of `ops: Op list * origin: ChangeOrigin`. Every other `Change` poster places origin `Edit`. `origin` exists only on the placed order. `CoreEventDispatch` reads that `origin`, writes document files, and posts `Notice.NodeUnpersisted` for origin `Edit`. Origin `Parse` keeps the file skip `PostGraphOnly` uses today. The logged Change holds the op list.

`CoreMsg.PostEvent` also carries `Undo`, `Redo`, `ActorStart`, and `Cancel`. [65 — Undo and Redo resolve their ops from the target](65-undo-and-redo-resolve-their-ops-from-the-target.md) and [66 — Move Actor orders onto Order and Notice](66-move-actor-orders-onto-order-and-notice.md) take those bodies. Upload, import, parse, Load, undo, and persist stay working.

## What to build

1. [ ] **Door** — Decision 4 **One Change** and decision 18 **ChangeOrigin**. `ChangeOrigin` is `Edit | Parse` in `src/Shared/MailboxLog.fs` beside `Order`. `Order.Change` is `ops: Op list * origin: ChangeOrigin`. `origin` exists only on the placed order. `CoreEventDispatch` reads that `origin`. The logged Change holds the op list. `CoreMailboxApi.postEvents` and `postGraphOnly` place `Mailbox.PlaceOrder` of that `Order.Change`. `Api.postEvents`, `RunAgentActor`, and `TestActor` use `postEvents`. The Parse callers in Context use `postGraphOnly` and place origin `Parse`. Every other `Change` poster places origin `Edit`. Remove `CoreMsg.PostGraphOnly`. `CoreMsg.PostEvent` stays for the four bodies Context names.
2. [ ] **Reply** — The `PlaceOrder` reply is `Result<EventId, string>`. `Ok` carries the assigned `EventId`. `Api.postEvents` builds the HTTP change response from that id and from the state read it already does. The mailbox reply is not `CoreChangesAccepted`.
3. [ ] **Files** — `CoreEventDispatch` writes document files for a placed `Order.Change` of origin `Edit`. Origin `Parse` keeps the file skip `PostGraphOnly` uses today.
4. [ ] **NodeUnpersisted** — Decision 12 **NodeUnpersisted**. Add `Notice.NodeUnpersisted of nodeId: NodeId`. After a successful placed `Change` of origin `Edit`, `CoreEventDispatch` posts one `Notice.NodeUnpersisted` for each document root of the ops, using `DocumentPartition.documentRootForNode`. `CoreMailboxLoad.applyNotice` sets that node `PersistState.Unpersisted` and appends one `Ev` whose body is that notice. For origin `Parse`, `CoreEventDispatch` keeps the file skip `PostGraphOnly` uses today, and that post's log entry is the `Change`.

### Acceptance Criteria

1. [ ] **One order** — `postEvents and postGraphOnly place Order.Change`. An origin `Edit` change through `postEvents` and an origin `Parse` change through `postGraphOnly` both place `Order.Change` with that origin. Each reply is `Ok` of the new `EventId`. `CoreMsg.PostGraphOnly` leaves.
2. [ ] **Parse** — `postParseOps skips the document file`. `ParseThread.postParseOps` places `Order.Change` with origin `Parse` and completes. `CoreEventDispatch` keeps the file skip `PostGraphOnly` uses today. The logged Change holds the op list.
3. [ ] **Edit** — `an ordinary Change writes the document file`. A placed `Change` of origin `Edit` writes the document file the way `PostEvent` does today. The log then has `Notice.NodeUnpersisted` of the document root, and that node is `PersistState.Unpersisted`.
4. [ ] **Other bodies** — `the four bodies Context names stay on PostEvent`. A post of each of those bodies still uses `CoreMsg.PostEvent`.
5. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [65 — Undo and Redo resolve their ops from the target](65-undo-and-redo-resolve-their-ops-from-the-target.md)
- [66 — Move Actor orders onto Order and Notice](66-move-actor-orders-onto-order-and-notice.md)
- [62 — Admit at the entry](62-admit-at-the-entry.md)
- [GLOSSARY](../../../GLOSSARY.md)
