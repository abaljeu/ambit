# 110 — Log ActorStart and Cancel as EventBody.Order
**Status**: `defined`
One job: The `EventBody.ActorStart` and `EventBody.Cancel` cases leave History with their `actorStart` and `cancel` body kinds. The Server logs each Actor start as `EventBody.Order` of an Order that holds its `ActorStart`, and the Browser sends a Start or a Cancel as `EventBody.Order`. The reason is decisions 3, 7 and 9 in [Message shape](plan/core-refinement/cqrs-messages.md): `Order.Cancel` replaces the old History `EventBody.Cancel`, and the old cases leave one at a time.

Type: coding
Blockers: [109 — Log Undo, Redo, and Load as EventBody.Order](109-log-undo-redo-and-load-as-eventbody-order.md)

## Inputs

### Working

#### History

[ ] ``EventBody`` in `src/Shared/History.fs` has `ActorStart of ActorStart` and `Cancel of focusId: NodeId`.
[ ] ``Order`` in `src/Shared/History.fs` has `StartActor of ActorStart`, `StartPeerActor of peerName * request: ActorStart`, `LoadSave of path * peerName * request: LoadSaveCommandRequest`, and `Cancel of focusId: NodeId`.
[ ] ``Ev.ops``, ``Ev.withOps`` and ``Ev.target`` in `src/Shared/History.fs` each have an `EventBody.ActorStart _ | EventBody.Cancel _` arm.

#### Logging an Actor start

