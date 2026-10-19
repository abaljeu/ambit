# 68 — Remove the CoreMsg case

**Status:** `defined`
**Type:** coding
**Blocked by:** 61 — Move Session off the core mailbox; 63 — Move queries to Ask; 64 — Merge PostEvent and PostGraphOnly into Change; 65 — Log Undo and Redo as the target; 66 — Move Actor orders onto Order and Notice; 67 — Log Load as Order.Load
**Trackers:** architecture

## Context

After the move tickets, every order is a `PlaceOrder` and every query is an `Ask`. `Mailbox` still has `CoreMsg of CoreMsg`, and `History.Ev` still has `id`, `submissionId`, `authority`, `commandName`, and `body`. Today's `EventBody` is `Change`, `Undo`, `Redo`, `ActorStart`, `ActorStop`, and `Cancel`. [Message shape](../cqrs-messages.md) §4 **Collapse** removes the `CoreMsg` case and the `CoreMsg` type. `Ev` becomes `{ id; body }`. `submissionId` leaves `Ev`. `authority`, `orderId`, and `commandName` stay on `PlacedOrder`. A `Notice` has none of them. Upload, import, parse, Load, undo, and persist stay working.

## What to build

Remove `CoreMsg`. Make the `MailboxLog` event the `History` event. Read the order envelope from `PlacedOrder`.

### 1. Collapse

1. [ ] **CoreMsg** — Remove the `Mailbox.CoreMsg` case and the `CoreMsg` type. `Mailbox` is `PlaceOrder` and `Ask`. `CoreQueue` stays `Public of Mailbox | Notice of Notice`. Dispatch and the startup scan have no `CoreMsg` arm.
2. [ ] **Ev** — `History.Ev` becomes `{ id: EventId; body: EventBody }`. Remove `submissionId`, `authority`, and `commandName` from `Ev`. `EventBody` becomes `Order of PlacedOrder | Notice of Notice`. Remove `Change`, `Undo`, `Redo`, `ActorStart`, `ActorStop`, and `Cancel` from `EventBody`. Move the `MailboxLog` definitions into `History.fs` and delete `MailboxLog` together with `ofEv` and `toEv`.
3. [ ] **Envelope** — `EventJson` writes `authority`, `orderId`, and `commandName` from `PlacedOrder` when the body is `EventBody.Order`. A `Notice` body has none of those fields. `EventLog` dedupes an order by `PlacedOrder.orderId`. `ClientHistory.tryPeekUndoName` reads `commandName` from the undo `PlacedOrder`.
4. [ ] **Callers** — `CoreEventDispatch`, `CoreMailboxActors`, `DbAgent`, `FileAgent`, `SyncLogic`, `BootCache`, `ResidentProjection`, `ClientHistory`, `Controller`, and `App` read `authority`, the order id, and `commandName` from `PlacedOrder` when the body is an order.

### Acceptance Criteria

1. [ ] **Types** — The server has no `CoreMsg` type and `Mailbox` has no `CoreMsg` case. `Ev` has `id` and `body` only. `EventBody` is `Order` or `Notice`.
2. [ ] **Order event** — A stored `Change` has `authority`, `orderId`, and `commandName` on its `PlacedOrder`. `EventJson` round-trips those three fields.
3. [ ] **Notice event** — A stored `Notice.NodeUnpersisted` has `id` and that notice. The JSON has no `authority`, `orderId`, or `commandName`.
4. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [59 — Rename InMsg to Notice](59-rename-inmsg-to-notice.md)
- [61 — Move Session off the core mailbox](61-move-session-off-the-core-mailbox.md)
- [63 — Move queries to Ask](63-move-queries-to-ask.md)
- [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [65 — Log Undo and Redo as the target](65-log-undo-and-redo-as-the-target.md)
- [66 — Move Actor orders onto Order and Notice](66-move-actor-orders-onto-order-and-notice.md)
- [67 — Log Load as Order.Load](67-log-load-as-order-load.md)
- [GLOSSARY](../../../GLOSSARY.md)
