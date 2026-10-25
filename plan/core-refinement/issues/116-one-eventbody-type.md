# 116 — One EventBody type

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [68 — Remove the CoreMsg case](68-remove-the-coremsg-case.md)

## 3. Look at

### Owner

[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) owns `MailboxEventBody`, `MailboxLog.Ev`, `ofEv`, `toEv`

### Source

[ ] [History.fs](src/Shared/History.fs) `EventBody`, `Ev`
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `postedMailbox`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `orderEvent`

### Match sites and constructors

[ ] `MailboxLog.ofEv` is called in `postedMailbox` and in `CoreMailboxDoorTests.fs`
[ ] `MailboxLog.toEv` is called in `orderEvent`
[ ] `MailboxEventBody` is named in `MailboxLog.fs`, `MailboxLogTests.fs`, and the two callers above

### Tests this ticket names

[ ] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs), every test
[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs), the test that calls `MailboxLog.ofEv`

## 4. Context

### Today

[ ] After 110 and 81, `EventBody` is `Order of Order | Notice of Notice`
[ ] `MailboxEventBody` is `Order of Order | Notice of Notice`
[ ] `MailboxLog.Ev` has the fields of `Ev` with a `MailboxEventBody` body
[ ] `ofEv` and `toEv` copy one to the other

### Decisions

[ ] Alan, 2026-10-24: the last ticket is a pure rename that makes `MailboxEventBody` and `EventBody` one type

## 5. Changes

### Change

[ ] One rename: `MailboxEventBody` and `MailboxLog.Ev` leave; their users use `EventBody` and `Ev`
[ ] No behavior changes

### MailboxLog.fs

[ ] Remove `MailboxEventBody`, `MailboxLog.Ev`, `ofEv`, `toEv`, and `historyBody`
[ ] `MailboxLog.commandName` and `MailboxLog.commandText` stay

### Callers

[ ] `postedMailbox` matches `event.body` as `EventBody.Order` or `EventBody.Notice`
[ ] `orderEvent` builds an `Ev` with body `EventBody.Order placed.order`

### Size

[ ] 3 source files: `MailboxLog.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`
[ ] 2 test files: `MailboxLogTests.fs`, `CoreMailboxDoorTests.fs`

## 6. Tests

### Removed

[ ] Remove each `MailboxLogTests.fs` test that maps through `ofEv` or `toEv`
[ ] Keep each `MailboxLogTests.fs` test of `commandName` and `commandText`
[ ] Filter: `MailboxLogTests`

### Callers

[ ] The `CoreMailboxDoorTests.fs` test that calls `ofEv` matches `stored.body` instead
[ ] Filter: `CoreMailboxDoorTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
