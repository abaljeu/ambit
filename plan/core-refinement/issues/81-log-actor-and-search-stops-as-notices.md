# 81 — Log Actor and search stops as Notices

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [110 — Log ActorStart and Cancel as EventBody.Order](110-log-actorstart-and-cancel-as-eventbody-order.md)
[ ] Lands after [45 — SetDocumentState leaves; NodeUnpersisted replaces it](45-remove-document-state-set-ops.md), 2026-10-25. Both change `History.fs`, `EventJson.fs`, `Database.fs`, `CoreMailboxLoad.fs`, and `CoreEventDispatch.fs`.

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `Notice`, `EventBody.ActorStop`, the `EventBody` module

### Source

[ ] [EventJson.fs](src/Shared/EventJson.fs) `encodeNotice`, `noticeFrom`, `decodeNoticeBody`, `encodeActorStop`, `decodeActorStopBody`, `encodeActorResult`
[ ] [Database.fs](src/Server/Database.fs) `writeNoticeRow`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `applyNotice`, `runMsg` arm `RecordSearchStop`
[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `completeAction`, `actorStop`, `appendLifecycle`
[ ] [ActorLive.fs](src/Shared/ActorLive.fs) `applyEvent`, `resultOf`
[ ] [SyncLogic.fs](src/Shared/SyncLogic.fs) `applyLoggedNotice`, `foldProjectedEvents`, `withProjectedGraph`; `isNoticeEvent` and `loadResponseToPoll`, read only
[ ] [Api.fs](src/Server/Api.fs) `countsAsExternal`, read only
[ ] [UpdateActorLive.fs](src/Client/UpdateActorLive.fs) `withActorCmdResult`, read only

### Match sites and constructors

[ ] `Notice` is matched with no wildcard in `CoreMailboxLoad.applyNotice`, `Database.writeNoticeRow`, and `EventJson.encodeNotice`
[ ] `EventBody.ActorStop` appears in 8 source files and 20 test files
[ ] Constructors: `CoreEventDispatch.actorStop`, `CoreMailboxActors.runCancel`, `CoreMailboxActors.recordFailedStart`, `EventJson.decodeActorStopBody`
[ ] Patterns: `Ev.ops`, `Ev.target`, `ActorLive.applyEvent`, `ActorLive.resultOf`, `CoreEventDispatch.completeAction`, `CoreMailboxActors.dispatchPostEvent`, `CoreMailboxApi.isClientActorStop`, `CoreMailboxApi.expandStored`, `EventJson.encodeBody`, `MailboxLog.orderOfBody`
[ ] `EventBody.Notice _` arms that now catch a stop: `SyncLogic.isNoticeEvent`, `Api.countsAsExternal`, `CoreEventDispatch.completeAction`
[ ] `SyncLogic.isNoticeEvent` and `Api.countsAsExternal` catch a stop on purpose and stay as written
[ ] `ActorLive.applyEvent` is called only from `SyncLogic.withProjectedGraph` on the poll path; `foldProjectedEvents` sends a Notice to `applyLoggedNotice`, which does not call it
[ ] The `EventBody` module function and pattern keep the other sites as written

### Tests this ticket names

[ ] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) `ActorStop failed without message decodes as empty`, `Notice JSON round trip`, `Ev JSON round-trips ActorStart and ActorStop`
[ ] [ActorLiveTests.fs](tests/Shared.Tests/ActorLiveTests.fs) `ActorStart adds and ActorStop removes a live Focus`
[ ] [SearchActorTests.fs](tests/Server.Tests/SearchActorTests.fs) `postSearch records ActorStart at the root`
[ ] [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs) `applyServerTail ActorStart adds and ActorStop removes a live Focus`

## 4. Context

### Today

[ ] A done Actor, a Cancel, and a failed start each log `EventBody.ActorStop` of the focus and the result
[ ] `CoreMsg.RecordSearchStop` calls `CoreEventDispatch.actorStop` with the root id and `ActorSucceeded`
[ ] `ActorLive.applyEvent` removes the focus on `EventBody.ActorStop`
[ ] `SyncLogic.isNoticeEvent` is true for every `EventBody.Notice`
[ ] A Notice event applies no ops on the client and does not count as substantive
[ ] `Api.countsAsExternal` is false for every `EventBody.Notice`
[ ] `SyncLogic.applyLoggedNotice` advances the event id and applies `Notice.NodePersisted`; it leaves `actorLiveFocusIds` alone
[ ] `UpdateActorLive.withActorCmdResult` reads the event list directly
[ ] `CoreEventDispatch.completeAction` has its `EventBody.Notice` arm before its `EventBody.ActorStop` arm
[ ] `EventJsonTests.fs` `ActorStop failed without message decodes as empty` decodes a JSON string of `kind` `actorStop`

### Decisions

