# 68 — Remove the CoreMsg case

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [102 — Place Parse changes as Order.Change](102-place-parse-changes-as-order-change.md)
[ ] [65 — Place Undo and Redo as orders](65-undo-and-redo-resolve-their-ops-from-the-target.md)
[ ] [80 — Place Actor starts through PlaceOrder](80-place-actor-orders-through-placeorder.md)
[ ] [111 — Place search start as an order, and post SearchStopped](111-place-search-start-as-an-order-and-post-searchstopped.md)
[ ] [112 — Place Cancel through PlaceOrder](112-place-cancel-through-placeorder.md)
[ ] [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md)
[ ] [115 — Notice is a Mailbox case](115-notice-is-a-mailbox-case.md)
[ ] [116 — One EventBody type, and MailboxLog.Ev leaves](116-one-eventbody-type-and-mailboxlog-ev-leaves.md)

## 3. Look at

### Owner

[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) owns `CoreMsg` and `Mailbox.CoreMsg`

### Source

[ ] [MailboxHost.fs](src/Server/Core/MailboxHost.fs) `coreMsgItem`
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `reply`, `postEventAccepted`, `postedMailbox`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `runMsg`, `messageCaller`, `rejectDropped`, `dispatchMailbox`, `isStartupRead`, `placeOrder`
[ ] [CoreMailboxEvents.fs](src/Server/Core/CoreMailboxEvents.fs) `operationContext`, `replyFailure`
[ ] [CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) `dispatchPostEvent`, `postActorStart`, `postCancel`

### Match sites and constructors

[ ] After 102, 65, 80, 111, 112, and 114, `CoreMsg` has one case, `PostEvent`
[ ] `PostEvent` is built in `postEventAccepted` and in `postedMailbox`
[ ] `postedMailbox` sends a client `ActorStart`, a client `Cancel`, and a posted `Ev` with a non-zero id to `PostEvent`
[ ] `Mailbox.CoreMsg` is matched in `dispatchMailbox`, `isStartupRead`, and `MailboxExpandTests.mailboxKind`

### Tests this ticket names

[ ] [MailboxExpandTests.fs](tests/Server.Tests/MailboxExpandTests.fs) `Mailbox is CoreMsg, PlaceOrder, Ask, and Notice`, `postEvent runs through Mailbox CoreMsg`
[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `CoreMsg is not a public type`, `ActorStart and Cancel still post as PostEvent`
[ ] [Issue41CoreMailboxTests.fs](tests/Server.Tests/Issue41CoreMailboxTests.fs) `postEvent rejects a non-zero EventId on a new client Event`

## 4. Context

### Today

[ ] `dispatchPostEvent` sends a client `ActorStart` to `postActorStart` and a client `Cancel` to `postCancel`
[ ] Since 80 and 112, `placeOrder` runs the same start and cancel logic, keyed by `orderId`
[ ] A posted `Ev` with a non-zero id gets `Error "posted EventId must be zero"`

### Decisions

[ ] Alan, 2026-10-24: 68 keeps only the `CoreMsg` removal

## 5. Changes

### Change

[ ] One contract: the client `ActorStart` and `Cancel` are placed through `Mailbox.PlaceOrder`; then `CoreMsg`, `Mailbox.CoreMsg`, and `MailboxHost.coreMsgItem` leave

### CoreMailboxApi.fs

[ ] `postedMailbox` places the client `ActorStart` and `Cancel` through `Mailbox.PlaceOrder`, as it already places every other order body
[ ] `postedMailbox` replies `Error "posted EventId must be zero"` for a non-zero id, before it places
[ ] `postEventAccepted` uses `postedMailbox`
[ ] Remove `reply`

### Server

[ ] Remove `CoreMsg` and `Mailbox.CoreMsg`
[ ] Remove `MailboxHost.coreMsgItem`
[ ] Remove `runMsg`, `messageCaller`, and `rejectDropped`, and the `Mailbox.CoreMsg` arms of `dispatchMailbox` and `isStartupRead`
[ ] Remove `operationContext` and `replyFailure` arms for `CoreMsg`, and the functions if nothing else calls them
[ ] Remove `dispatchPostEvent`, `postActorStart`, and `postCancel`

### Size

[ ] 6 source files: `CoreMsg.fs`, `MailboxHost.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`, `CoreMailboxEvents.fs`, `CoreMailboxActors.fs`
[ ] 3 test files: `MailboxExpandTests.fs`, `CoreMailboxDoorTests.fs`, `Issue41CoreMailboxTests.fs`

## 6. Tests

### Shape

[ ] Rename `Mailbox is CoreMsg, PlaceOrder, Ask, and Notice` to `Mailbox is PlaceOrder, Ask, and Notice`
[ ] Filter: `Mailbox is PlaceOrder, Ask, and Notice`
[ ] Rename `postEvent runs through Mailbox CoreMsg` to `postEvent runs through Mailbox PlaceOrder`
[ ] Filter: `postEvent runs through Mailbox PlaceOrder`
[ ] Rename `CoreMsg is not a public type` to `CoreMsg is gone`; no type named `CoreMsg` is in the server assembly
[ ] Filter: `CoreMsg is gone`
[ ] Rename `ActorStart and Cancel still post as PostEvent` to `ActorStart and Cancel are placed as orders`
[ ] Filter: `ActorStart and Cancel are placed as orders`

### Kept

[ ] Keep `postEvent rejects a non-zero EventId on a new client Event`
[ ] Filter: `postEvent rejects a non-zero EventId on a new client Event`
[ ] Keep every test in `CancelByFocusTests.fs`
[ ] Filter: `CancelByFocusTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
