# 66 — Move Actor orders onto Order and Notice

**Status:** `cancelled`
**Type:** coding
**Blocked by:** None — split into [79](79-place-git-save-as-order-save.md), [80](80-place-actor-orders-through-placeorder.md), [81](81-log-actor-and-search-stops-as-notices.md)
**Trackers:** architecture

## Context

`CoreMsg` carries these cases:

- `StartActor`
- `RecordSearchStart`
- `StartPeerActor`
- `StartLoadSaveCommand`
- `CancelActor`
- `ActorStop`
- `RecordSearchStop`

`CoreMailboxActors.dispatchStartActor` starts the pool. `dispatchRecordSearchStart` appends `ActorStart` and does not run the walk. `RouteRegistration.searchDoor` calls `recordSearchStart` and `recordSearchStop`. `dispatchRecordSearchStop` stores `ActorStop` of the root id and `ActorSucceeded`. `dispatchActorStop` and `dispatchCancelActor` reply `Ok ()` and append nothing when no Actor runs for the caller or the focus. A start that runs no Actor replies `Ok ()` and appends nothing. A client `PostEvent` of `EventBody.ActorStart` or `EventBody.Cancel` is handled in `dispatchPostEvent`. 60 — Add Mailbox beside CoreMsg moves `PeerActorName` onto Shared so `Order` can carry it.

This ticket is decision 19 **One case each** in [Message shape](../cqrs-messages.md). Each case Context lists moves to its own case. `ActorStop` and `RecordSearchStop` become `Notice` cases. The others become `Order` cases. Each handler is the dispatcher that case uses today.

## What to build

Do these steps in order. Finish each step before starting the next.

1. [ ] **Order cases** — Add `RecordSearchStart of ActorStart` to `Order`. `MailboxLog.ofEv` and `MailboxLog.toEv` map `EventBody.ActorStart` and `EventBody.ActorStop` both ways. Files: `src/Shared/MailboxLog.fs` (where `Order` is defined), `tests/Shared.Tests/MailboxLogTests.fs`. Done when it compiles and `MailboxLogTests` pass.
2. [ ] **Place and handle** — Each `CoreMailboxApi` function places its order through `Mailbox.PlaceOrder`: `startActor` places `Order.StartActor`, `recordSearchStart` `Order.RecordSearchStart`, `startPeerActor` `Order.StartPeerActor`, `startLoadSaveCommand` `Order.LoadSave`, `cancelByFocus` `Order.Cancel`, and a client `EventBody.ActorStart` or `EventBody.Cancel` in `dispatchPostEvent` places `Order.StartActor` or `Order.Cancel`. The `PlaceOrder` arm of `CoreMailboxLoad.dispatchMailbox` sends each order to the handler its `CoreMsg` case uses today in `CoreMailboxLoad.runMsg` (`dispatchStartActor`, `dispatchStartPeerActor`, `dispatchStartLoadSaveCommand`, `CoreEventDispatch.actorStart` for a search start, `dispatchCancelActor`) and checks authority as `PlaceOrder` does today. Every accepted order appends an `Ev`: where today's handler appends one, that `Ev` is the order's entry; a start that runs no Actor appends `ActorStart` of the request, then `ActorStop` of its focus and `ActorSucceeded`. `PlaceOrder` replies `Ok` of the `EventId` of the order's first appended `Ev`, and each `CoreMailboxApi` function keeps its return type. Add acceptance tests 1, 3 and 6. Files: `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreMailboxActors.fs`, `src/Server/Core/CoreEventDispatch.fs`, `tests/Server.Tests/CoreMsgActorCasesTests.fs`. Done when `startActor places Order.StartActor`, `startPeerActor and startLoadSaveCommand place their orders` and `each accepted Actor order appends an Ev` pass.
3. [ ] **Stops** — Add `Notice.ActorStopped of focusId: NodeId * result: ActorResult` and `Notice.SearchStopped of rootId: NodeId`, with `EventJson` writing and reading both. `CoreMailboxApi.actorStop` posts `Notice.ActorStopped` and `recordSearchStop` posts `Notice.SearchStopped` with `postNotice`; the logged notice is the stop entry. The `ActorStopped` handler in `CoreMailboxLoad.applyNotice` finishes the pool entry as `dispatchActorStop` does. The `Order.Cancel` handler appends `Notice.ActorStopped` of the focus and `ActorCancelled` as its stop entry and finishes the pool entry as `dispatchCancelActor` does; a cancel that finds no running Actor replies `Error` of "no running actor". The browser reads an Actor result from `Notice.ActorStopped` and a search stop from `Notice.SearchStopped` where it reads `EventBody.ActorStop` today. Add acceptance tests 2, 4 and 5. Files: `src/Shared/History.fs` (where `Notice` is defined), `src/Shared/EventJson.fs`, `src/Shared/ActorLive.fs`, `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreMailboxActors.fs`, `tests/Server.Tests/CoreMsgActorCasesTests.fs`. Done when `recordSearchStart places its order and recordSearchStop posts SearchStopped`, `cancelByFocus places Order.Cancel` and `actorStop posts Notice.ActorStopped` pass.
4. [ ] **Remove** — Remove `StartActor`, `RecordSearchStart`, `StartPeerActor`, `StartLoadSaveCommand`, `CancelActor`, `ActorStop` and `RecordSearchStop` from `CoreMsg`, with their arms. Files: `src/Server/Core/CoreMsg.fs` (where `CoreMsg` is defined), `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreMailboxEvents.fs`, and each test that names those cases. Done when it compiles, `CoreMsgActorCasesTests` pass, and no file under `src/` or `tests/` names `RecordSearchStart (` or `CancelActor (` as a `CoreMsg` case.

### Acceptance Criteria

1. [ ] **Start** — `startActor places Order.StartActor`. The pool starts the Actor, and the reply is `Ok` of the `ActorStart` entry's `EventId`.
2. [ ] **Search** — `recordSearchStart places its order and recordSearchStop posts SearchStopped`. The start does not call `pool.startActor`. The log holds the start, then `Notice.SearchStopped` of the root id.
3. [ ] **Peer and load-save** — `startPeerActor and startLoadSaveCommand place their orders`. `Order.StartPeerActor` and `Order.LoadSave` carry the `PeerActorName` the caller passes.
4. [ ] **Cancel** — `cancelByFocus places Order.Cancel`. The Actor for that focus finishes. The log holds `Notice.ActorStopped` of the focus and `ActorCancelled`. The reply is `Ok` of that entry's `EventId`.
5. [ ] **Stop** — `actorStop posts Notice.ActorStopped`. The log holds `Notice.ActorStopped` of the focus and the result, and the pool entry is finished.
6. [ ] **Every order logs** — `each accepted Actor order appends an Ev`. A start that runs no Actor logs its start and its stop.
7. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [62 — Admit at the entry](62-admit-at-the-entry.md)
- [63 — Move queries to Ask](63-move-queries-to-ask.md)
- [64 — Place Edit changes as Order.Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [79 — Place git Save as Order.Save](79-place-git-save-as-order-save.md)
- [80 — Place Actor orders through PlaceOrder](80-place-actor-orders-through-placeorder.md)
- [81 — Log Actor and search stops as Notices](81-log-actor-and-search-stops-as-notices.md)
- [GLOSSARY](../../../GLOSSARY.md)
