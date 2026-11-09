# 15 — Delete the old Actor starts
**Status**: defined
One job: `CoreMsg.StartPeerActor` is gone, and so is every function that exists only for it. The Git arm of `CoreActorPool.startLoadSaveCommand` is gone. `CoreMsg.StartLoadSaveCommand` stays.

[ ] `startFunction` stays.
[ ] `CoreMailboxApi.startPeerActor` has no callers. `CoreActorPool.registerPeer` has no callers.
[ ] `CoreMsg.StartPeerActor` is matched in `runMsg` and `messageCaller` in `src/Server/Core/CoreMailboxLoop.fs`, and in `operationContext` and `replyFailure` in `src/Server/Core/CoreMailboxEvents.fs`.
[ ] `CoreMailboxActors.dispatchStartPeerActor` is the handler. The case is in `src/Server/Core/CoreMsg.fs`. `startPeerActor` is a field of `CoreActorPool` in `src/Server/Core/CoreActorPool.fs`.
[ ] The Git arm of `CoreActorPool.startLoadSaveCommand` calls `run.pool.startPeerActor`. The Desk arm returns `Ok None`.
[ ] `RouteRegistration.registerStateRoutes` sends a Git path to `GithubTransportActor.start`. Production does not reach the Git arm.
[ ] Two tests in `tests/Server.Tests/ParseThreadLoadTests.fs` reach that Git arm: ``plain Load of a Workspace still requires the git actor`` and ``explicit git Load of a File still requires the git actor``.
[ ] `CoreMsg.StartLoadSaveCommand` stays until [20 — Move Desk Load and Desk Save](../20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md).
[ ] `Order.StartActorDeprecated` stays until 16 — Delete the old Order cases.
[x] 112 — Place Cancel through PlaceOrder landed on staging in PR 522. The cancel case is `Order.CancelActor`. This ticket does not edit it.

Type:

[ ] coding

Blockers:

[ ] none

## Inputs

### Working

[ ] `startPeerActor` and `registerPeer` are on `CoreActorPool` in `src/Server/Core/CoreActorPool.fs`.
[ ] `CoreMailboxApi.startPeerActor` in `src/Server/Core/CoreMailboxApi.fs` is the only builder of `CoreMsg.StartPeerActor`.
[ ] `CoreMsg` in `src/Server/Core/CoreMsg.fs` is `StartPeerActor`, `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `ActorStop`, and `PostEvent`.
[ ] The Git arm is `LoadSavePath.Git` in `CoreActorPool.startLoadSaveCommand`.

### Reference

[ ] [Arch](../../arch.md).
[ ] [Size limit](../../context/size-limit.md).

## Do not load

[ ] `startFunction`. It is the target start.
[ ] `startActor` and `register`. They stay.
[ ] `CoreMsg.StartLoadSaveCommand`, `CoreMailboxApi.startLoadSaveCommand`, `CoreMailboxLoad.dispatchStartLoadSaveCommand`, and `CoreMailboxLoad.logDeskOrder`. 20 — Move Desk Load and Desk Save deletes the message at its end.
[ ] `Order.StartActorDeprecated` and `Order.StartActor` of `ActorOrder`. 16 — Delete the old Order cases deletes `Order.StartActorDeprecated` and leaves `Order.StartActor`.
[ ] `CoreMsg.RecordSearchStart` and `CoreEventDispatch.actorStart`. 09 — Start the query command deletes them.
[ ] A new `CoreMsg` case.
[ ] `ActorDispatch.fs`. Leave `Order` as the cases in `src/Shared/History.fs`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Contract: Delete `CoreMsg.StartPeerActor` and its arms in `CoreMailboxLoop.runMsg`, `CoreMailboxLoop.messageCaller`, `CoreMailboxEvents.operationContext`, and `CoreMailboxEvents.replyFailure`.
3. [ ] Contract: Delete `CoreMailboxApi.startPeerActor` and `CoreMailboxActors.dispatchStartPeerActor`.
4. [ ] Contract: Delete `CoreActorPool.startPeerActor` and `CoreActorPool.registerPeer`. Leave `startFunction`, `startActor`, and `register`.
5. [ ] Contract: Delete the `LoadSavePath.Git` arm of `CoreActorPool.startLoadSaveCommand`. Leave the `LoadSavePath.Desk` arm.
6. [ ] Contract: Delete ``plain Load of a Workspace still requires the git actor`` and ``explicit git Load of a File still requires the git actor`` in `tests/Server.Tests/ParseThreadLoadTests.fs`.
7. [ ] Search `src/` and `tests/` for `startPeerActor`, `registerPeer`, and `StartPeerActor`. The search is empty. `StartLoadSaveCommand` remains.

## Outputs

[ ] `src/Server/Core/CoreActorPool.fs`
[ ] `src/Server/Core/CoreMsg.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxEvents.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `tests/Server.Tests/ParseThreadLoadTests.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/15-delete-the-old-actor-starts-stop.md`, then ask.
[ ] Stop when a search hit remains outside Outputs.
[ ] Stop when the edit deletes `startFunction`, `startActor`, or `CoreMsg.StartLoadSaveCommand`.
[ ] Stop when the edit deletes `Order.StartActorDeprecated` or `Order.StartActor`.
[ ] Stop when the edit adds a `CoreMsg` case or an `Order` case.
[x] 112 — Place Cancel through PlaceOrder landed on staging in PR 522.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `ParseThreadLoadTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/15-delete-the-old-actor-starts-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
