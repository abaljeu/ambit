# 65 — Undo and Redo resolve their ops from the target

**Status:** `defined`
**Actual:** 4h
**Type:** coding
**Blocked by:** 60 — Add Mailbox beside CoreMsg
**Trackers:** architecture

## Context

Today's `EventBody.Undo` and `EventBody.Redo` each store `EventId * Op list`. `CoreMailboxActors.dispatchPostEvent` sends those bodies through `CoreEventDispatch.postEvent`. `ClientHistory.invertAs` and `CoreEventDispatch` fill the ops. Decision 1 **Logged as placed** in [Message shape](../cqrs-messages.md): `Order.Undo` and `Order.Redo` carry the target `EventId` only, and the client and the server resolve that target the same way. This ticket is separate from [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md) because the stored undo body loses its ops. [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md) leaves `Undo` and `Redo` on `CoreMsg.PostEvent`. Upload, import, parse, Load, undo, and persist stay working.

## What to build

Place undo and redo as `Order.Undo` and `Order.Redo` of the target `EventId`. Store that order. Resolve the target on the client and on the server. Keep apply of an already stored undo that still carries ops.

### 1. Change

1. [ ] **Order** — The undo and redo arm of `CoreMailboxActors.dispatchPostEvent` places `Order.Undo` or `Order.Redo` of the target `EventId`. It does not place `Order.Change`. The `PlaceOrder` reply is `Result<EventId, string>`.
2. [ ] **Log** — The stored body is that order and has no op list. Change `History.EventBody.Undo` and `History.EventBody.Redo` to `of target: EventId` so today's `Ev` can store them until [68 — Remove the CoreMsg case](68-remove-the-coremsg-case.md). Accept copies the target from the order. It does not fill ops. `EventJson` writes the target id. The decoder still reads an old payload that carries ops, and item 4 applies those ops. Update `MailboxLog.ofEv` and `MailboxLog.toEv` so both directions carry only the target.
3. [ ] **Resolve** — `ClientHistory` and `CoreEventDispatch` load the target event from the log. Undo uses the inverse of the target. Redo uses the target's forward ops. Both use that resolution. `ClientHistory.tryPeekUndoName` still returns the undo event's command name.
4. [ ] **Old events** — A stored undo or redo that still carries an op list applies that list.

### Acceptance Criteria

1. [ ] **New undo** — After a `Change`, an undo places `Order.Undo` of that `EventId`. The stored event has that target and no ops. The graph matches the inverse of the target.
2. [ ] **New redo** — A redo places `Order.Redo` of the undone `EventId`. The stored event has that target and no ops. The graph matches the target's forward ops.
3. [ ] **Same resolution** — For one target, `ClientHistory` and `CoreEventDispatch` produce the same op list.
4. [ ] **Old undo** — An event already stored as `Undo` of an id and an op list still applies that op list.
5. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [47 — Undo and redo set Unpersisted](47-undo-and-redo-set-unpersisted.md)
- [GLOSSARY](../../../GLOSSARY.md)

## Amendments

1. **Carried ops** — `EventBody.Undo` and `EventBody.Redo` are `of target: EventId * carried: Op list option`. A new event uses `None`. The shape `of target: EventId` cannot keep an old op list through `EventJson`. `EventJson` omits `ops` when `carried` is `None` and reads a present `ops` field as `Some`, including an empty list.
2. **Redo target** — `Order.Redo` targets the undone event, the event `Order.Undo` inverted. `CoreEventDispatch.acceptUndoRedo` applies that event's forward ops. The redo does not target the undo event's own id.
3. **ClientHistory keeps events** — `ClientHistory` keeps an action that left the undo or redo stack so a later lookup still finds it. Local events share `EventId.zero`, so `ClientHistory` links the produced submission to the source submission and `ClientHistory.approve` stamps that target.
4. **Client event log** — `SyncLogic` appends applied events to `ClientSyncState.eventLog` so a later target-only undo can resolve. A client that applied a change before that log existed cannot resolve a later target-only undo of that change.
5. **Accept stores no ops** — `CoreEventDispatch.acceptUndoRedo` ignores ops on a newly posted undo or redo. It resolves the target, applies a temporary copy that carries the resolved ops, and commits the target-only event. `CoreMailboxActors.dispatchPostEvent` places `Order.Undo` or `Order.Redo` and replies with that stored event.
6. **PlaceOrder replay** — `CoreEventDispatch.acceptUndoRedo` returns the stored event for a repeated submission. A new post whose `EventId` is not zero is refused.
7. **Pending copy** — `SyncLogic.applyInverse` keeps the resolved ops on the pending event so `SyncPlanner.restorePending` and rewind can apply them. `SyncBatch.toWireBatch` strips those ops. `SyncPlanner.tryStartSubmit` holds an undo or redo whose target is still `EventId.zero` until `ClientHistory.retarget` fills that target from the confirmed change.
8. **Boot fold** — `BootCache.foldLog` resolves a target-only undo or redo through `UndoTarget` against the delta it folds.

## Time

- 2026-10-20 4h (from chat) — undo and redo resolve ops from the target
