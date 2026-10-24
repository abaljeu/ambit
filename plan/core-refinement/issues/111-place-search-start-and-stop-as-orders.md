# 111 — Place search start and stop as orders

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
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `recordSearchStart`, `recordSearchStop`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `placeOrder`, `runMsg`, `messageCaller`
[ ] [CoreMailboxEvents.fs](src/Server/Core/CoreMailboxEvents.fs) `operationContext`, `replyFailure`
[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) `CoreMsg.RecordSearchStart`, `CoreMsg.RecordSearchStop`

### Match sites and constructors

[ ] `recordSearchStart` and `recordSearchStop` are called from `RouteRegistration.searchDoor`; their signatures stay
[ ] `Order` is matched with no wildcard in `EventJson`, `MailboxLog.historyBody`, `CoreMailboxLoad.placeOrder`, and `Ev.withOps`
[ ] The two `CoreMsg` cases are matched in `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`

### Tests this ticket names

[ ] [SearchActorTests.fs](tests/Server.Tests/SearchActorTests.fs) `postSearch records ActorStart at the root`, `recordSearchStop logs SearchStopped`
[ ] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) the order round-trip test from 107

## 4. Context

### Today

[ ] `CoreMsg.RecordSearchStart` appends `EventBody.ActorStart` of the request and does not call `pool.startActor`
[ ] After 81, `CoreMsg.RecordSearchStop` appends `Notice.SearchStopped` of the root id

### Decisions

[ ] Alan, 2026-10-24: `Order.StartSearch` and `Order.CancelSearch`
[ ] `CancelSearch` logs `Notice.SearchStopped`

## 5. Changes

### Change

[ ] One change: the search start and stop are placed as orders, and their two `CoreMsg` cases leave

### History.fs

[ ] Add `Order.StartSearch of ActorStart` and `Order.CancelSearch of rootId: NodeId`
[ ] `EventBody.(|ActorStart|_|)` also gives `start` for `Order.StartSearch start`
[ ] `Ev.withOps` leaves both unchanged

### EventJson.fs

[ ] Add the order kinds `startSearch` and `cancelSearch`

### Server

[ ] `recordSearchStart` places `Order.StartSearch request`; `recordSearchStop` places `Order.CancelSearch rootId`
[ ] Both map the `EventId` reply to `()`
[ ] The `StartSearch` arm of `placeOrder` appends `EventBody.Order placed.order` with the caller and does not call `pool.startActor`
[ ] The `CancelSearch` arm appends `EventBody.Notice (Notice.SearchStopped rootId)` with the caller
[ ] Each arm replies `Ok` of the appended `EventId`
[ ] Remove `RecordSearchStart` and `RecordSearchStop` from `CoreMsg` and their arms from `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`

### Size

[ ] 6 source files: `History.fs`, `EventJson.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`, `CoreMailboxEvents.fs`, `CoreMsg.fs`
[ ] 2 test files: `SearchActorTests.fs`, `EventJsonTests.fs`

## 6. Tests

### Search

[ ] Add `recordSearchStart places Order.StartSearch` in `SearchActorTests.fs`
[ ] The log holds the start, and the pool has no new run
[ ] Filter: `recordSearchStart places Order.StartSearch`
[ ] Add `recordSearchStop places Order.CancelSearch`
[ ] The log holds `Notice.SearchStopped` of the root id
[ ] Filter: `recordSearchStop places Order.CancelSearch`

### JSON

[ ] Add `StartSearch` and `CancelSearch` to the order round-trip test in `EventJsonTests.fs`
[ ] Filter: `EventJsonTests`

### Kept

[ ] Keep `postSearch records ActorStart at the root` and `recordSearchStop logs SearchStopped`
[ ] Filter: `SearchActorTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
