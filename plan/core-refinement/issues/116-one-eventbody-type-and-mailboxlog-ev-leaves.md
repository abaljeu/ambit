# 116 — One EventBody type, and MailboxLog.Ev leaves
**Status**: `coded`
One job: `MailboxEventBody`, `MailboxLog.Ev`, `MailboxLog.ofEv` and `MailboxLog.toEv` leave, and the mailbox reads a `PlacedOrder` from the History `Ev` and builds the History `Ev` from a `PlacedOrder` without them, with no change in behavior. The reason is decisions 9 **EventBody** and 14 **Leaves** in [Message shape](plan/core-refinement/cqrs-messages.md): `MailboxEventBody` and `EventBody` are one type, and `MailboxLog.Ev`, `ofEv` and `toEv` leave.

Type: coding
Blockers: [109 — Log Undo, Redo, and Load as EventBody.Order](109-log-undo-redo-and-load-as-eventbody-order.md), [110 — Log ActorStart and Cancel as EventBody.Order](110-log-actorstart-and-cancel-as-eventbody-order.md). [108 — Log Change as EventBody.Order](108-log-change-as-eventbody-order.md) is on staging.

## Inputs

### Working

#### MailboxLog

[ ] `src/Shared/MailboxLog.fs` holds the shared `PlacedOrder` record `{ orderId; authority; commandName; order }`, outside the `MailboxLog` module.
[ ] ``MailboxEventBody`` in `src/Shared/MailboxLog.fs` is `Order of PlacedOrder | Notice of Notice`.
[ ] ``MailboxLog.Ev`` in `src/Shared/MailboxLog.fs` is `{ id: EventId; body: MailboxEventBody }`.
[ ] ``ofEv`` in `src/Shared/MailboxLog.fs` maps a History `Ev` to `MailboxLog.Ev` through `orderOfBody` and `placedOrder`. It moves `submissionId`, `authority` and `commandName` into the `PlacedOrder`.
[ ] ``toEv`` in `src/Shared/MailboxLog.fs` maps `MailboxLog.Ev` back through `historyBody`. It refuses `MailboxEventBody.Notice`.
[ ] ``commandFromText`` and ``commandToText`` in `src/Shared/MailboxLog.fs` map `""` to `None` and back. They are private.
[ ] `orderOfBody` and `historyBody` map the old `EventBody` cases to orders and back. After 109 and 110, the only old case left is `EventBody.ActorStop`, which `orderOfBody` refuses.

#### Callers

[ ] ``placedOrder`` in `src/Server/Core/CoreMailboxApi.fs` calls `MailboxLog.ofEv` and matches `MailboxLog.MailboxEventBody.Order`. `messageForPosted` and `acceptOne` use it to place a posted Ev.
[ ] ``editEvent`` in `src/Server/Core/CoreMailboxLoop.fs` builds a `MailboxLog.Ev` and calls `MailboxLog.toEv`.
[ ] ``replyPlaced`` in `src/Server/Core/CoreMailboxLoop.fs` builds a `MailboxLog.Ev` and calls `MailboxLog.toEv`.
[ ] `Mailbox.PlaceOrder` in `src/Server/Core/CoreMsg.fs` carries `PlacedOrder * Caller * reply`. Read only.
[ ] No file opens `MailboxLog`. Every use is qualified.
[ ] The History `Ev` in `src/Shared/History.fs` keeps `id`, `submissionId`, `authority`, `commandName` and `body`. Read only.

#### Tests

[ ] `tests/Shared.Tests/MailboxLogTests.fs` calls `ofEv` and `toEv` through its helpers `logged`, `restored` and `placed`.
[ ] ``empty command name maps to None and returns as empty`` in `tests/Shared.Tests/MailboxLogTests.fs`.
[ ] `fromOrder` in `tests/Shared.Tests/EventTests.fs` builds a `MailboxLog.Ev` and calls `toEv`. ``Replay sets persistState Unpersisted and ignores persist Notices`` and ``Replay of Parse sets persistState Unpersisted and leaves persistedVersion`` use it.
[ ] ``postEvents places Order.Change with origin Edit`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` calls `MailboxLog.ofEv` and matches `MailboxEventBody`.

### Reference

[ ] [Message shape](plan/core-refinement/cqrs-messages.md) §3 decisions 9, 10 and 14, and decision 1 **Logged as placed**.
[ ] 2026-10-25: this ticket deletes `MailboxEventBody`, `MailboxLog.Ev`, `ofEv` and `toEv`. It is not a rename.

## Do not load

[ ] The shared `PlacedOrder` stays, as the `Mailbox.PlaceOrder` payload.
[ ] `EventBody.ActorStop` stays. Its removal is [81 — Log Actor and search stops as Notices](81-log-actor-and-search-stops-as-notices.md).
[ ] `Notice` as a mailbox case is [115 — Notice is a Mailbox case](115-notice-is-a-mailbox-case.md).

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] `MailboxEventBody`, `MailboxLog.Ev`, `ofEv` and `toEv` leave.
3. [x] The mailbox reads the `PlacedOrder` of a posted Ev, and builds the logged Ev from a `PlacedOrder`, without them.
4. [x] A logged order keeps its `submissionId`, `authority` and `commandName`, with `None` as `""`.
5. [x] Behavior does not change.
6. [x] The tests in Inputs that used `ofEv`, `toEv` or `MailboxEventBody` check the same facts without them, or leave when `EventJsonTests.fs` already checks the fact.

## Outputs

[x] `src/Shared/MailboxLog.fs`, `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoop.fs`, and any other source file the builder changes, up to 8
[x] `tests/Shared.Tests/MailboxLogTests.fs`
[x] `tests/Shared.Tests/EventTests.fs` — `fromOrder`
[x] `tests/Server.Tests/CoreMailboxDoorTests.fs` — ``postEvents places Order.Change with origin Edit``

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/116-one-eventbody-type-and-mailboxlog-ev-leaves-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts [Message shape](plan/core-refinement/cqrs-messages.md).
[ ] Stop when the edit needs more than 8 source files or 4 test files.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: `MailboxLogTests`
  - `tests/Shared.Tests`: ``Replay sets persistState Unpersisted and ignores persist Notices``
  - `tests/Shared.Tests`: ``Replay of Parse sets persistState Unpersisted and leaves persistedVersion``
  - `tests/Server.Tests`: ``postEvents places Order.Change with origin Edit``
[x] All executed tests pass.
[x] Write the verdict to `plan/core-refinement/reports/116-one-eventbody-type-and-mailboxlog-ev-leaves-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
