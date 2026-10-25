# 115 — Notice is a Mailbox case

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md)

## 3. Look at

### Owner

[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) owns `Mailbox` and `CoreQueue`

### Source

[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `postNotice`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `dispatchMailbox`, `dispatchItem`, `isStartupRead`

### Match sites and constructors

[ ] `CoreQueue.Notice` is built only in `postNotice`
[ ] `CoreQueue.Notice` is matched in `dispatchItem` and in `MailboxExpandTests.fs`
[ ] `Mailbox` is matched with no wildcard in `dispatchMailbox`, `isStartupRead`, and `MailboxExpandTests.mailboxKind`

### Tests this ticket names

[ ] [MailboxExpandTests.fs](tests/Server.Tests/MailboxExpandTests.fs) `Mailbox is CoreMsg, PlaceOrder, and Ask`

## 4. Context

### Today

[ ] A Notice is a `CoreQueue` case beside `CoreQueue.Public`
[ ] `dispatchItem` logs the Notice, then applies it

### Decisions

[ ] Alan, 2026-10-24: the mailbox receives Order, Ask, and Notice, and logs Order and Notice

## 5. Changes

### Change

[ ] One change: `Notice` moves from `CoreQueue` to `Mailbox`

### CoreMsg.fs

[ ] `Mailbox` gains `Notice of Notice`
[ ] Remove `CoreQueue.Notice`

### Server

[ ] `postNotice` posts `CoreQueue.Public (Mailbox.Notice notice)`
[ ] The `CoreQueue.Notice` arm of `dispatchItem` moves to a `Mailbox.Notice` arm of `dispatchMailbox`
[ ] `isStartupRead` is false for `Mailbox.Notice`

### Size

[ ] 3 source files: `CoreMsg.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`
[ ] 1 test file: `MailboxExpandTests.fs`

## 6. Tests

### Shape

[ ] Rename `Mailbox is CoreMsg, PlaceOrder, and Ask` to `Mailbox is CoreMsg, PlaceOrder, Ask, and Notice`
[ ] `mailboxKind` gains `Notice`, and the test builds a `Mailbox.Notice`
[ ] Remove the `CoreQueue.Notice` arm from the test
[ ] Filter: `Mailbox is CoreMsg, PlaceOrder, Ask, and Notice`

### Kept

[ ] Keep `ActorStopped for an idle focus logs nothing` from [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md)
[ ] Filter: `ActorStopped for an idle focus logs nothing`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
