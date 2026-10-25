# 80 — Place Actor starts through PlaceOrder

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [81 — Log Actor and search stops as Notices](81-log-actor-and-search-stops-as-notices.md)

## 3. Look at

### Owner

[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) owns `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, `CoreMsg.StartLoadSaveCommand`

### Source

[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `startActor`, `startPeerActor`, `startLoadSaveCommand`, `reply`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `placeOrder`, `runMsg`, `messageCaller`, `dispatchStartLoadSaveCommand`
[ ] [CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) `dispatchStartActor`, `dispatchStartPeerActor`, `dispatchActorStartResult`, `prepareActorStart`, `finishReadyStart`, `postActorStart`
[ ] [CoreMailboxEvents.fs](src/Server/Core/CoreMailboxEvents.fs) `operationContext`, `replyFailure`, `storedBySubmission`

### Match sites and constructors

[ ] The three cases are built only in `CoreMailboxApi`
[ ] They are matched in `CoreMailboxLoad.runMsg`, `CoreMailboxLoad.messageCaller`, `CoreMailboxEvents.operationContext`, and `CoreMailboxEvents.replyFailure`
[ ] `placeOrder` today replies `Error` for `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave`

### Tests this ticket names

[ ] [CoreMsgActorCasesTests.fs](tests/Server.Tests/CoreMsgActorCasesTests.fs), every test
[ ] [LoadSaveCommandTests.fs](tests/Server.Tests/LoadSaveCommandTests.fs), every test
[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `CoreMailboxApi.startActor appends ActorStart to lifecycle events`

## 4. Context

### Today

[ ] Each start function posts its `CoreMsg` case through `reply` and gets `Result<unit, string>`
[ ] The pool start appends `EventBody.ActorStart` of the request with a new submission id
[ ] A start that runs no Actor replies `Ok ()` and appends nothing
[ ] A client `EventBody.ActorStart` goes through `postActorStart`, which finds a resubmission by `storedBySubmission`
[ ] After 110, `EventBody.ActorStart start` is `EventBody.Order (Order.StartActor start)`

### Decisions

[ ] Decision 19 **One case each** in [Message shape](plan/core-refinement/cqrs-messages.md): each Actor case is an `Order` placed through `Mailbox.PlaceOrder`
[ ] Every accepted order appends an `Ev`
[ ] Each handler is the one its `CoreMsg` case uses today

## 5. Changes

### Change

[ ] One change: the pool start, the peer start, and the load-save start are placed as orders, and their three `CoreMsg` cases leave

### CoreMailboxApi.fs

[ ] `startActor` places `Order.StartActor request` through `Mailbox.PlaceOrder`
[ ] `startPeerActor` places `Order.StartPeerActor(peerName, request)`
[ ] `startLoadSaveCommand` places `Order.LoadSave(path, peerName, request)`
[ ] Each `PlacedOrder` has a new `orderId`, the caller, and no `commandName`
[ ] Each function keeps its return type and maps the `EventId` reply to `()`

### Handlers

[ ] `placeOrder` sends each of the three orders to the handler its `CoreMsg` case uses today
[ ] Each handler takes the `PlacedOrder`
[ ] Each handler uses `placed.orderId` as the submission id and replies the stored `Ev` for a resubmission, as `postActorStart` does
[ ] Each handler logs `EventBody.Order placed.order`
[ ] A start that runs no Actor appends the start, then `EventBody.ActorStop` of its focus and `ActorSucceeded`
[ ] `PlaceOrder` replies `Ok` of the `EventId` of the first appended `Ev`

### Remove

[ ] Remove `StartActor`, `StartPeerActor`, and `StartLoadSaveCommand` from `CoreMsg`
[ ] Remove their arms from `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`

### Size

[ ] 5 source files: `CoreMsg.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`, `CoreMailboxActors.fs`, `CoreMailboxEvents.fs`
[ ] 3 test files: `CoreMsgActorCasesTests.fs`, `LoadSaveCommandTests.fs`, `CoreMailboxDoorTests.fs`

## 6. Tests

### Start

[ ] Add `startActor places Order.StartActor` in `CoreMsgActorCasesTests.fs`
[ ] The pool starts the Actor, and the log holds the start
[ ] Filter: `startActor places Order.StartActor`

### Peer and load-save

[ ] Add `startPeerActor and startLoadSaveCommand place their orders` in `LoadSaveCommandTests.fs`
[ ] `Order.StartPeerActor` and `Order.LoadSave` carry the `PeerActorName` the caller passes
[ ] Filter: `startPeerActor and startLoadSaveCommand place their orders`

### Every order logs

[ ] Add `each accepted Actor start appends an Ev` in `CoreMsgActorCasesTests.fs`
[ ] A start that runs no Actor logs its start and its stop
[ ] Filter: `each accepted Actor start appends an Ev`

### Kept

[ ] Keep `CoreMailboxApi.startActor appends ActorStart to lifecycle events`
[ ] Keep every other test in `CoreMsgActorCasesTests.fs` and `LoadSaveCommandTests.fs`
[ ] Filter: `CoreMailboxApi.startActor appends ActorStart to lifecycle events`
[ ] Filter: `CoreMsgActorCasesTests`
[ ] Filter: `LoadSaveCommandTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