[ ] Alan, 2026-10-24: names follow `Order.Cancel*` and `Notice.*Stopped`
[ ] ActorStop is logged as `Notice.ActorStopped`
[ ] The search stop is logged as `Notice.SearchStopped`
[ ] Existing `EventBody` member names stay
[ ] Old-form logs are dropped
[ ] Alan, 2026-10-24: the rule of [77 — Rename SnapshotDone to NodePersisted, and Notices start no catch-up](77-rename-snapshotdone-and-notices-start-no-catch-up.md) covers stops: a stop Notice starts no catch-up
[ ] A stop Notice tells the client it can drop its hold on the Actor; the client acts on it like any other Notice

## 5. Changes

### Change

[ ] One contract: `EventBody.ActorStop` leaves; a stop is logged as `Notice.ActorStopped`, and a search stop as `Notice.SearchStopped`
[ ] One client behavior changes: a poll that holds only a stop Notice starts no catch-up

### History.fs

[ ] Add `Notice.ActorStopped of focusId: NodeId * result: ActorResult`
[ ] Add `Notice.SearchStopped of rootId: NodeId`
[ ] Remove `ActorStop` from `EventBody`
[ ] The `EventBody` module gains `ActorStop (focusId: NodeId, result: ActorResult)`, which is `EventBody.Notice (Notice.ActorStopped(focusId, result))`
[ ] The module gains `(|ActorStop|_|)` over `Notice.ActorStopped`

### EventJson.fs

[ ] `encodeNotice` writes `actorStopped` with `focusId` and the result fields that `encodeActorStop` writes today
[ ] `encodeNotice` writes `searchStopped` with `rootId`
[ ] The Notice decoder reads both
[ ] Remove the `actorStop` body kind from `encodeBody` and `decodeBody`

### Server

[ ] `Database.writeNoticeRow` writes no row for `Notice.ActorStopped` or `Notice.SearchStopped`
[ ] `CoreMailboxLoad.applyNotice` returns `Ok ()` for both
[ ] The `RecordSearchStop` arm of `runMsg` appends `EventBody.Notice (Notice.SearchStopped rootId)` with the caller, the way `CoreEventDispatch.actorStop` appends today
[ ] `CoreEventDispatch.completeAction` puts the `EventBody.ActorStop` arm before the `EventBody.Notice` arm

### Client

[ ] `SyncLogic.isNoticeEvent` and `Api.countsAsExternal` do not change; a stop is a Notice to both
[ ] `SyncLogic.applyLoggedNotice` sets `actorLiveFocusIds` to `ActorLive.applyEvent event st.actorLiveFocusIds` for every Notice
[ ] `ActorLive.applyEvent` removes the focus on `Notice.ActorStopped`, through the `EventBody.(|ActorStop|_|)` pattern
[ ] `ActorLive.applyEvent` removes the root on `Notice.SearchStopped`
[ ] `ActorLive.applyEvent` leaves the live set alone for every other Notice
[ ] `ActorLive.resultOf` gives the same result for `Notice.SearchStopped root` as for an `ActorStop` of `root` and `ActorSucceeded`
[ ] `UpdateActorLive.withActorCmdResult` does not change

### Size

[ ] 7 source files: `History.fs`, `EventJson.fs`, `Database.fs`, `CoreMailboxLoad.fs`, `CoreEventDispatch.fs`, `ActorLive.fs`, `SyncLogic.fs`
[ ] 4 test files: `EventJsonTests.fs`, `ActorLiveTests.fs`, `SearchActorTests.fs`, `SyncLogicTests.fs`

## 6. Tests

### JSON

[ ] Change `ActorStop failed without message decodes as empty` to decode a Notice of `kind` `actorStopped`
[ ] Add the two cases to `Notice JSON round trip`
[ ] Add `An old actorStop row is skipped on read`
[ ] Filter: `ActorStop failed without message decodes as empty`
[ ] Filter: `Notice JSON round trip`
[ ] Filter: `An old actorStop row is skipped on read`

### Search stop

[ ] Add `recordSearchStop logs SearchStopped` in `SearchActorTests.fs`
[ ] The log holds the search start, then `Notice.SearchStopped` of the root id
[ ] Filter: `recordSearchStop logs SearchStopped`

### Browser

[ ] Add `SearchStopped removes the root` in `ActorLiveTests.fs`
[ ] Filter: `SearchStopped removes the root`
[ ] Add `a stop-only poll removes the live focus and starts no catch-up` in `SyncLogicTests.fs`
[ ] The state has a live focus; the poll holds only `Notice.ActorStopped` of that focus
[ ] `applyServerTail` removes the focus from `actorLiveFocusIds`
[ ] `loadResponseToPoll` gives `externalChanges` false, so no catch-up starts
[ ] Filter: `a stop-only poll removes the live focus and starts no catch-up`

### Kept

[ ] Keep `Ev JSON round-trips ActorStart and ActorStop`
[ ] Keep `ActorStart adds and ActorStop removes a live Focus`
[ ] Keep `postSearch records ActorStart at the root`
[ ] Keep `applyServerTail ActorStart adds and ActorStop removes a live Focus`
[ ] Filter: `ActorStart and ActorStop`
[ ] Filter: `ActorStop removes a live Focus`
[ ] Filter: `postSearch records ActorStart at the root`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
