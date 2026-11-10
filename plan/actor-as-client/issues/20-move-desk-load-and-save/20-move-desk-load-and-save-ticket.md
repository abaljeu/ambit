# 20 — Move Desk Load and Desk Save
**Status**: defined
One job: Desktop_mapped callers leave `CoreMsg.StartLoadSaveCommand` after the Git case and the Neither case are straight. The last step deletes that message.

The Neither case is [23 — Run the Load actor for Git and Neither](../23-run-the-load-actor-for-git-and-neither/23-run-the-load-actor-for-git-and-neither-ticket.md). A Save actor is the follow-on for Save.

WebDAV needs its own design effort before it is built. That design also considers the Grok Bot AI response channel. `GrokBotSettings.deliverResponseUrl` in `src/Server/GrokBotSettings.fs` names `POST /ambit/actors/deliver`, `RouteRegistration` maps that route, and `RunAgentActor` in `src/Server/RunAgentActor.fs` sends the address as `ResponseUrl`. One part of the design is the desktop sending material to the server. `loadActorFunction` and `saveActorFunction` in `src/Server/GithubTransportActor.fs` pull and push when a person runs Load or Save, and the design has to place a desktop send beside that request style.

[ ] `Order.Load` of `nodeId` is in `src/Shared/History.fs`. `EventBody.Load` wraps it. `CoreMailboxLoop.placeOrder` runs `CoreMailboxLoad.replyLoad` for that case. `replyLoad` calls `dispatchLoad`.
[ ] The Desk arm of `RouteRegistration.registerStateRoutes` calls `CoreMailboxApi.startLoadSaveCommand`. The Git arm calls `GithubTransportActor.start`.
[ ] `CoreMailboxApi.startLoadSaveCommand` is the builder of `CoreMsg.StartLoadSaveCommand` in `src/Server/Core/CoreMailboxApi.fs`.
[ ] `CoreMailboxLoad.dispatchStartLoadSaveCommand` runs `DeskLoadPush.runLoadSave`. `runLoadSave` loads the subject node. Pull request 589 removed `logDeskOrder`.
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
[ ] Pull request 589 removed `logDeskOrder` from `src/Server/Core/CoreMailboxLoad.fs`.
[ ] `CoreActorPool.startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk`.

### Reference

[ ] [Arch](../../arch.md).
[ ] `Order.Load` in `src/Shared/History.fs`.

## Do not load

[ ] `CoreMsg.StartPeerActor`, `startPeerActor`, and `registerPeer`. 15 — Delete the old Actor starts deletes them.
[ ] `Order.StartActorDeprecated`. 16 — Delete the old Order cases deletes it.
[ ] The WebDAV branch. The design above comes first.
[ ] A new `CoreMsg` case.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] The Neither case stays on 23 — Run the Load actor for Git and Neither. This ticket leaves `Order.Load` unused for that case.
3. [ ] The Save actor is the follow-on. This ticket leaves that actor unbuilt. The WebDAV design stays open.
4. [ ] Change: `RouteRegistration.registerStateRoutes` no longer calls `CoreMailboxApi.startLoadSaveCommand` for Desk.
5. [ ] Change: `LoadSaveCommandTests.productionRouter` and `LoadSaveCommandTests.routeMatrixRouter` no longer send `CoreMsg.StartLoadSaveCommand`.
6. [ ] Contract: Delete `CoreMsg.StartLoadSaveCommand`, `CoreMailboxApi.startLoadSaveCommand`, and `CoreMailboxLoad.dispatchStartLoadSaveCommand`. This is the last step.

## Outputs

[ ] `src/Server/RouteRegistration.fs`
[ ] `src/Server/Core/CoreMsg.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxLoad.fs`
[ ] `src/Shared/History.fs` — only when the WebDAV design names an order that is absent
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/20-move-desk-load-and-save-stop.md`, then ask.
[ ] Stop when the edit builds the WebDAV branch or the Save actor.
[ ] Stop when the edit deletes `CoreMsg.StartLoadSaveCommand` before the Desktop_mapped callers have left it.
[ ] Stop when the edit deletes `CoreMsg.StartPeerActor`.
[ ] Stop when a send of `StartLoadSaveCommand` remains at the end.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `LoadSaveCommandTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/20-move-desk-load-and-save-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
