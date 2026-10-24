# 64 — Place Edit changes as Order.Change

**Status:** `defined`
**Type:** coding
**Blocked by:** 60 — Add Mailbox beside CoreMsg; 44 — Apply document state from Node events; 71 — Persist thread takes from the stack
**Trackers:** architecture

## Context

`CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly` both carry a caller, an `Ev`, and a reply. Both send `EventBody.Change` through `CoreEventDispatch.postEvent`.

- **Change** — `CoreMailboxActors.dispatchPostEvent` calls that function with `graphOnly` false.
- **Graph only** — `CoreMailboxEvents.dispatchPostGraphOnly` calls that function with `graphOnly` true.

Decision 4 **One Change** and decision 18 **ChangeOrigin** in [Message shape](../cqrs-messages.md): the two doors become `Order.Change` of `ops: Op list * origin: ChangeOrigin`. This ticket adds the origin and moves the origin `Edit` door, `CoreMailboxApi.postEvents`. [102 — Place Parse changes as Order.Change](102-place-parse-changes-as-order-change.md) moves the origin `Parse` callers and removes `CoreMsg.PostGraphOnly`.

Today, after a Change, Undo or Redo, `CoreEventDispatch.pushChangeSpecials` marks each owning special node Unpersisted at the event id, logs `Notice.NodePersisting` for it, and pushes a persist request. This ticket keeps that path. It does not add `Notice.NodeUnpersisted`.

`CoreMsg.PostEvent` also carries `Undo`, `Redo`, `ActorStart`, and `Cancel`. [65 — Place Undo and Redo as orders](65-undo-and-redo-resolve-their-ops-from-the-target.md) and [80 — Place Actor orders through PlaceOrder](80-place-actor-orders-through-placeorder.md) take those bodies. Upload, import, parse, Load, undo, and persist stay working.

## What to build

1. [ ] **Origin** — Decision 18 **ChangeOrigin**. `ChangeOrigin` is `Edit | Parse` in `src/Shared/MailboxLog.fs` beside `Order`. `Order.Change` becomes `ops: Op list * origin: ChangeOrigin`. `origin` exists only on the placed order. `MailboxLog.ofEv` maps a logged Change to origin `Edit`. The logged Change holds the op list.
2. [ ] **Edit door** — `CoreMailboxApi.postEvents` places `Mailbox.PlaceOrder` of `Order.Change` with origin `Edit` for a `Change` body. `Api.postEvents`, `RunAgentActor`, and `TestActor` keep calling `postEvents`. `CoreEventDispatch` reads `origin` and writes document files for origin `Edit` the way `PostEvent` does today. `CoreMsg.PostEvent` stays for the four bodies Context names. `CoreMsg.PostGraphOnly` stays until 102.
3. [ ] **Reply** — The `PlaceOrder` reply is `Result<EventId, string>`. `Ok` carries the assigned `EventId`. `Api.postEvents` builds the HTTP change response from that id and from the state read it already does. The mailbox reply is not `CoreChangesAccepted`.

### Acceptance Criteria

1. [ ] **Edit order** — `postEvents places Order.Change with origin Edit`. A Change through `postEvents` places `Order.Change` with origin `Edit`. The reply is `Ok` of the new `EventId`.
2. [ ] **Edit** — `an ordinary Change writes the document file`. A placed `Change` of origin `Edit` writes the document file the way `PostEvent` does today. The owning special node is Unpersisted at the Change's event id and a persist request is pushed.
3. [ ] **Other bodies** — `the four bodies Context names stay on PostEvent`. A post of each of those bodies still uses `CoreMsg.PostEvent`.
4. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [102 — Place Parse changes as Order.Change](102-place-parse-changes-as-order-change.md)
- [65 — Place Undo and Redo as orders](65-undo-and-redo-resolve-their-ops-from-the-target.md)
- [80 — Place Actor orders through PlaceOrder](80-place-actor-orders-through-placeorder.md)
- [62 — Admit at the entry](62-admit-at-the-entry.md)
- [GLOSSARY](../../../GLOSSARY.md)
