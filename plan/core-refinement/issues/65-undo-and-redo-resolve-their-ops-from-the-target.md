# 65 — Place Undo and Redo as orders

**Status:** `defined`
**Type:** coding
**Blocked by:** 60 — Add Mailbox beside CoreMsg; 64 — Merge PostEvent and PostGraphOnly into Change
**Trackers:** architecture

## Context

`EventBody.Undo` and `EventBody.Redo` each store a target `EventId` and an Op list. `CoreMailboxActors.dispatchPostEvent` sends those bodies through `CoreEventDispatch.postEvent`. When the client sends an empty Op list, `CoreEventDispatch.completeAction` fills it with the inverse ops of the target. `Order.Undo` and `Order.Redo` in `src/Shared/MailboxLog.fs` carry only the target. `MailboxLog.historyBody` maps them to an empty Op list.

## What to build

1. [ ] **Order shape** — `Order.Undo` and `Order.Redo` are `of target: EventId * ops: Op list`. `MailboxLog.ofEv` and `MailboxLog.toEv` carry the target and the Op list both ways.
2. [ ] **Order** — The Undo and Redo arm of `CoreMailboxActors.dispatchPostEvent` places `Order.Undo` or `Order.Redo` of the target and the Op list through `Mailbox.PlaceOrder`. The `PlaceOrder` reply is `Result<EventId, string>`.
3. [ ] **Fill** — Accept fills an empty Op list from the inverse ops of the target, as `completeAction` does. The stored event holds the target and the filled Op list.
4. [ ] **Remove** — `CoreMsg.PostEvent` carries no Undo or Redo body.

### Acceptance Criteria

1. [ ] **Undo** — `undo places Order.Undo`. After a Change, an Undo of it replies `Ok` of a new `EventId`. The stored event holds the target and the inverse ops. The graph matches the state before the Change.
2. [ ] **Redo** — `redo places Order.Redo`. The stored event holds the undone `EventId` and its ops. The graph matches the state after the Change.
3. [ ] **Round trip** — `Undo and Redo keep their ops through MailboxLog`. `MailboxLog.toEv` of `MailboxLog.ofEv` returns the same target and Op list.
4. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [68 — Remove the CoreMsg case](68-remove-the-coremsg-case.md)
- [47 — Undo and redo set Unpersisted](47-undo-and-redo-set-unpersisted.md)
- [GLOSSARY](../../../GLOSSARY.md)
