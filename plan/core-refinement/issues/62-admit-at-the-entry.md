# 62 — Admit at the entry

**Status:** `defined`
**Type:** coding
**Actual:** 15m
**Blocked by:** 60 — Add Mailbox beside CoreMsg
**Trackers:** architecture

## Context

`CoreEventDispatch.postEvent` calls `context.admit`, and that function is `CoreMailboxEvents.admitCaller`. The same check runs in `CoreMailboxActors.prepareActorStart`, `dispatchActorStop`, `runCancel`, `dispatchRecordSearchStart`, `dispatchRecordSearchStop`, and `CoreMailboxLoad.dispatchLoad`. `RouteRegistration.withBrowserChanges` already calls `CoreMailboxApi.isAdmitted` before the route body. Decision 9 **Admission at the entry** in [Message shape](../cqrs-messages.md): the route checks a logged-in user before `PlaceOrder`, an Actor is admitted when it starts, and the core reads authority. `admitCaller` inside `postEvent` is the call that leaves. Upload, import, parse, Load, undo, and persist stay working.

## What to build

Check a logged-in user at the route, and admit an Actor in the start handler. `postEvent` reads `caller.authority` and does not admit.

### 1. Sequence

1. [ ] **Work order** — The order is [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md), then [63 — Move queries to Ask](63-move-queries-to-ask.md), then this ticket, then [61 — Move Session off the core mailbox](61-move-session-off-the-core-mailbox.md). This ticket is blocked by [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md) only. That ticket establishes `Mailbox.CoreMsg`, `PlaceOrder`, and `Ask` beside one another.
2. [ ] **Session handoff** — [61 — Move Session off the core mailbox](61-move-session-off-the-core-mailbox.md) is blocked by this ticket and by [63 — Move queries to Ask](63-move-queries-to-ask.md). Its route admission and its logout use the entry rule from this ticket. Logout stops admitting and queues `CoreQueue.DropAuthority`. The core removes the authority when it processes that case.
3. [ ] **Actor handoff** — [66 — Move Actor orders onto Order and Notice](66-move-actor-orders-onto-order-and-notice.md) is also blocked by this ticket. Complete Actor admission at start before that ticket moves Actor posts to the new order door.
4. [ ] **Collapse gate** — [68 — Remove the CoreMsg case](68-remove-the-coremsg-case.md) is also blocked by this ticket. It collapses the expansion seam after this ticket is complete. Every remaining order door receives only a logged-in user or an Actor admitted at start.

### 2. Change

1. [x] **Route** — `RouteRegistration.withBrowserChanges` checks that the cookie caller is logged in before the route body places an order. A missing cookie gets 401. A caller who is not logged in gets 401. Keep this check in front of today's `PostEvent` and `PostGraphOnly` throughout [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md), then keep the same check in front of `PlaceOrder`.
2. [x] **Actor** — `CoreMailboxActors.prepareActorStart` admits `Authority "Actor"` when the start runs, using the pool live check `admitCaller` uses today. Establish this permission before removing any repeated core check. A later post from that Actor reads the established authority and does not admit again.
3. [x] **Core** — After the matching route or Actor-start check is active, remove `admitCaller` from `CoreEventDispatch.postEvent`. `prepare` still copies `caller.authority` onto the stored event. Remove `admitCaller` from `dispatchActorStop`, `runCancel`, `dispatchRecordSearchStart`, `dispatchRecordSearchStop`, and `dispatchLoad` only after each path has an established entry permission. Those handlers read the caller on the message.
4. [x] **Safe migration** — Keep the old inner admission check while its replacement entry check is being added. At no intermediate step may a user route or Actor reach `PostEvent`, `PostGraphOnly`, `PlaceOrder`, or a successor order door without established permission. Duplicate checks are allowed during the handoff; an unchecked order door is not.

### Acceptance Criteria

1. [ ] **Route** — `withBrowserChanges` returns 401 for a missing cookie and for a caller who is not logged in. It does not call `postEvent` on that path.
2. [ ] **postEvent** — `CoreEventDispatch.postEvent` has no `admitCaller` call. A direct post stores the event and the stored `authority` is the caller's `authority`.
3. [ ] **Actor start** — `prepareActorStart` rejects an Actor whose secret is not live. After a live start, `postEvent` from that Actor stores the event and does not call `admitCaller`.
4. [ ] **Door switch** — Route admission remains in force while [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md) changes the route post to `PlaceOrder`. Actor start admission is in force before [66 — Move Actor orders onto Order and Notice](66-move-actor-orders-onto-order-and-notice.md) changes the Actor post. No test step exposes an order door without one of those entry checks.
5. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## Amendments

1. **Status** — Status stays `defined`. Acceptance stays unchecked.
2. **Docs** — Achieved admission claims in [HTTP contract](../../../doc/current/http-contract.md) and [Mailbox](../../../doc/current/mailbox.md) are plain statements. The old `[o]` admission lines are gone. `PlaceOrder` claims stay `[ ]`.
3. **Name** — `prepare` names the event copy `withCallerAuthority`. That copy reads `caller.authority` and does not admit.
4. **Search record** — `RecordSearchStart` and `RecordSearchStop` call `CoreEventDispatch` from the mailbox match. Those two one-line forwarders are gone.

## Time

- 2026-10-20 15m — moved admission to the browser route and Actor start, and removed the repeated core checks (from the earlier implementation).

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [61 — Move Session off the core mailbox](61-move-session-off-the-core-mailbox.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [63 — Move queries to Ask](63-move-queries-to-ask.md)
- [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [66 — Move Actor orders onto Order and Notice](66-move-actor-orders-onto-order-and-notice.md)
- [68 — Remove the CoreMsg case](68-remove-the-coremsg-case.md)
- [GLOSSARY](../../../GLOSSARY.md)
