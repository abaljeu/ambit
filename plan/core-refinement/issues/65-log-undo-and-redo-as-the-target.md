# 65 — Log Undo and Redo as the target

**Status:** `defined`
**Type:** coding
**Blocked by:** 60 — Add Mailbox beside CoreMsg
**Trackers:** architecture

## Context

`EventBody.Undo` and `EventBody.Redo` each store an `EventId` and an Op list. `CoreMailboxActors.dispatchPostEvent` sends those bodies through `CoreEventDispatch.postEvent`. `ClientHistory.invertAs` and `CoreEventDispatch` fill that Op list. 64 — Merge PostEvent and PostGraphOnly into Change leaves `Undo` and `Redo` on `CoreMsg.PostEvent`.

## What to build

1. [ ] **Order** — The Undo and Redo arm of `CoreMailboxActors.dispatchPostEvent` places `Order.Undo` or `Order.Redo` of the target `EventId`. It does not place `Order.Change`. The `PlaceOrder` reply is `Result<EventId, string>`.
2. [ ] **Log** — Decision 1 **Logged as placed** in Message shape. The stored body is the order from item 1 and has no Op list. `History.EventBody.Undo` and `History.EventBody.Redo` become `of target: EventId`, so an `Ev` can store them until 68 — Remove the CoreMsg case. Accept copies the target from the order. It does not fill the Op list. `EventJson` writes the target id. The decoder still reads an old payload that carries an Op list. Item 4 applies that list. `MailboxLog.ofEv` and `MailboxLog.toEv` carry only the target.
3. [ ] **Resolve** — `ClientHistory` and `CoreEventDispatch` load the target event from the log. Undo uses the inverse of the target. Redo uses the target's forward Op list. The client and the server use that same resolution. `ClientHistory.tryPeekUndoName` still returns the Undo event's command name.
4. [ ] **Old events** — A stored Undo or Redo that still carries an Op list applies that list.

### Acceptance Criteria

1. [ ] **New undo** — `new undo stores the target`. After a Change, the stored event has that target and no ops. The graph matches the inverse of the target.
2. [ ] **New redo** — `new redo stores the target`. The stored event has the undone `EventId` and no ops. The graph matches the target's forward Op list.
3. [ ] **Same resolution** — `client and server resolve one target`. For one target, `ClientHistory` and `CoreEventDispatch` produce the same Op list.
4. [ ] **Old undo** — `old undo applies its op list`. An event already stored as `Undo` of an id and an Op list still applies that list.
5. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [68 — Remove the CoreMsg case](68-remove-the-coremsg-case.md)
- [47 — Undo and redo set Unpersisted](47-undo-and-redo-set-unpersisted.md)
- [GLOSSARY](../../../GLOSSARY.md)
