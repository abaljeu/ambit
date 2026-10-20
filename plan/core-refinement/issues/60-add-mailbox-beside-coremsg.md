# 60 — Add Mailbox beside CoreMsg

**Status:** `defined`
**Type:** coding
**Actual:** 2h
**Blocked by:** 59 — Rename InMsg to Notice
**Trackers:** architecture

## Context

[59 — Rename InMsg to Notice](59-rename-inmsg-to-notice.md) makes `CoreQueue` `Public of CoreMsg | Notice of Notice`. This ticket adds `Mailbox` as the public sum and puts today's `CoreMsg` on it as one case. `PlaceOrder` and `Ask` sit beside that case. `Notice` is not a `Mailbox` case. `Order`, `PlacedOrder`, the new `EventBody`, and `Ev = { id; body }` are added beside today's `History.fs` types, with mapping functions. Callers stay on today's `Ev`. `Query` stays on the server because each case carries an `AsyncReplyChannel`. Upload, import, parse, Load, undo, and persist stay working.

## What to build

Add the public `Mailbox` and the Shared order log beside the types callers already use. Wire `CoreQueue.Public` to `Mailbox`. Leave every caller on today's `CoreMsg` doors and on `History.Ev`.

### 1. Expand

1. [ ] **Mailbox** — In `CoreMsg.fs`, add `Mailbox = CoreMsg of CoreMsg | PlaceOrder of caller: Caller * orderId: System.Guid * commandName: string option * order: Order * reply: AsyncReplyChannel<Result<EventId, string>> | Ask of Query`. `Mailbox` has no `Notice` case.
2. [ ] **Queue** — `CoreQueue.Public` carries `Mailbox`. `CoreMailboxApi.addCoreMsg` posts `Mailbox.CoreMsg`. `CoreMailboxLoad` runs today's `CoreMsg` handler for that case. The startup scan still serves a read that arrives as `Mailbox.CoreMsg`.
3. [ ] **Shared log** — Add `src/Shared/MailboxLog.fs`, module `MailboxLog`. It holds `Order` with `Change`, `Undo of target: EventId`, `Redo of target: EventId`, `StartActor of ActorStart`, `StartPeerActor of PeerActorName * ActorStart`, `LoadSave of LoadSavePath * PeerActorName * LoadSaveCommandRequest`, `Load of NodeId`, and `Cancel of NodeId`. It holds `PlacedOrder` of `orderId`, `authority`, `commandName: string option`, and `order`. It holds `EventBody = Order of PlacedOrder | Notice of Notice` and `Ev = { id: EventId; body: EventBody }`. Move `Notice` from the server file into this module so the body can hold it. The five cases and payloads from [59 — Rename InMsg to Notice](59-rename-inmsg-to-notice.md) stay.
4. [ ] **Query** — Add server-only `Query` beside `Mailbox`. The cases are `State of AsyncReplyChannel<Result<State, string>>`, `LastEventId of AsyncReplyChannel<Result<EventId, string>>`, and `EventsSince of after: EventId * AsyncReplyChannel<Result<EventLog, string>>`. No caller posts `Ask` or `PlaceOrder` in this ticket. The `PlaceOrder` arm replies `Error "unsupported"`. The `Ask` arm replies `Error "unsupported"` on that `Query` channel.
5. [ ] **PeerActorName** — Move `PeerActorName` from `CoreActorNames.fs` to Shared. `ActorName` stays in `CoreActorNames.fs`. `Order.StartPeerActor` and `Order.LoadSave` use the Shared `PeerActorName`.
6. [ ] **Mapping** — `MailboxLog.ofEv` and `MailboxLog.toEv` return `Result`. The post path leaves both functions unused. `submissionId` maps to `orderId`. `authority` maps onto `PlacedOrder`. `commandName` `""` maps to `None`, and any other string maps to `Some`. `toEv` maps `None` back to `""`. Mapped cases: `Change` keeps the ops; `Undo` and `Redo` keep the target, `ofEv` drops the ops, and `toEv` supplies an empty op list; `ActorStart` maps to `Order.StartActor`; `Cancel` maps to `Order.Cancel`. Unused cases return `Error "unsupported"`: `ofEv` on `ActorStop` (`Notice` has no `ActorStop` case; [66 — Move Actor orders onto Order and Notice](66-move-actor-orders-onto-order-and-notice.md)); `toEv` on a `Notice` body, and on `StartPeerActor`, `LoadSave`, and `Load` (today's `History.Ev` has no body for those orders).

### Acceptance Criteria

1. [ ] **Shape** — `Mailbox` is `CoreMsg`, `PlaceOrder`, and `Ask`. It has no `Notice` case. `CoreQueue` is `Public of Mailbox | Notice of Notice`.
2. [ ] **Same path** — `CoreMailboxApi.postEvent` still runs today's `PostEvent` handler through `Mailbox.CoreMsg`.
3. [ ] **Round trip** — `ofEv` then `toEv` on a `Change` keeps the ops, the authority, the order id, and the command name. `""` becomes `None` and returns as `""`.
4. [ ] **Callers** — Route and Actor callers still build `History.Ev` and still post `CoreMsg`. `History.Ev` still has `submissionId`, `authority`, `commandName`, and `body`.
5. [ ] **PeerActorName** — Shared declares `PeerActorName`. `CoreActorNames.fs` does not. `Order.StartPeerActor` carries it.
6. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## Amendments

1. **Mapping result** — §6 **Mapping** states the `Result`, the mapped cases, and the unused cases. `ofEv` returns `Error "unsupported"` on `ActorStop`. `toEv` returns `Error "unsupported"` on a `Notice` body, and on `StartPeerActor`, `LoadSave`, and `Load`.
2. **Type home** — `Order`, `Notice`, and `PlacedOrder` are Shared types in [MailboxLog.fs](../../../src/Shared/MailboxLog.fs). They sit beside module `MailboxLog`. `EventBody` and `Ev` stay in the module. An `EventBody` case named `Order` or `Notice` hides a type of that name in a pattern when that type is inside the module.
3. **PeerActorName** — `PeerActorName` moves to Shared. `ActorStart`, `ActorResult`, `LoadSavePath`, and `LoadSaveCommandRequest` are already Shared.
4. **Visibility** — `Mailbox` and `Query` are internal, as `CoreMsg` is internal.

## Time

- 2026-10-19 2h — expand Mailbox beside CoreMsg (from chat)

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [59 — Rename InMsg to Notice](59-rename-inmsg-to-notice.md)
- [GLOSSARY](../../../GLOSSARY.md)
