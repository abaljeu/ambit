# 06 — Add the function-and-focus start
**Status**: defined
One job: The Server accepts a start whose arguments are a function and a FocusId, beside the old starts.

[ ] Callers of `startActor`, `startPeerActor`, and `startFunction` stay on those starts.

Type:

[ ] coding

Blockers:

[ ] none

## Inputs

### Working

[ ] `CoreActorPool` in `src/Server/Core/CoreActorPool.fs` has `register`, `registerPeer`, `startActor`, `startFunction`, and `startPeerActor`.
[ ] `FunctionStart` carries `actor`, `getGraph`, `focusId`, and `commandId`. `FunctionActor` is `FunctionRun -> CoreChangesDoor -> Async<unit>`.
[ ] `runSchedule` runs `FunctionPending` with `Async.Start` on the row cancellation token.
[ ] `putFunctionStart` mints `sessionId` with `Guid.NewGuid` and stores it on the live row.
[ ] `CoreMsg` in `src/Server/Core/CoreMsg.fs` has `StartActor`, `StartPeerActor`, and `StartLoadSaveCommand`.
[ ] Those three cases are matched in `src/Server/Core/CoreMailboxLoop.fs` (`dispatch`, `messageCaller`) and `src/Server/Core/CoreMailboxEvents.fs` (`operationContext`, `replyFailure`).
[ ] `Order` in `src/Shared/History.fs` has `StartActor` and `StartPeerActor`.
[ ] `Order` is matched in `src/Shared/EventJson.fs` (`encodeOrder`, `decodeOrder`), `src/Shared/MailboxLog.fs`, and `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs`.
[ ] Hand-written `CoreActorPool` literals set `startFunction` in `tests/Server.Tests/Issue41CoreMailboxTests.fs`, `Issue42PersistHandlersTests.fs`, `MailboxHistoryDurabilityTests.fs`, `PersistHandlersRestoreTests.fs`, `CoreMailboxDoorTests.fs`, and `CoreMsgActorCasesTests.fs`.

### Reference

[ ] [Actor as client](../../project.md) decisions of 2026-11-03.
[ ] [Size limit](../../context/size-limit.md): 8 source files, 4 test files. This ticket is over that limit. A new `Order` case and a new `CoreMsg` case are exhaustive, and every `CoreActorPool` literal must gain the new field in the same change. The expand is one ticket so the tree still builds.

## Do not load

[ ] Do not move a caller off `startActor` or `startPeerActor`. Later tickets do that.
[ ] Do not delete a name table, `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, or `Order.StartPeerActor`.
[ ] Closed: `src/Client`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand `CoreActorPool` with a start whose arguments are the function and the FocusId. Leave `startActor`, `startPeerActor`, `startFunction`, `register`, and `registerPeer`.
3. [ ] The running call copies `runSchedule`: `Async.Start` on the row token, and the pool passes `CoreChangesDoor`. Secret, `getGraph`, graph ids, the session id, and the command node are not parameters of this start.
4. [ ] Expand `CoreMsg` with a case beside `StartActor` and `StartPeerActor`. The payload is the function and the FocusId, plus the caller and the reply the other start cases use.
5. [ ] Add the match arms in `CoreMailboxLoop.fs` and `CoreMailboxEvents.fs`. The arm calls the new pool start.
6. [ ] `CoreMailboxApi` posts the new `CoreMsg`. The old `startActor` and `startPeerActor` functions stay.
7. [ ] Expand `Order` with a case beside `StartActor` and `StartPeerActor`. Add the `EventJson` and `MailboxLog` arms, and the `placeOrder` arm. `placeOrder` stays unsupported for this case. [80 — Place Actor starts through PlaceOrder](../../../core-refinement/issues/80-place-actor-orders-through-placeorder.md) places it.
8. [ ] Give each hand-written `CoreActorPool` literal the new field.

## Outputs

[ ] `src/Server/Core/CoreActorPool.fs`
[ ] `src/Server/Core/CoreMsg.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxEvents.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Shared/History.fs`
[ ] `src/Shared/EventJson.fs`
[ ] `src/Shared/MailboxLog.fs`
[ ] The six literal test files named in Inputs
[ ] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` — `function and focus start runs the function`
[ ] Nine source files. Over the size limit, for the reason in Reference.

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/06-add-the-function-and-focus-start-stop.md`, then ask.
[ ] Stop when the function parameter list is not `CoreChangesDoor -> Async<unit>`. Alan named the start arguments. He did not name this parameter list. It is copied from `runSchedule`.
[ ] Stop when the new `Order` must store the function. A function is not a log value.
[ ] Stop when the edit needs a file outside Inputs.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``function and focus start runs the function``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/06-add-the-function-and-focus-start-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
