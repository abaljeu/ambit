# 20 — Move Desk Load and Desk Save
**Status**: defined
One job: Desk Load and Desk Save leave `CoreMsg.StartLoadSaveCommand`. Desk Load probably uses `Order.Load`. Where Desk Save goes is an open question. The last step deletes `CoreMsg.StartLoadSaveCommand`.

[ ] `Order.Load` of `nodeId` is in `src/Shared/History.fs`. `EventBody.Load` wraps it. `CoreMailboxLoop.placeOrder` runs `CoreMailboxLoad.replyLoad` for that case. `replyLoad` calls `dispatchLoad`.
[ ] The Desk arm of `RouteRegistration.registerStateRoutes` calls `CoreMailboxApi.startLoadSaveCommand`. The Git arm calls `GithubTransportActor.start`.
[ ] `CoreMailboxApi.startLoadSaveCommand` is the builder of `CoreMsg.StartLoadSaveCommand` in `src/Server/Core/CoreMailboxApi.fs`.
[ ] `CoreMailboxLoad.dispatchStartLoadSaveCommand` logs a Desk order in `logDeskOrder` and runs `DeskLoadPush.runLoadSave`. `runLoadSave` loads the subject node. That path is not an actor start.
[ ] `LoadSaveCommandTests.productionRouter` and `LoadSaveCommandTests.routeMatrixRouter` in `tests/Server.Tests/LoadSaveCommandTests.fs` call `CoreMailboxApi.startLoadSaveCommand` for Desk.
[ ] 15 — Delete the old Actor starts does not delete this message. This ticket does not wait for 15.

Type:

[ ] coding

Blockers:

[ ] none

## Inputs

### Working

[ ] `CoreMsg.StartLoadSaveCommand` is in `src/Server/Core/CoreMsg.fs`.
[ ] `runMsg` in `src/Server/Core/CoreMailboxLoop.fs` calls `CoreMailboxLoad.dispatchStartLoadSaveCommand` for that case.
[ ] `logDeskOrder` appends `Order.StartActor` with `EventJson.stubActor` for a Desk path.
[ ] `CoreActorPool.startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk`.

### Reference

[ ] [Arch](../../arch.md).
[ ] `Order.Load` in `src/Shared/History.fs`.

## Do not load

[ ] `CoreMsg.StartPeerActor`, `startPeerActor`, and `registerPeer`. 15 — Delete the old Actor starts deletes them.
[ ] `Order.StartActorDeprecated`. 16 — Delete the old Order cases deletes it.
[ ] A decision about where Desk Save goes. That question stays open.
[ ] A new `CoreMsg` case.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: Desk Load leaves `CoreMsg.StartLoadSaveCommand`. The likely order is `Order.Load` in `src/Shared/History.fs`. `replyLoad` already loads a subject through `dispatchLoad`.
3. [ ] Open: where Desk Save goes. Do not choose an order in this step.
4. [ ] Change: `RouteRegistration.registerStateRoutes` no longer calls `CoreMailboxApi.startLoadSaveCommand` for Desk.
5. [ ] Change: `LoadSaveCommandTests.productionRouter` and `LoadSaveCommandTests.routeMatrixRouter` no longer send `CoreMsg.StartLoadSaveCommand`.
6. [ ] Contract: Delete `CoreMsg.StartLoadSaveCommand`, `CoreMailboxApi.startLoadSaveCommand`, and `CoreMailboxLoad.dispatchStartLoadSaveCommand`. Delete `logDeskOrder` when that handler was its only caller. This is the last step.

## Outputs

[ ] `src/Server/RouteRegistration.fs`
[ ] `src/Server/Core/CoreMsg.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxLoad.fs`
[ ] `src/Shared/History.fs` — only if Desk Save needs an order that is not already there
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/20-move-desk-load-and-save-stop.md`, then ask.
[ ] Stop when the edit chooses where Desk Save goes before that question is answered.
[ ] Stop when the edit deletes `CoreMsg.StartLoadSaveCommand` before Desk Load and Desk Save have left it.
[ ] Stop when the edit deletes `CoreMsg.StartPeerActor`.
[ ] Stop when a send of `StartLoadSaveCommand` remains at the end.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `LoadSaveCommandTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/20-move-desk-load-and-save-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
