# 15 — Delete the old Actor starts
**Status**: coded
One job: `CoreMsg.StartPeerActor` is gone, and so is every function that exists only for it. The Git arm of `CoreActorPool.startLoadSaveCommand` is gone. `CoreMsg.StartLoadSaveCommand` stays.

[x] `startFunction` stays.
[x] `CoreMailboxApi.startPeerActor` is deleted. `CoreActorPool.registerPeer` is deleted.
[x] `CoreMsg.StartPeerActor` is deleted from `runMsg` and `messageCaller` in `src/Server/Core/CoreMailboxLoop.fs`, and from `operationContext` and `replyFailure` in `src/Server/Core/CoreMailboxEvents.fs`.
[x] `CoreMailboxActors.dispatchStartPeerActor` is deleted. The case is gone from `src/Server/Core/CoreMsg.fs`. `startPeerActor` is gone from `CoreActorPool` in `src/Server/Core/CoreActorPool.fs`.
[x] `CoreActorPool.startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk` and `LoadSavePath.Git`. It does not read the pool.
[x] `RouteRegistration.registerStateRoutes` sends a Git path to `GithubTransportActor.start`. Production does not reach `CoreActorPool.startLoadSaveCommand` for Git.
[x] ``plain Load of a Workspace still requires the git actor`` and ``explicit git Load of a File still requires the git actor`` are deleted from `tests/Server.Tests/ParseThreadLoadTests.fs`.
[x] `CoreMsg.StartLoadSaveCommand` stays until [20 — Move Desk Load and Desk Save](../20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md).
[x] `Order.StartActorDeprecated` stays until 16 — Delete the old Order cases.
[x] 112 — Place Cancel through PlaceOrder landed on staging in PR 522. The cancel case is `Order.CancelActor`. This ticket does not edit it.

Type:

[x] coding

Blockers:

[x] none

## Inputs

### Working

[x] `startPeerActor` and `registerPeer` are deleted from `CoreActorPool` in `src/Server/Core/CoreActorPool.fs`. `peerDefs` is deleted with them.
[x] `CoreMailboxApi.startPeerActor` is deleted from `src/Server/Core/CoreMailboxApi.fs`.
[x] `CoreMsg` in `src/Server/Core/CoreMsg.fs` is `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `ActorStop`, and `PostEvent`.
[x] `CoreActorPool.startLoadSaveCommand` matches `LoadSavePath.Desk` and `LoadSavePath.Git` and returns `Ok None` for both.

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

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Contract: Delete `CoreMsg.StartPeerActor` and its arms in `CoreMailboxLoop.runMsg`, `CoreMailboxLoop.messageCaller`, `CoreMailboxEvents.operationContext`, and `CoreMailboxEvents.replyFailure`.
3. [x] Contract: Delete `CoreMailboxApi.startPeerActor` and `CoreMailboxActors.dispatchStartPeerActor`.
4. [x] Contract: Delete `CoreActorPool.startPeerActor` and `CoreActorPool.registerPeer`. Leave `startFunction`, `startActor`, and `register`.
5. [x] Contract: Delete the `LoadSavePath.Git` arm of `CoreActorPool.startLoadSaveCommand`. Leave the `LoadSavePath.Desk` arm. Both path cases return `Ok None`. The signature stays.
6. [x] Contract: Delete ``plain Load of a Workspace still requires the git actor`` and ``explicit git Load of a File still requires the git actor`` in `tests/Server.Tests/ParseThreadLoadTests.fs`.
7. [x] Search of `src/` and `tests/` for `startPeerActor` and `StartPeerActor` is empty.
8. [x] `registerPeer` remains as a forbidden name in `tests/Server.Tests/AppGithubTransportBoundaryTests.fs`.
9. [x] `StartLoadSaveCommand` remains.

## Outputs

[x] `src/Server/Core/CoreActorPool.fs`
[x] `src/Server/Core/CoreMsg.fs`
[x] `src/Server/Core/CoreMailboxLoop.fs`
[x] `src/Server/Core/CoreMailboxEvents.fs`
[x] `src/Server/Core/CoreMailboxActors.fs`
[x] `src/Server/Core/CoreMailboxApi.fs`
[x] `tests/Server.Tests/ParseThreadLoadTests.fs`
[x] `tests/Server.Tests/CoreMailboxDoorTests.fs`
[x] `tests/Server.Tests/CoreMsgActorCasesTests.fs`
[x] `tests/Server.Tests/Issue41CoreMailboxTests.fs`
[x] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
[x] `tests/Server.Tests/PersistHandlersRestoreTests.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/15-delete-the-old-actor-starts-stop.md`, then ask.
[ ] Stop when a search hit remains outside Outputs.
[ ] Stop when the edit deletes `startFunction`, `startActor`, or `CoreMsg.StartLoadSaveCommand`.
[ ] Stop when the edit deletes `Order.StartActorDeprecated` or `Order.StartActor`.
[ ] Stop when the edit adds a `CoreMsg` case or an `Order` case.
[x] 112 — Place Cancel through PlaceOrder landed on staging in PR 522.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `ParseThreadLoadTests`
[x] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/15-delete-the-old-actor-starts-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
