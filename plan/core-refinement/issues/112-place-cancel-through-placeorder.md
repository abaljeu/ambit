# 112 — Place Cancel through PlaceOrder

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [111 — Place search start and stop as orders](111-place-search-start-and-stop-as-orders.md)

## 3. Look at

### Owner

[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) owns `CoreMsg.CancelActor`

### Source

[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `cancelByFocus`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `placeOrder`, `runMsg`, `messageCaller`
[ ] [CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) `dispatchCancelActor`, `runCancel`, `postCancel`
[ ] [CoreMailboxEvents.fs](src/Server/Core/CoreMailboxEvents.fs) `operationContext`, `replyFailure`

### Match sites and constructors

[ ] `CoreMsg.CancelActor` is built in `cancelByFocus` and in `MailboxExpandTests.fs`
[ ] It is matched in `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`

### Tests this ticket names

[ ] [CancelByFocusTests.fs](tests/Server.Tests/CancelByFocusTests.fs), every test
[ ] [MailboxExpandTests.fs](tests/Server.Tests/MailboxExpandTests.fs) `Mailbox is CoreMsg, PlaceOrder, and Ask`

## 4. Context

### Today

[ ] `dispatchCancelActor` appends `EventBody.ActorStop` of the focus and `ActorCancelled` and finishes the pool entry
[ ] With no running Actor for the focus it appends nothing
[ ] `postCancel` finds a resubmission by `storedBySubmission`
[ ] `Mailbox is CoreMsg, PlaceOrder, and Ask` builds `CoreMsg.CancelActor` as its sample

## 5. Changes

### Change

[ ] One change: `cancelByFocus` places `Order.Cancel`, and `CoreMsg.CancelActor` leaves

### Server

[ ] `cancelByFocus` places `Order.Cancel focusId` with a new `orderId` and maps the reply to `()`
[ ] The `Cancel` arm of `placeOrder` runs `runCancel` with `placed.orderId` as the submission id, as `postCancel` does
[ ] A stored stop replies `Ok` of its `EventId`
[ ] A Cancel for a run that is no longer active appends nothing and replies `Ok EventId.zero`
[ ] Remove `dispatchCancelActor`
[ ] Remove `CancelActor` from `CoreMsg` and its arms from `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`

### Size

[ ] 5 source files: `CoreMsg.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`, `CoreMailboxActors.fs`, `CoreMailboxEvents.fs`
[ ] 2 test files: `CancelByFocusTests.fs`, `MailboxExpandTests.fs`

## 6. Tests

### Cancel

[ ] Add `cancelByFocus places Order.Cancel` in `CancelByFocusTests.fs`
[ ] The Actor finishes, and the log holds `EventBody.ActorStop` of the focus and `ActorCancelled`
[ ] Filter: `cancelByFocus places Order.Cancel`
[ ] Add `second Cancel for the same run logs one stop`
[ ] The second Cancel replies `Ok ()`, and the log holds one cancel stop
[ ] Filter: `second Cancel for the same run logs one stop`

### Shape

[ ] Change `Mailbox is CoreMsg, PlaceOrder, and Ask` to build `CoreMsg.PostEvent` as its sample
[ ] Filter: `Mailbox is CoreMsg, PlaceOrder, and Ask`

### Kept

[ ] Keep every other test in `CancelByFocusTests.fs`
[ ] Filter: `CancelByFocusTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
