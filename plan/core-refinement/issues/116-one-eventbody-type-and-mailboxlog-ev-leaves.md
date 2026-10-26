# 116 — One EventBody type, and MailboxLog.Ev leaves

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [108 — Log Change as EventBody.Order](108-log-change-as-eventbody-order.md)
[ ] [109 — Log Undo, Redo, and Load as EventBody.Order](109-log-undo-redo-and-load-as-eventbody-order.md)
[ ] [110 — Log ActorStart and Cancel as EventBody.Order](110-log-actorstart-and-cancel-as-eventbody-order.md)

## 3. Look at

### Owner

[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) owns `MailboxEventBody`, `MailboxLog.Ev`, `ofEv`, `toEv`, `placedOrder`, `commandFromText`, `commandToText`, and the shared `PlacedOrder`

### Source

[ ] [History.fs](src/Shared/History.fs) `EventBody`, `Ev`
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `postedMailbox`, `postGraphOnly`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `orderEvent`

### Match sites and constructors

[ ] `MailboxLog.ofEv` is called in `postedMailbox`, in `postGraphOnly` after 102, and in `CoreMailboxDoorTests.fs`
[ ] `MailboxLog.toEv` is called in `orderEvent`
[ ] `MailboxEventBody` is named in `MailboxLog.fs`, `MailboxLogTests.fs`, and the two callers above
[ ] `MailboxLog.Ev` is built only by `ofEv` and in `orderEvent` (the record passed to `toEv`); it is read only in `postedMailbox`, `postGraphOnly`, `MailboxLogTests.fs`, and `CoreMailboxDoorTests.fs`
[ ] No file opens `MailboxLog`; every use is qualified

### Tests this ticket names

[ ] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs), every test
[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs), the test that calls `MailboxLog.ofEv`

## 4. Context

### Today

[ ] After 108, 109, and 110, every order body is `EventBody.Order`; `EventBody.ActorStop` may stay until 81, and `ofEv` never accepted it
[ ] The History `Ev` keeps `submissionId`, `authority`, and `commandName` (choice (c))
[ ] `MailboxEventBody` is `Order of PlacedOrder | Notice of Notice`; the shared `PlacedOrder` is `{ orderId; authority; commandName; order }` and stays (104)
[ ] `MailboxLog.Ev` is `{ id: EventId; body: MailboxEventBody }`
[ ] `ofEv` moves `submissionId`, `authority`, and `commandName` from the History `Ev` into the shared `PlacedOrder`; `toEv` moves them back
[ ] So `MailboxLog.Ev` is the History `Ev` with the envelope fields moved into the body; it carries nothing the History `Ev` lacks
[ ] `Mailbox.PlaceOrder` is `PlacedOrder * Caller * reply` (104); `postedMailbox` and `postGraphOnly` need a shared `PlacedOrder` from a History `Ev`, and `orderEvent` needs a History `Ev` from a shared `PlacedOrder`

### Decisions

[ ] 2026-10-25: this ticket deletes `MailboxEventBody`, `MailboxLog.Ev`, `ofEv`, and `toEv`; it is not a rename
[ ] Alan, 2026-10-24: this ticket also removes `MailboxLog.Ev`; its users take the History `Ev`, and `ofEv` and `toEv` leave

## 5. Changes

### Change

[ ] One deletion: `MailboxEventBody`, `MailboxLog.Ev`, `ofEv`, and `toEv` leave; their users use `EventBody` and `Ev`
[ ] The shared `PlacedOrder` stays, as the `Mailbox.PlaceOrder` payload
[ ] No behavior changes

### MailboxLog.fs

[ ] Remove `MailboxEventBody`, `MailboxLog.Ev`, `ofEv`, `toEv`, `orderOfBody`, and `historyBody`
[ ] `placedOrder` becomes public as `MailboxLog.placedOrder (event: Ev) (order: Order) : PlacedOrder`
[ ] `commandToText` becomes public as `MailboxLog.commandText`
[ ] `commandFromText` stays private; `placedOrder` uses it

### Callers

[ ] `postedMailbox` matches `event.body` as `EventBody.Order order` and builds `MailboxLog.placedOrder event order` with `authority = caller.authority`, with no `ofEv` call
[ ] `postGraphOnly` builds its shared `PlacedOrder` the same way, with no `ofEv` call
[ ] `orderEvent` builds the History `Ev` directly: `submissionId = placed.orderId.Value`, `authority = placed.authority`, `commandName = MailboxLog.commandText placed.commandName`, `body = EventBody.Order placed.order`, with no `toEv` call
[ ] No `Result` from `ofEv` or `toEv` is left to handle; remove the `Error` arms that handled it

### Size

[ ] 3 source files: `MailboxLog.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`
[ ] 2 test files: `MailboxLogTests.fs`, `CoreMailboxDoorTests.fs`
[ ] Within the limits; no follow-on ticket

## 6. Tests

### Removed

[ ] Remove each `MailboxLogTests.fs` test that maps through `ofEv` or `toEv`, and its `ofEv`, `toEv`, and `MailboxEventBody` helpers
[ ] A test that read the `PlacedOrder` through `placed` reads `EventBody.Order` from the History `Ev` instead, when it still checks something `EventJsonTests.fs` does not
[ ] `empty command name maps to None and returns as empty` checks `MailboxLog.placedOrder` gives `commandName = None` for `""` and `MailboxLog.commandText None` is `""`
[ ] Filter: `MailboxLogTests`

### Callers

[ ] The `CoreMailboxDoorTests.fs` test that calls `ofEv` matches `stored.body` as `EventBody.Order` instead of `MailboxLog.MailboxEventBody.Order`
[ ] Filter: `CoreMailboxDoorTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