[ ] ``finishReadyStart`` in `src/Server/Core/CoreMailboxActors.fs` logs `EventBody.ActorStart request` through `CoreEventDispatch.appendLifecycle`. `dispatchActorStartResult` and `postActorStart` call it.
[ ] ``recordFailedStart`` in `src/Server/Core/CoreMailboxActors.fs` logs `EventBody.ActorStart request`, then `EventBody.ActorStop` with `ActorFailed`.
[ ] ``actorStart`` in `src/Server/Core/CoreEventDispatch.fs` logs `EventBody.ActorStart request` for `CoreMsg.RecordSearchStart`.
[ ] Actor starts reach the mailbox as `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, `CoreMsg.StartLoadSaveCommand` and `CoreMsg.RecordSearchStart`, or as a posted `EventBody.ActorStart`. None is placed through `placeOrder`.
[ ] ``placeOrder`` in `src/Server/Core/CoreMailboxLoop.fs` replies unsupported for `Order.StartActor`, `Order.StartPeerActor`, `Order.LoadSave` and `Order.Cancel`.
[ ] ``historyBody`` in `src/Shared/MailboxLog.fs` maps `Order.StartActor` to `EventBody.ActorStart` and `Order.Cancel` to `EventBody.Cancel`. ``orderOfBody`` maps them back.

#### Cancel

[ ] No Server path logs `EventBody.Cancel`. ``runCancel`` in `src/Server/Core/CoreMailboxActors.fs` logs `EventBody.ActorStop(focusId, ActorCancelled)` when the focus is live.
[ ] ``queueStart`` and ``queueCancel`` in `src/Shared/RunLaunch.fs` post `EventBody.ActorStart` and `EventBody.Cancel` from the Browser.
[ ] ``dispatchPostEvent`` in `src/Server/Core/CoreMailboxActors.fs` sends a posted `EventBody.ActorStart` to `postActorStart` and a posted `EventBody.Cancel` to `postCancel`.

#### JSON

[ ] ``encodeBody`` in `src/Shared/EventJson.fs` writes `EventBody.ActorStart` through `encodeActorStart` and `EventBody.Cancel` through `encodeCancel`.
[ ] ``decodeBody`` in `src/Shared/EventJson.fs` reads `kind` `actorStart` through `decodeStartRequest` and `kind` `cancel` inline.
[ ] ``encodeStartRequest`` and ``decodeStartRequest`` in `src/Shared/EventJson.fs` are also used by the order codec. ``encodeCancel`` is shared by `encodeBody` and `encodeOrder`.
[ ] `EventLogFile.decodeEvent` and the `DbAgent` restore skip an entry that does not decode. `BootCache` fetches `/state` on a decode error.

#### Match sites

[ ] Source sites that name `EventBody.ActorStart` or `EventBody.Cancel`:
  - `src/Shared/History.fs`: `Ev.ops`, `Ev.withOps`, `Ev.target`
  - `src/Shared/EventJson.fs`: `encodeBody`, `decodeBody`
  - `src/Shared/MailboxLog.fs`: `orderOfBody`, `historyBody`
  - `src/Shared/ActorLive.fs`: `applyEvent`, `resultOf`
  - `src/Shared/RunLaunch.fs`: `queueStart`, `queueCancel`
  - `src/Server/Core/CoreEventDispatch.fs`: `actorStart`, `completeOther`
  - `src/Server/Core/CoreMailboxActors.fs`: `finishReadyStart`, `recordFailedStart`, `dispatchPostEvent`
  - `src/Server/Core/CoreMailboxApi.fs`: `expandStored`
[ ] 20 test files name `EventBody.ActorStart` or `EventBody.Cancel`.
[ ] `TreatWarningsAsErrors` is on in `Directory.Build.props`, so any warning fails the build.

#### Tests

[ ] ``Ev JSON round-trips ActorStart and ActorStop`` and ``cancel request JSON round-trips Focus NodeId and EventId`` in `tests/Shared.Tests/EventJsonTests.fs`.
[ ] ``ActorStart adds and ActorStop removes a live Focus`` in `tests/Shared.Tests/ActorLiveTests.fs`.
[ ] ``applyServerTail ActorStart adds and ActorStop removes a live Focus`` in `tests/Shared.Tests/SyncLogicTests.fs`.
[ ] ``CoreMailboxApi.startActor appends ActorStart to lifecycle events`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`.
[ ] `tests/Server.Tests/CancelByFocusTests.fs`, every test.

### Reference

[ ] [Message shape](plan/core-refinement/cqrs-messages.md) §3 decisions 3, 4, 5, 7, 9 and 11, and decision 1 **Logged as placed**.

## Do not load

[ ] `EventBody.ActorStop` stays. Its removal is [81 — Log Actor and search stops as Notices](81-log-actor-and-search-stops-as-notices.md).
[ ] Placing Actor starts through `placeOrder` is [80 — Place Actor starts through PlaceOrder](80-place-actor-orders-through-placeorder.md). Placing Cancel through `placeOrder` is [112 — Place Cancel through PlaceOrder](112-place-cancel-through-placeorder.md).
[ ] `MailboxEventBody`, `MailboxLog.Ev`, `ofEv` and `toEv` stay. Their removal is [116 — One EventBody type, and MailboxLog.Ev leaves](116-one-eventbody-type-and-mailboxlog-ev-leaves.md).

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] The `EventBody.ActorStart` and `EventBody.Cancel` cases leave the `EventBody` union.
3. [ ] The Server logs each Actor start as `EventBody.Order` of an Order that holds its `ActorStart`.
4. [ ] The Browser posts a Start or a Cancel as `EventBody.Order`, and the Server still runs it as today.
5. [ ] JSON has no `actorStart` or `cancel` body kind. An old row of those kinds is skipped on read.
6. [ ] Test files that write `EventBody.ActorStart` or `EventBody.Cancel` stay as written, except the ones in Outputs.
7. [ ] The build stays warning-free.
8. [ ] Add a test that a JSON body with `kind` `actorStart` or `cancel` fails `EventJson.decode`.

## Outputs

[ ] The source files the builder changes, up to 8
[ ] `tests/Shared.Tests/EventJsonTests.fs` — the step 8 test

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/110-log-actorstart-and-cancel-as-eventbody-order-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts [Message shape](plan/core-refinement/cqrs-messages.md).
[ ] Stop when the edit needs more than 8 source files or 4 test files.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: the step 8 test name written in the test file
  - `tests/Shared.Tests`: ``Ev JSON round-trips ActorStart and ActorStop``
  - `tests/Shared.Tests`: ``cancel request JSON round-trips Focus NodeId and EventId``
  - `tests/Shared.Tests`: ``ActorStart adds and ActorStop removes a live Focus``
  - `tests/Shared.Tests`: ``applyServerTail ActorStart adds and ActorStop removes a live Focus``
  - `tests/Server.Tests`: ``CoreMailboxApi.startActor appends ActorStart to lifecycle events``
  - `tests/Server.Tests`: `CancelByFocusTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/110-log-actorstart-and-cancel-as-eventbody-order-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
