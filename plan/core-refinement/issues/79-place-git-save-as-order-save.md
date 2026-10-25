# 79 — Place git Save as Order.Save

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [109 — Log Undo, Redo, and Load as EventBody.Order](109-log-undo-redo-and-load-as-eventbody-order.md)
[ ] Out of the type chain, 2026-10-25. It adds `Order.Save` and removes no extra type. No ticket waits on it.

## 3. Look at

### Owner

[ ] [GithubTransportActor.fs](src/Server/GithubTransportActor.fs) owns `runOperation`, `saveTracked`, `runBody`

### Source

[ ] [History.fs](src/Shared/History.fs) `Order`
[ ] [EventJson.fs](src/Shared/EventJson.fs) `encodeOrder`, `decodeOrder`
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `mbLoadNode`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `placeOrder`, `replyLoad`
[ ] [CoreDoor.fs](src/Server/Core/CoreDoor.fs) `loadNode`
[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `appendLifecycle`, read only

### Match sites

[ ] `Order` is matched in `CoreMailboxLoad.placeOrder` and `EventJson.encodeOrder`
[ ] `MailboxLog.historyBody` maps a new order to `HistoryBody.Order order` with no edit
[ ] `Ev.ops` and `Ev.target` give `None` for `EventBody.Order (Order.Save _)`, so Undo skips it
[ ] `CoreEventDispatch.completeAction` refuses a client `EventBody.Order` that is not a Change, Undo, Redo, or Load

### Tests this ticket names

[ ] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) `Ev JSON round-trips each EventBody.Order case`
[ ] [MailboxExpandTests.fs](tests/Server.Tests/MailboxExpandTests.fs) `loadNode logs Order.Load`
[ ] [GithubTransportActorTests.fs](tests/Server.Tests/GithubTransportActorTests.fs), the git Save tests

## 4. Context

### git Save

[ ] `GithubTransportActor.runOperation` runs git Load and git Save
[ ] For `GithubTransportOperation.Save` it calls `saveTracked`, which commits the Workspace and pushes
[ ] It returns `Ok ()` or the push `Error`
[ ] git Save appends no `Ev`

### Load

[ ] `CoreDoor.loadNode` calls `CoreMailboxApi.mbLoadNode`
[ ] `mbLoadNode` places `Order.Load` through `Mailbox.PlaceOrder`
[ ] `replyLoad` appends one `Ev` through `appendLifecycle` and replies `Ok` of its id

### Decisions

[ ] Decision 2 **Load is logged** and decision 19 **One case each** in [Message shape](plan/core-refinement/cqrs-messages.md)
[ ] Load and Save have the same shape
[ ] The mailbox assigns and logs the Save `EventId`, then the push runs
[ ] Save does not Load or parse
[ ] Alan, 2026-10-24: a new body is an `Order` inside `EventBody.Order`, and no new `EventBody` case is added

## 5. Changes

### Change

[ ] One change: git Save places `Order.Save` and logs it as `EventBody.Order`

### History.fs

[ ] Add `Order.Save of nodeId: NodeId`

### EventJson.fs

[ ] Order kind `save` writes and reads `nodeId`

### CoreMailboxApi.fs

[ ] Add `mbSaveNode host caller subject : Async<Result<EventId, string>>`
[ ] It places `Order.Save subject` through `Mailbox.PlaceOrder`, the way `mbLoadNode` places `Order.Load`

### CoreMailboxLoad.fs

[ ] `placeOrder` sends `Order.Save subject` to `replySave`
[ ] `replySave` appends one `Ev` of `EventBody.Order placed.order` through `appendLifecycle`
[ ] `replySave` replies `Ok` of that id
[ ] `replySave` does not Load, parse, or push

### CoreDoor.fs

[ ] Add `saveNode` beside `loadNode`

### GithubTransportActor.fs

[ ] `runOperation` for `GithubTransportOperation.Save` calls `CoreDoor.saveNode` of the Workspace first
[ ] On `Ok` of the `EventId` it runs `saveTracked` and returns `Ok` of that `EventId`
[ ] A push error returns `Error`
[ ] An `Error` from `saveNode` returns that `Error` and runs no push
[ ] `runOperation` returns `Result<EventId, string>` for Load and Save
[ ] Load returns the `EventId` that `CoreDoor.loadNode` replies
[ ] `runBody` maps `Ok` to `ActorSucceeded`

### Size

[ ] 6 source files: `History.fs`, `EventJson.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`, `CoreDoor.fs`, `GithubTransportActor.fs`
[ ] 3 test files: `EventJsonTests.fs`, `MailboxExpandTests.fs`, `GithubTransportActorTests.fs`

## 6. Tests

### Logged

[ ] Add `saveNode logs Order.Save` in `MailboxExpandTests.fs`
[ ] `saveNode` of a Workspace replies `Ok` of a new `EventId`
[ ] The log at that id is `EventBody.Order (Order.Save workspace)`
[ ] The parse stack gains no node
[ ] Filter: `saveNode logs Order.Save`

### git Save

[ ] Add `git Save logs Order.Save before the push` in `GithubTransportActorTests.fs`
[ ] git Save returns `Ok` of the `EventId`
[ ] The log holds `EventBody.Order (Order.Save workspace)` at that id
[ ] The push runs after that entry is logged
[ ] Filter: `git Save logs Order.Save before the push`

### Push error

[ ] Add `git Save push error returns Error` in `GithubTransportActorTests.fs`
[ ] A failed push returns `Error` of the push message
[ ] Filter: `git Save push error returns Error`

### JSON

[ ] `Ev JSON round-trips each EventBody.Order case` includes `Order.Save`
[ ] Filter: `Ev JSON round-trips each EventBody.Order case`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
