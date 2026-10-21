# 66 — Move Actor orders onto Order and Notice

**Status:** `coded`
**Type:** coding
**Blocked by:** 60 — Add Mailbox beside CoreMsg; 63 — Move queries to Ask; 62 — Admit at the entry
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

This ticket is decision 19 **One case each** in [Message shape](../cqrs-messages.md). `ActorStop` is an `Order`. `RecordSearchStop` is `Notice.SearchStopped`. The other cases are `Order` cases. Each handler is the dispatcher that case uses today.

## What to build

Do these steps in order. Finish each step before starting the next.

1. [x] **Order cases** — Add `RecordSearchStart of ActorStart` to `Order`. `MailboxLog.ofEv` and `MailboxLog.toEv` map `EventBody.ActorStart` and `EventBody.ActorStop` both ways. Files: `src/Shared/MailboxLog.fs` (where `Order` is defined), `tests/Shared.Tests/MailboxLogTests.fs`. Done when it compiles and `MailboxLogTests` pass.
2. [x] **Place and handle** — Each `CoreMailboxApi` function places its order through `Mailbox.PlaceOrder`: `startActor` places `Order.StartActor`, `recordSearchStart` `Order.RecordSearchStart`, `startPeerActor` `Order.StartPeerActor`, `startLoadSaveCommand` `Order.LoadSave`, `cancelByFocus` `Order.Cancel`, and a client `EventBody.ActorStart` or `EventBody.Cancel` in `dispatchPostEvent` places `Order.StartActor` or `Order.Cancel`. The `PlaceOrder` arm of `CoreMailboxLoad.dispatchMailbox` sends each order to the handler its `CoreMsg` case uses today in `CoreMailboxLoad.runMsg` (`dispatchStartActor`, `dispatchStartPeerActor`, `dispatchStartLoadSaveCommand`, `CoreEventDispatch.actorStart` for a search start, `dispatchCancelActor`) and checks authority as `PlaceOrder` does today. `CoreDoor.saveNode` places `Order.Save` of the node. The `PlaceOrder` arm appends one `Ev` of `EventBody.Save` of that node and replies `Ok` of that `EventId`. `EventJson` writes and reads `EventBody.Save`. `MailboxLog.ofEv` and `MailboxLog.toEv` map `Order.Save` and `EventBody.Save` both ways. Undo skips `EventBody.Save` beside `EventBody.Load`. git Save places `Order.Save` of the Workspace, then runs the push, and returns `Ok` of that `EventId`. A push error returns `Error`. Every accepted order appends an `Ev`: where today's handler appends one, that `Ev` is the order's entry; a start that runs no Actor appends `ActorStart` of the request, then `ActorStop` of its focus and `ActorSucceeded`. `PlaceOrder` replies `Ok` of the `EventId` of the order's first appended `Ev`, and each `CoreMailboxApi` function keeps its return type. Add acceptance tests 1, 3 and 6. Files: `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreMailboxActors.fs`, `src/Server/Core/CoreEventDispatch.fs`, `tests/Server.Tests/CoreMsgActorCasesTests.fs`. Done when `startActor places Order.StartActor`, `startPeerActor and startLoadSaveCommand place their orders` and `each accepted Actor order appends an Ev` pass.
3. [x] **Stops** — `Notice.ActorStopped` carries `focusId` and `result`. `Notice.SearchStopped` carries `rootId`. `EventJson` writes and reads both. `Order.ActorStop` carries the caller id and the `ActorResult`. `CoreMailboxApi.actorStop` places `Order.ActorStop` through `Mailbox.PlaceOrder`. The handler finds the live focus for that caller, appends one `Ev` of `EventBody.ActorStop` of the focus and the result, finishes the pool entry, and replies `Ok` of that `EventId`. A caller with no running Actor gets `Error` of "no running actor" and the log gains no entry. `recordSearchStop` posts `Notice.SearchStopped`. The `Order.Cancel` handler appends `Notice.ActorStopped` of the focus and `ActorCancelled` and finishes the pool entry. The browser reads an Actor result from `EventBody.ActorStop` and from `Notice.ActorStopped`, and a search stop from `Notice.SearchStopped`. Add acceptance tests 2, 4 and 5. Files: `src/Shared/History.fs` (where `Notice` is defined), `src/Shared/MailboxLog.fs` (where `Order` is defined), `src/Shared/EventJson.fs`, `src/Shared/ActorLive.fs`, `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreMailboxActors.fs`, `tests/Server.Tests/CoreMsgActorCasesTests.fs`. Done when `recordSearchStart places its order and recordSearchStop posts SearchStopped`, `cancelByFocus places Order.Cancel` and `actorStop places Order.ActorStop` pass.
4. [x] **Remove** — Remove `StartActor`, `RecordSearchStart`, `StartPeerActor`, `StartLoadSaveCommand`, `CancelActor`, `ActorStop` and `RecordSearchStop` from `CoreMsg`, with their arms. Files: `src/Server/Core/CoreMsg.fs` (where `CoreMsg` is defined), `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreMailboxEvents.fs`, and each test that names those cases. Done when it compiles, `CoreMsgActorCasesTests` pass, and no file under `src/` or `tests/` names `RecordSearchStart (` or `CancelActor (` as a `CoreMsg` case.

