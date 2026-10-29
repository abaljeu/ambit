# 115 — The CoreQueue post is private

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] None.

## 3. Look at

### Owner

[ ] [MailboxHost.fs](src/Server/Core/MailboxHost.fs) owns the `MailboxHost` object and `postItem`

### Source

[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) `CoreQueue`, `Mailbox`
[ ] [MailboxHost.fs](src/Server/Core/MailboxHost.fs) `create`, `coreMsgItem`, `postItem`, `postForReplies`, `postAndAsyncReply`, `postMailboxAndAsyncReply`
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `addCoreMsg`, `postNotice`, `logout`
[ ] [CoreMailbox.fs](src/Server/Core/CoreMailbox.fs) `hostPublishing`
[ ] [CoreMailboxLoop.fs](src/Server/Core/CoreMailboxLoop.fs) `armMailbox`, `dispatchItem`, read only

### Raw CoreQueue posts

[ ] `MailboxHost.postItem` takes any `CoreQueue` and is `internal`
[ ] `CoreMailboxApi.addCoreMsg` posts `MailboxHost.coreMsgItem msg`, which is `CoreQueue.Public (Mailbox.CoreMsg msg)`
[ ] `CoreMailboxApi.postNotice` posts `CoreQueue.Notice`
[ ] `CoreMailboxApi.logout` posts `CoreQueue.DropAuthority`
[ ] `CoreMailboxLoop.armMailbox` posts `CoreQueue.Shutdown` on the processor it owns
[ ] `postForReplies`, `postAndAsyncReply`, and `postMailboxAndAsyncReply` wrap a `Mailbox` in `CoreQueue.Public`

### Holders of the notice and logout posts

[ ] `CoreMailbox.hostPublishing` binds `CoreMailboxApi.postNotice` into the persist thread through `bindPersistFinished`
[ ] `ParseThread` and `LazyLoadReconciliationServer` call `CoreMailboxApi.postNotice`
[ ] `RouteRegistration` calls `CoreMailboxApi.logout`
[ ] 11 test files call `CoreMailboxApi.postNotice`; `CoreMailboxDoorTests.fs` and `SessionTests.fs` call `CoreMailboxApi.logout`

## 4. Context

### Today

[ ] Any code that holds a `MailboxHost` can post any `CoreQueue` item through `postItem`
[ ] `CoreQueue.Notice` and `CoreQueue.DropAuthority` have no `Mailbox` case

### Decisions

[ ] Alan, 2026-10-29: `CoreQueue` stays a public type. Its post function is private. A holder that receives only the mailbox object posts `Mailbox` messages. An approved holder receives the mailbox object and the private `CoreQueue` post function.
[ ] Alan, 2026-10-29: Notice posting does not become a public `Mailbox` case. `CoreQueue.Notice` stays.

## 5. Changes

### Change

[ ] One change: posting a raw `CoreQueue` item needs the private post function, and only approved holders receive it

### Rules

[ ] `CoreQueue` keeps its cases: `Public`, `Notice`, `DropAuthority`, `Shutdown`
[ ] No code outside the approved holders can post `CoreQueue.Notice`, `CoreQueue.DropAuthority`, or `CoreQueue.Shutdown`
[ ] Posting `Mailbox` messages through the mailbox object is unchanged

### Size

[ ] Over the test-file limit if each of the 13 test files above changes; needs Alan's approval

## 6. Tests

### Kept

[ ] Keep `Mailbox is CoreMsg, PlaceOrder, and Ask` in [MailboxExpandTests.fs](tests/Server.Tests/MailboxExpandTests.fs)
[ ] Keep `CoreMailboxApi.logout rejects a later order once authority is dropped` in [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)
[ ] Keep `ActorStopped for an idle focus logs nothing` from [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md)
[ ] Filter: `Mailbox is CoreMsg, PlaceOrder, and Ask`
[ ] Filter: `CoreMailboxApi.logout rejects a later order once authority is dropped`
[ ] Filter: `ActorStopped for an idle focus logs nothing`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
