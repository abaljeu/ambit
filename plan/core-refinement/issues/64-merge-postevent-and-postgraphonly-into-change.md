# 64 — Merge PostEvent and PostGraphOnly into Change

**Status:** `defined`
**Type:** coding
**Blocked by:** 60 — Add Mailbox beside CoreMsg; 44 — Apply document state from Node events; 51 — Stand the persist stack
**Trackers:** architecture

## Context

`CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly` both carry a caller, an `Ev`, and a reply. Both send `EventBody.Change` through `CoreEventDispatch.postEvent`.

- **Change** — `CoreMailboxActors.dispatchPostEvent` calls that function with `graphOnly` false.
- **Graph only** — `CoreMailboxEvents.dispatchPostGraphOnly` calls that function with `graphOnly` true.

Every `PostGraphOnly` caller mints `commandName` `"Parse"`:

- **Parse ops** — `ParseThread.postParseOps`
- **Parse file** — `Api.applyParseFile`
- **Load** — `LazyLoadReconciliationServer` through `GraphOnlyChangePost.postChunks`

`CoreMsg.PostEvent` also carries `Undo`, `Redo`, `ActorStart`, and `Cancel`. 65 — Log Undo and Redo as the target and 66 — Move Actor orders onto Order and Notice take those bodies.

## What to build

1. [ ] **Door** — Decision 4 **One Change**. `CoreMailboxApi.postEvents` and `postGraphOnly` place `Mailbox.PlaceOrder` of `Order.Change` of the op list. `Order.Change` has no graph-only flag. `Api.postEvents`, `RunAgentActor`, and `TestActor` use `postEvents`. The Parse callers in Context use `postGraphOnly`. Remove `CoreMsg.PostGraphOnly`. `CoreMsg.PostEvent` stays for the four bodies Context names.
2. [ ] **Reply** — The `PlaceOrder` reply is `Result<EventId, string>`. `Ok` carries the assigned `EventId`. `Api.postEvents` builds the HTTP change response from that id and from the state read it already does. The mailbox reply is not `CoreChangesAccepted`.
3. [ ] **Files** — `CoreEventDispatch` writes document files when `commandName` is not `Some "Parse"`. A `Change` whose `commandName` is `Some "Parse"` keeps the file skip `PostGraphOnly` uses today.
4. [ ] **NodeUnpersisted** — Decision 12 **NodeUnpersisted**. Add `Notice.NodeUnpersisted of nodeId: NodeId`. After a successful `Change` that writes document files, `CoreEventDispatch` posts one `Notice.NodeUnpersisted` for each document root of the ops, using `DocumentPartition.documentRootForNode`. `CoreMailboxLoad.applyNotice` sets that node `PersistState.Unpersisted` and appends one `Ev` whose body is that notice. A `Change` that keeps the file skip posts no `Notice.NodeUnpersisted`.

### Acceptance Criteria

1. [ ] **One order** — `postEvents and postGraphOnly place Order.Change`. Each reply is `Ok` of the new `EventId`. `CoreMsg` has no `PostGraphOnly`.
2. [ ] **Parse** — `postParseOps skips the document file`. The call completes. The log for that change has no `Notice.NodeUnpersisted`.
3. [ ] **Edit** — `an ordinary Change writes the document file`. The log has `Notice.NodeUnpersisted` of the document root, and that node is `PersistState.Unpersisted`.
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
- [65 — Log Undo and Redo as the target](65-log-undo-and-redo-as-the-target.md)
- [66 — Move Actor orders onto Order and Notice](66-move-actor-orders-onto-order-and-notice.md)
- [62 — Admit at the entry](62-admit-at-the-entry.md)
- [GLOSSARY](../../../GLOSSARY.md)
