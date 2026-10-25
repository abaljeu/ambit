# 111 — Place search start as an order, and post SearchStopped

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [80 — Place Actor starts through PlaceOrder](80-place-actor-orders-through-placeorder.md)

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `Order` and the `EventBody` module

### Source

[ ] [EventJson.fs](src/Shared/EventJson.fs) the order codec
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `recordSearchStart`, `recordSearchStop`, `postNotice`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `placeOrder`, `runMsg`, `messageCaller`
[ ] [CoreMailboxEvents.fs](src/Server/Core/CoreMailboxEvents.fs) `operationContext`, `replyFailure`
[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) `CoreMsg.RecordSearchStart`, `CoreMsg.RecordSearchStop`
[ ] [RouteRegistration.fs](src/Server/RouteRegistration.fs) `searchDoor`
[ ] [Api.fs](src/Server/Api.fs) `SearchActorDoor`, `searchReply`, read only

### Match sites and constructors

[ ] `recordSearchStart` and `recordSearchStop` are called only from `RouteRegistration.searchDoor` and `SearchActorTests.fs`
[ ] `Api.searchReply` records the start, runs the walk, then calls `door.recordStop` with the root id
[ ] `Order` is matched with no wildcard in `EventJson`, `MailboxLog.historyBody`, `CoreMailboxLoad.placeOrder`, and `Ev.withOps`
[ ] The two `CoreMsg` cases are matched in `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`

### Tests this ticket names

[ ] [SearchActorTests.fs](tests/Server.Tests/SearchActorTests.fs) `postSearch records ActorStart at the root`, `recordSearchStop logs SearchStopped`
[ ] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) the order round-trip test from 107

## 4. Context

### Today

[ ] `CoreMsg.RecordSearchStart` appends `EventBody.ActorStart` of the request and does not call `pool.startActor`
[ ] After 81, `CoreMsg.RecordSearchStop` appends `Notice.SearchStopped` of the root id
[ ] The search route sends that stop as a message with the Browser caller when the walk ends
[ ] The walk runs inside the search route; no pool row exists for a search
[ ] No client cancels a search today

### Decisions

[ ] Alan, 2026-10-24: `Order.StartSearch` and `Order.CancelSearch`
[ ] Alan, 2026-10-24: a search just stops when done, like an Actor. The server sees the search end and posts `Notice.SearchStopped`. The search route sends no stop
[ ] `Order.CancelSearch` is only the client's cancel order. This ticket does not add it, because no client cancels a search yet

## 5. Changes

### Change

[ ] One change: the search start is placed as an order, the search end posts `Notice.SearchStopped`, and the two `CoreMsg` cases leave

### History.fs

[ ] Add `Order.StartSearch of ActorStart`
[ ] `EventBody.(|ActorStart|_|)` also gives `start` for `Order.StartSearch start`
[ ] `Ev.withOps` leaves it unchanged

### EventJson.fs

[ ] Add the order kind `startSearch`

### Server

[ ] `recordSearchStart` places `Order.StartSearch request` and maps the `EventId` reply to `()`
[ ] The `StartSearch` arm of `placeOrder` appends `EventBody.Order placed.order` with the caller, does not call `pool.startActor`, and replies `Ok` of the appended `EventId`
[ ] `recordSearchStop` becomes `CoreMailboxApi.searchStopped host rootId`, which posts `Notice.SearchStopped rootId` with `postNotice`; it takes no caller and returns `unit`
[ ] `searchDoor.recordStop` calls `searchStopped` when the walk ends
[ ] Remove `RecordSearchStart` and `RecordSearchStop` from `CoreMsg` and their arms from `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`

### Size

[ ] 7 source files: `History.fs`, `EventJson.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`, `CoreMailboxEvents.fs`, `CoreMsg.fs`, `RouteRegistration.fs`
[ ] 2 test files: `SearchActorTests.fs`, `EventJsonTests.fs`

## 6. Tests

### Search

[ ] Add `recordSearchStart places Order.StartSearch` in `SearchActorTests.fs`
[ ] The log holds the start, and the pool has no new run
[ ] Filter: `recordSearchStart places Order.StartSearch`
[ ] Rename `recordSearchStop logs SearchStopped` to `a search that ends posts SearchStopped`
[ ] Its door's `recordStop` calls `searchStopped`; the log holds the start, then `Notice.SearchStopped` of the root id
[ ] Filter: `a search that ends posts SearchStopped`

### JSON

[ ] Add `StartSearch` to the order round-trip test in `EventJsonTests.fs`
[ ] Filter: `EventJsonTests`

### Kept

[ ] Keep `postSearch records ActorStart at the root`
[ ] Filter: `SearchActorTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
