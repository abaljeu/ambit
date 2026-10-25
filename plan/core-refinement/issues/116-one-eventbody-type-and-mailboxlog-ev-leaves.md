# 116 — One EventBody type, and MailboxLog.Ev leaves

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
[ ] `MailboxLog.Ev` is built only by `ofEv` and in `orderEvent` (the record passed to `toEv`); it is read only in `postedMailbox`, `MailboxLogTests.fs`, and `CoreMailboxDoorTests.fs`
[ ] No file opens `MailboxLog`; every use is qualified

### Tests this ticket names

[ ] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs), every test
[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs), the test that calls `MailboxLog.ofEv`

## 4. Context

### Today

[ ] After 110 and 81, `EventBody` is `Order of Order | Notice of Notice`
[ ] `MailboxEventBody` is `Order of Order | Notice of Notice`
[ ] `MailboxLog.Ev` has the fields of `Ev` with a `MailboxEventBody` body
[ ] `ofEv` and `toEv` copy one to the other
[ ] After 104, `MailboxLog.Ev` has the same fields as the History `Ev`; after this rename its body is `EventBody`, so it is the History `Ev` under a second name, and `ofEv` and `toEv` are the identity

### Decisions

[ ] Alan, 2026-10-24: the last ticket is a pure rename that makes `MailboxEventBody` and `EventBody` one type
[ ] Alan, 2026-10-24: this ticket also removes `MailboxLog.Ev`; its users take the History `Ev`, and `ofEv` and `toEv` leave rather than stay as the identity

## 5. Changes

### Change

[ ] One rename: `MailboxEventBody` and `MailboxLog.Ev` leave; their users use `EventBody` and `Ev`
[ ] No behavior changes

### MailboxLog.fs

[ ] Remove `MailboxEventBody`, `MailboxLog.Ev`, `ofEv`, `toEv`, and `historyBody`
[ ] `MailboxLog.commandName` and `MailboxLog.commandText` stay

### Callers

[ ] `postedMailbox` takes the History `Ev` and matches `event.body` as `EventBody.Order` or `EventBody.Notice`, with no `ofEv` call
[ ] `orderEvent` builds the History `Ev` directly, with body `EventBody.Order placed.order`, with no `toEv` call
[ ] No `Result` from `ofEv` or `toEv` is left to handle; remove the `Error` arms that handled it

### Size

[ ] 3 source files: `MailboxLog.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`
[ ] 2 test files: `MailboxLogTests.fs`, `CoreMailboxDoorTests.fs`
[ ] Within the limits; no follow-on ticket

## 6. Tests

### Removed

[ ] Remove each `MailboxLogTests.fs` test that maps through `ofEv` or `toEv`, and its `ofEv`, `toEv`, and `MailboxEventBody` helpers
[ ] A test that read the `PlacedOrder` through `placed` reads `EventBody.Order` from the History `Ev` instead, when it still checks something `EventJsonTests.fs` does not
[ ] Keep each `MailboxLogTests.fs` test of `commandName` and `commandText`
[ ] Filter: `MailboxLogTests`

### Callers

[ ] The `CoreMailboxDoorTests.fs` test that calls `ofEv` matches `stored.body` as `EventBody.Order` instead of `MailboxLog.MailboxEventBody.Order`
[ ] Filter: `CoreMailboxDoorTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
