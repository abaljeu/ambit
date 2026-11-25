# 20 — Move Desk Load and Desk Save
**Status**: coded
One job: Desktop_mapped callers leave `CoreMsg.StartLoadSaveCommand` and place their work on the order queue that Git Load and Git Save use. The last step deletes that message.

The Neither case is [23 — Run the Load actor for Git and Neither](../23-run-the-load-actor-for-git-and-neither/23-run-the-load-actor-for-git-and-neither-ticket.md). A Save actor is the follow-on for Save.

Alan decided on 2026-11-25 that the desk path moves directly onto the order queue, with no WebDAV redesign first. The Grok Bot AI response channel (`GrokBotSettings.deliverResponseUrl`, `POST /ambit/actors/deliver`) is not part of this ticket.

[x] `Order.Load` of `nodeId` is in `src/Shared/History.fs`. `EventBody.Load` wraps it. `CoreMailboxLoop.placeOrder` runs `CoreMailboxLoad.replyLoad` for that case. `replyLoad` calls `dispatchLoad`.
[x] The Desk arm of `RouteRegistration.registerStateRoutes` reaches `LoadSaveStart.startCommand`, which calls `LoadSaveStart.startDesk` for Desk. `startDesk` places `Order.Load` through `CoreDoor.loadNode` for a Desk Load of a valid subject, and places nothing for Desk Save or a held stack. The Git arm calls `GithubTransportActor.start`.
[x] `CoreMailboxApi.startLoadSaveCommand` is deleted from `src/Server/Core/CoreMailboxApi.fs`.
[x] `CoreMailboxLoad.dispatchStartLoadSaveCommand` is deleted. `DeskLoadPush.runLoadSave` is unchanged and has no caller. `DeskLoadPush.subjectToLoad` is internal, and `LoadSaveStart.startDesk` reuses it. Pull request 589 removed `logDeskOrder`.
[x] `LoadSaveCommandFixtures.actorRouter` (used by `productionRouter` and `routeMatrixRouter` in `tests/Server.Tests/LoadSaveCommandTests.fs`) and the Desk tests in `LoadSaveCommandTests`, `LoadSaveSaveCommandTests`, and `ParseThreadLoadTests` call `LoadSaveStart.startDesk` for Desk.
[x] 15 — Delete the old Actor starts does not delete this message. This ticket does not wait for 15.

Type:

[x] coding

Blockers:

[x] none

## Inputs

### Working

[x] `CoreMsg.StartLoadSaveCommand` was in `src/Server/Core/CoreMsg.fs`. Nothing sends it now.
[x] Pull request 589 removed `logDeskOrder` from `src/Server/Core/CoreMailboxLoad.fs`.
[x] `CoreActorPool.startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk`.

### Reference

[x] [Arch](../../arch.md).
[x] `Order.Load` in `src/Shared/History.fs`.

## Do not load

[x] `CoreMsg.StartPeerActor`, `startPeerActor`, and `registerPeer`. 15 — Delete the old Actor starts deletes them.
[x] `Order.ClientRequestActor`. [16 — Rename the client start request to ClientRequestActor](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) keeps that case.
[x] A new `CoreMsg` case.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] The Neither case stays on 23 — Run the Load actor for Git and Neither. This ticket leaves `Order.Load` unused for that case.
3. [x] The Save actor is the follow-on. This ticket leaves that actor unbuilt.
4. [x] Change: the Desk arm of `RouteRegistration.registerStateRoutes` no longer calls `CoreMailboxApi.startLoadSaveCommand`. It places `Order.Load` for a Desk Load.
5. [x] Change: `LoadSaveCommandTests.productionRouter` and `LoadSaveCommandTests.routeMatrixRouter` no longer send `CoreMsg.StartLoadSaveCommand`.
6. [x] Contract: Delete `CoreMsg.StartLoadSaveCommand`, `CoreMailboxApi.startLoadSaveCommand`, and `CoreMailboxLoad.dispatchStartLoadSaveCommand`. This is the last step. Then `CoreMsg` is empty, so the `CoreMsg` type, `Mailbox.CoreMsg`, `MailboxHost.coreMsgItem`, `MailboxHost.postAndAsyncReply`, `CoreMailboxApi.addCoreMsg`, and the `CoreMailboxLoop` handling are deleted.

## Outputs

[x] `src/Server/LoadSaveStart.fs`
[x] `src/Server/Core/DeskLoadPush.fs` — `subjectToLoad` only
[x] `src/Server/Core/CoreMsg.fs`
[x] `src/Server/Core/CoreMailboxApi.fs`
[x] `src/Server/Core/CoreMailboxLoop.fs`
[x] `src/Server/Core/CoreMailboxLoad.fs`
[x] `src/Server/Core/CoreMailboxEvents.fs`
[x] `src/Server/Core/MailboxHost.fs`
[x] `tests/Server.Tests/LoadSaveCommandTests.fs`
[x] `tests/Server.Tests/LoadSaveCommandFixtures.fs`
[x] `tests/Server.Tests/LoadSaveSaveCommandTests.fs`
[x] `tests/Server.Tests/ParseThreadLoadTests.fs`
[x] `tests/Server.Tests/MailboxExpandTests.fs`
[x] `tests/Server.Tests/CoreMailboxDoorTests.fs`

## Stop when

[x] Record the stop at `plan/actor-as-client/reports/20-move-desk-load-and-save-stop.md`, then ask.
[x] Stop when the edit builds the Save actor.
[x] Stop when the edit deletes `CoreMsg.StartLoadSaveCommand` before the Desktop_mapped callers have left it.
[x] Stop when the edit deletes `CoreMsg.StartPeerActor`.
[x] Stop when a send of `StartLoadSaveCommand` remains at the end.

## Check

[x] Run `scripts/agent-test.sh --full`.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `LoadSaveCommandTests`
[x] All executed tests pass.
[x] The verdict and counts are in [20 — Move Desk Load and Desk Save build](20-build.md).
[x] A reader who reads only the One job line can say what the ticket builds.