### Acceptance Criteria

1. [x] **Start** — `startActor places Order.StartActor`. The pool starts the Actor, and the reply is `Ok` of the `ActorStart` entry's `EventId`.
2. [x] **Search** — `recordSearchStart places its order and recordSearchStop posts SearchStopped`. The start does not call `pool.startActor`. The log holds the start, then `Notice.SearchStopped` of the root id.
3. [x] **Peer and load-save** — `startPeerActor and startLoadSaveCommand place their orders`. `Order.StartPeerActor` and `Order.LoadSave` carry the `PeerActorName` the caller passes.
4. [x] **Cancel** — `cancelByFocus places Order.Cancel`. The Actor for that focus finishes. The log holds `Notice.ActorStopped` of the focus and `ActorCancelled`. The reply is `Ok` of that entry's `EventId`.
5. [x] **Stop** — `actorStop places Order.ActorStop`. The reply is `Ok` of the logged `ActorStop` entry's `EventId`. The log holds `EventBody.ActorStop` of the focus and the result. The pool entry is finished.
6. [x] **Every order logs** — `each accepted Actor order appends an Ev`. A start that runs no Actor logs its start and its stop.
7. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.
8. [x] **Save** — git Save places `Order.Save` of the Workspace, then runs the push. The reply is `Ok` of that `EventId`. The log holds `EventBody.Save` of the Workspace at that id.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.

- [x] `MailboxLog.ofEv` maps `EventBody.ActorStop` to `Notice.ActorStopped` once that case exists. Step 1 maps `Order.RecordSearchStart` to `EventBody.ActorStart` on `toEv`. `ofEv` of that body stays `Order.StartActor`.
- [x] `PlaceOrder` replies `Ok` of the `EventId`. `startActor`, `recordSearchStart`, `startPeerActor`, `startLoadSaveCommand`, `cancelByFocus`, and `recordSearchStop` map that reply to `Ok ()`. `actorStop` returns the `EventId`.
- [x] `dispatchPostEvent` calls the order handler on the mailbox thread. A nested `PlaceOrder` would wait on the same processor.
- [x] A client `PostEvent` of a start that fails still stores `ActorStart` and `ActorStop` of `ActorFailed`. `startActor`'s `PlaceOrder` returns `Error` and does not log.
- [x] `Order.StartActor` from a client event still uses the query function start. `startActor` uses `pool.startActor`.
- [x] `actorStop` places `Order.ActorStop` of the caller id and the `ActorResult`. The mailbox writes `EventBody.ActorStop` of the live focus and the result, assigns that entry's `EventId`, and finishes the pool entry. No running Actor replies `Error` of "no running actor" and writes no entry. `MailboxLog.ofEv` maps `EventBody.ActorStop` to `Order.ActorStop`. `MailboxLog.toEv` maps that order back to `EventBody.ActorStop` of the focus and the result. `Notice.ActorStopped` stays the cancel stop.
- [x] `recordSearchStop` posts `Notice.SearchStopped`, then waits with `getEventId`.
- [x] A cancel that finds no running actor replies `Error` of "no running actor".
- [x] A Desk load that runs no actor logs `ActorStart` and `ActorStop` of `ActorSucceeded`.
- [x] `SyncLogic` applies an actor notice onto the live focus set. `applyNotice` takes the pool so a queued `ActorStopped` can finish the pool entry.
- [x] `Database.writeNoticeRow` leaves `ActorStopped` and `SearchStopped` unread. They change no graph. The logged `Ev` for each still gets its row in `events` through `appendEvent`. An `Order.ActorStop` entry is `EventBody.ActorStop` and gets its row the same way.
- [x] git Save places `Order.Save` of the Workspace, then runs the push. `GithubTransportActor.runOperation` returns `Ok` of that `EventId`. A push error returns `Error`. The log holds `EventBody.Save` of the Workspace at that id.

## Time

- 2026-10-21 2h — move Actor orders onto Order and Notice
- 2026-10-21 1h — `actorStop` places `Order.ActorStop`; git Save places `Order.Save` and returns that `EventId`

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [62 — Admit at the entry](62-admit-at-the-entry.md)
- [63 — Move queries to Ask](63-move-queries-to-ask.md)
- [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [GLOSSARY](../../../GLOSSARY.md)
