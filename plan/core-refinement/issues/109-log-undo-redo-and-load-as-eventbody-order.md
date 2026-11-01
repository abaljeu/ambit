# 109 — Log Undo, Redo, and Load as EventBody.Order
**Status**: `coded`
One job: The `EventBody.Undo`, `EventBody.Redo` and `EventBody.Load` cases leave History with their `undo`, `redo` and `load` body kinds, and the Server logs a Load as `EventBody.Order` of `Order.Load`. The reason is decision 9 **EventBody** and decision 1 **Logged as placed** in [Message shape](plan/core-refinement/cqrs-messages.md): the old cases leave one at a time, and the logged order is the placed order.

Type: coding
Blockers: none ([65 — Place Undo and Redo as orders](65-undo-and-redo-resolve-their-ops-from-the-target.md) is on staging)

## Inputs

### Working

#### History

[ ] ``EventBody`` in `src/Shared/History.fs` has the cases `Undo of target: EventId * ops: Op list`, `Redo of target: EventId * ops: Op list` and `Load of nodeId: NodeId`, beside `Order of Order`.
[ ] ``Order`` in `src/Shared/History.fs` has `Undo of target: EventId * ops: Op list`, `Redo of target: EventId * ops: Op list` and `Load of nodeId: NodeId`.
[ ] ``Ev.ops``, ``Ev.withOps`` and ``Ev.target`` in `src/Shared/History.fs` each have arms for `EventBody.Undo`, `EventBody.Redo` and `EventBody.Load`.
[ ] The `EventBody` module in `src/Shared/History.fs` has `Change` and `(|Change|_|)` from [108 — Log Change as EventBody.Order](108-log-change-as-eventbody-order.md). Test files kept writing `EventBody.Change` after the `Change` case left.

#### Logging

[ ] A posted Undo or Redo is placed through `placeOrder` and logged as `EventBody.Order` of `Order.Undo` or `Order.Redo`.
[ ] ``replyLoad`` in `src/Server/Core/CoreMailboxLoad.fs` logs `EventBody.Load subject` through `CoreEventDispatch.appendLifecycle`.
[ ] ``historyBody`` in `src/Shared/MailboxLog.fs` maps `Order.Load` to `EventBody.Load`, and `Order.Change`, `Order.Undo` and `Order.Redo` to `EventBody.Order`.
[ ] ``orderOfBody`` in `src/Shared/MailboxLog.fs` maps `EventBody.Undo`, `EventBody.Redo` and `EventBody.Load` to their orders.
[ ] ``postGraphOnly`` in `src/Server/Core/CoreMailboxApi.fs` sends any Ev as `CoreMsg.PostGraphOnly`. ``dispatchPostGraphOnly`` in `src/Server/Core/CoreMailboxEvents.fs` passes it to `CoreEventDispatch.postEvent` unplaced, so an old `EventBody.Undo` or `EventBody.Redo` body can still be stored this way. That path disappears with the cases.

#### JSON

[ ] ``encodeBody`` in `src/Shared/EventJson.fs` writes `EventBody.Undo` and `EventBody.Redo` as `kind` `undo` and `redo` with `target` and `ops`, and `EventBody.Load` as `kind` `load` through `encodeLoad`.
[ ] ``decodeBody`` in `src/Shared/EventJson.fs` reads `kind` `undo`, `redo` and `load` through `decodeUndoBody`, `decodeRedoBody` and an inline `load` decoder.
[ ] ``encodeOrder`` and ``decodeOrder`` in `src/Shared/EventJson.fs` write and read `Order.Undo`, `Order.Redo` and `Order.Load`.
[ ] `EventLogFile.decodeEvent` and the `DbAgent` restore skip an entry that does not decode. `BootCache` fetches `/state` on a decode error.

#### Match sites

