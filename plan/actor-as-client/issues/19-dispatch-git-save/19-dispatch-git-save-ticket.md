# 19 — Start git Save
**Status**: defined
One job: `GithubTransportActor.start` builds the git Save `ActorFunction` in `saveActorFunction` and puts `Order.StartActor` of `ActorOrder` on the mailbox queue. `postLoadSaveCommand` reaches that post through `router.startCommand`. A Desk Save runs no Actor.

The Git Save post is already in the code. Keep that post.

[x] `RouteRegistration.registerStateRoutes` calls `GithubTransportActor.start` when the path is Git.
[x] `GithubTransportActor.start` calls `saveActorFunction` for Save.
[x] `GithubTransportActor.start` calls `CoreMailboxApi.coreMailboxStartActorFromFunction`.
[x] `ActorOrderFromStart.orderFromPlacement` builds the `ActorOrder`.
[x] `MailboxHost.postActorOrder` posts `Order.StartActor`.
[x] `CoreMailboxLoop.scheduleActorOrder` runs that order through `CoreActorPool.startFunction`.
[x] `GithubTransportActor.transportActorName` returns `github-save` for Save.
[x] `CoreActorPool.startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk` and for `LoadSavePath.Git`.
[x] [15 — Delete the old Actor starts](../15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md) deleted `startPeerActor` and `registerPeer` from `CoreActorPool` in `src/Server/Core/CoreActorPool.fs`.

This remains.

[ ] `CoreMailboxApi.startLoadSaveCommand` is the only builder of `CoreMsg.StartLoadSaveCommand`.
[ ] `RouteRegistration.registerStateRoutes` calls that builder when the path is Desk.
[ ] `LoadSaveCommandTests.productionRouter` calls that builder when the path is Desk.
[ ] `LoadSaveCommandTests.routeMatrixRouter` calls that builder when the path is Desk.
[ ] 10 — Start git Load still has Load sends of the same message. A Desk Save send stays until 20 — Move Desk Load and Desk Save.

[ ] This ticket keeps the Git Save post. A Git Save caller uses `GithubTransportActor.start`.
[ ] A Desk Save send of `CoreMsg.StartLoadSaveCommand` stays until 20 — Move Desk Load and Desk Save.
[ ] [20 — Move Desk Load and Desk Save](../20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md) moves Desk off the message and deletes `CoreMsg.StartLoadSaveCommand` at the end.
[ ] The Git Save post and the Desk send already match this ticket. The edit count is 0 while those inputs match. The size limit is 8 source files and 4 test files.

Type:

[ ] coding

Blockers:

[ ] [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)
[ ] [10 — Start git Load](../10-dispatch-git-load-and-save/10-dispatch-git-load-and-save-ticket.md)

## Inputs

### Working

[ ] `postLoadSaveCommand` is in `src/Server/Api.fs`. It calls `router.startCommand`. The route is `MapPost "/ambit/load-save-command"` in `src/Server/RouteRegistration.fs`.
[ ] The Git arm of `RouteRegistration.registerStateRoutes` calls `GithubTransportActor.start`. The Desk arm calls `CoreMailboxApi.startLoadSaveCommand`.
[ ] `GithubTransportActor.saveActorFunction` builds the Save `ActorFunction`. `GithubTransportActor.start` for Save calls `CoreMailboxApi.coreMailboxStartActorFromFunction`, which places `Order.StartActor`.
[ ] `CoreActorPool.startLoadSaveCommand` in `src/Server/Core/CoreActorPool.fs` returns `Ok None` for `LoadSavePath.Desk` and for `LoadSavePath.Git`.
[ ] `CoreMailboxApi.startLoadSaveCommand` is the only send of `CoreMsg.StartLoadSaveCommand`.
[ ] Save callers of that send are the Desk arm of `src/Server/RouteRegistration.fs` and the Desk arms of `LoadSaveCommandTests.productionRouter` and `LoadSaveCommandTests.routeMatrixRouter` in `tests/Server.Tests/LoadSaveCommandTests.fs`.

### Reference

[ ] [Arch](../../arch.md) **Load and save**. `GithubTransportActor.start` is the Git Save post. `postLoadSaveCommand` calls the route's `startCommand`.

## Do not load

[ ] `startPeerActor` and `registerPeer` stay deleted. 20 — Move Desk Load and Desk Save deletes `CoreMsg.StartLoadSaveCommand`.
[ ] Find and Move `recordSearchStart`.
[ ] `DeskLoadPush.fs`.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.
[ ] Do not write `Order.StartActorDeprecated`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Keep: `GithubTransportActor.start` for Save calls `saveActorFunction` and `CoreMailboxApi.coreMailboxStartActorFromFunction`. `postLoadSaveCommand` reaches that post through `router.startCommand`.
3. [ ] Keep: the Desk arm of `RouteRegistration.registerStateRoutes` calls `CoreMailboxApi.startLoadSaveCommand`. `CoreActorPool.startLoadSaveCommand` returns `Ok None`. That Desk Save runs no Actor.
4. [ ] Keep: `LoadSaveCommandTests.productionRouter` and `LoadSaveCommandTests.routeMatrixRouter` call `GithubTransportActor.start` when the path is Git and `CoreMailboxApi.startLoadSaveCommand` when the path is Desk.
5. [ ] Keep: `src/Server/RouteRegistration.fs` has no `registerPeer` call. `startPeerActor` and `registerPeer` stay deleted.
6. [ ] A Desk send of `CoreMsg.StartLoadSaveCommand` stays until 20 — Move Desk Load and Desk Save.
7. [ ] Leave `Order` as the cases in `src/Shared/History.fs`. `Order.StartActor` of `ActorOrder` stays the Git Save post.

## Outputs

[ ] No source file changes while the Working inputs match.
[ ] `tests/Server.Tests/GithubTransportActorTests.fs` stays the Save check of `GithubTransportActor.start`.
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs` stays the Desk Save check of `startLoadSaveCommand`.

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/19-dispatch-git-save-stop.md`, then ask.
[ ] Stop when Desk Save must start an Actor.
[ ] Stop when a test file outside Outputs must change.
[ ] Stop when this ticket writes `Order.StartActorDeprecated`.
[ ] Stop when a Git Save send of `StartLoadSaveCommand` remains. A Desk send stays for 20 — Move Desk Load and Desk Save.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `GithubTransportActorTests`
  - `tests/Server.Tests`: `LoadSaveCommandTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/19-dispatch-git-save-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