[ ] Source sites that name `EventBody.Undo`, `EventBody.Redo` or `EventBody.Load`:
  - `src/Shared/History.fs`: `Ev.ops`, `Ev.withOps`, `Ev.target`
  - `src/Shared/EventJson.fs`: `encodeBody`, `decodeUndoBody`, `decodeRedoBody`, `decodeBody`
  - `src/Shared/MailboxLog.fs`: `orderOfBody`, `historyBody`
  - `src/Shared/ActorLive.fs`: `applyEvent`, `resultOf`
  - `src/Shared/ClientHistory.fs`: `undoEvent`, `redoEvent`, `stampBody`
  - `src/Shared/SyncLogic.fs`: `markUndoRedoVersion`
  - `src/Server/Core/CoreEventDispatch.fs`: `undoRedoOf`, `completeOther`, `pushChangeSpecials`
  - `src/Server/Core/CoreMailboxApi.fs`: `isUndoOrRedo`
  - `src/Server/Core/CoreMailboxLoad.fs`: `replyLoad`
[ ] `undoRedoOf`, `pushChangeSpecials` and `markUndoRedoVersion` already have `Order.Undo` and `Order.Redo` arms beside the old ones.
[ ] 11 test files name `EventBody.Undo`, `EventBody.Redo` or `EventBody.Load`.
[ ] `TreatWarningsAsErrors` is on in `Directory.Build.props`, so any warning fails the build.

#### Tests

[ ] ``Ev JSON round-trips name-only Undo`` in `tests/Shared.Tests/EventJsonTests.fs`.
[ ] ``Undo and Redo keep the target and the ops`` and ``Load keeps its node through EventJson and MailboxLog`` in `tests/Shared.Tests/MailboxLogTests.fs`.
[ ] ``loadNode logs Order.Load`` in `tests/Server.Tests/MailboxExpandTests.fs` asserts the stored body equals `EventBody.Load fileId`.
[ ] ``name-only Undo and Redo store completed Events with submission ids`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs`.
[ ] ``Undo after restart refuses a target from before the restart`` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`.

### Reference

[ ] [Message shape](plan/core-refinement/cqrs-messages.md) §3 decisions 2, 6 and 9, and decision 1 **Logged as placed**.

## Do not load

[ ] `EventBody.ActorStart` and `EventBody.Cancel` stay. Their removal is [110 — Log ActorStart and Cancel as EventBody.Order](110-log-actorstart-and-cancel-as-eventbody-order.md).
[ ] `MailboxEventBody`, `MailboxLog.Ev`, `ofEv` and `toEv` stay. Their removal is [116 — One EventBody type, and MailboxLog.Ev leaves](116-one-eventbody-type-and-mailboxlog-ev-leaves.md).

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] The `EventBody.Undo`, `EventBody.Redo` and `EventBody.Load` cases leave the `EventBody` union.
3. [x] The Server logs a Load as `EventBody.Order` of `Order.Load`.
4. [x] JSON has no `undo`, `redo` or `load` body kind. An old row of those kinds is skipped on read.
5. [x] Test files that write `EventBody.Undo`, `EventBody.Redo` or `EventBody.Load` stay as written, except the ones in Outputs.
6. [x] The build stays warning-free.
7. [x] Add a test that a JSON body with `kind` `undo`, `redo` or `load` fails `EventJson.decode`.

## Outputs

[x] The source files the builder changes, up to 8
[x] `tests/Shared.Tests/EventJsonTests.fs` — the step 7 test

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/109-log-undo-redo-and-load-as-eventbody-order-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts [Message shape](plan/core-refinement/cqrs-messages.md).
[ ] Stop when the edit needs more than 8 source files or 4 test files.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: the step 7 test name written in the test file
  - `tests/Shared.Tests`: ``Ev JSON round-trips name-only Undo``
  - `tests/Shared.Tests`: ``Undo and Redo keep the target and the ops``
  - `tests/Shared.Tests`: ``Load keeps its node through EventJson and MailboxLog``
  - `tests/Server.Tests`: ``loadNode logs Order.Load``
  - `tests/Server.Tests`: ``name-only Undo and Redo store completed Events with submission ids``
  - `tests/Server.Tests`: ``Undo after restart refuses a target from before the restart``
[x] All executed tests pass.
[x] Write the verdict to `plan/core-refinement/reports/109-log-undo-redo-and-load-as-eventbody-order-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
