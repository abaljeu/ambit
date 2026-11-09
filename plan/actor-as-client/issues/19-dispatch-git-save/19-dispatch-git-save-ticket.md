# 19 — Start git Save
**Status**: defined
One job: `postLoadSaveCommand` builds the git Save `ActorFunction` and puts `Order.ActorOrder` on the mailbox queue. A Desk Save runs no Actor.

The Git Save post is already in the code. Keep that post.

[x] `RouteRegistration.registerStateRoutes` calls `GithubTransportActor.start` when the path is Git.
[x] `GithubTransportActor.start` calls `saveActorFunction` for Save.
[x] `GithubTransportActor.start` calls `CoreMailboxApi.coreMailboxStartActorFromFunction`.
[x] `ActorOrderFromStart.orderFromPlacement` builds the `ActorOrder`.
[x] `MailboxHost.postActorOrder` posts `Order.StartActor`.
[x] `CoreMailboxLoop.scheduleActorOrder` runs that order through `CoreActorPool.startFunction`.
[x] `GithubTransportActor.transportActorName` returns `github-save` for Save.
[x] `CoreActorPool.startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk`.

This remains.

[ ] `CoreMailboxApi.startLoadSaveCommand` is the only builder of `CoreMsg.StartLoadSaveCommand`.
[ ] `RouteRegistration.registerStateRoutes` calls that builder when the path is Desk.
[ ] `LoadSaveCommandTests.productionRouter` calls that builder when the path is Desk.
[ ] `LoadSaveCommandTests.routeMatrixRouter` calls that builder when the path is Desk.
[ ] 10 — Start git Load still has Load sends of the same message.

[ ] This ticket converts every remaining Save caller of `CoreMsg.StartLoadSaveCommand`.
[ ] After this ticket and 10 — Start git Load, nothing sends `StartLoadSaveCommand`.
[ ] 15 — Delete the old Actor starts deletes the message.
[ ] The source files are 3. The test files are 2. The size limit is 8 source files and 4 test files.

Type:

[ ] coding

Blockers:

[ ] [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)
[ ] [10 — Start git Load](../10-dispatch-git-load-and-save/10-dispatch-git-load-and-save-ticket.md)

## Inputs

### Working

[ ] `postLoadSaveCommand` is in `src/Server/Api.fs`. The route is `MapPost "/ambit/load-save-command"` in `src/Server/RouteRegistration.fs`.
[ ] `RouteRegistration.fs` calls `pool.registerPeer` for `github-save` as `GithubTransportActor.actorFn`. 10 — Start git Load removes the `github-load` call.
[ ] `GithubTransportActor.actorFn` is an `ActorFn`. `GithubTransportActor.start` for Save posts `CoreMsg.StartPeerActor`.
[ ] `CoreMailboxApi.startLoadSaveCommand` is the only send of `CoreMsg.StartLoadSaveCommand`.
[ ] Save callers of that send are `src/Server/RouteRegistration.fs` and `tests/Server.Tests/LoadSaveCommandTests.fs`.

### Reference

[ ] [Arch](../../arch.md). `postLoadSaveCommand` is the git Save handler.

## Do not load

[ ] Do not delete `registerPeer`, `startPeerActor`, or the `CoreMsg.StartLoadSaveCommand` case. 15 — Delete the old Actor starts deletes them.
[ ] Find and Move `recordSearchStart`.
[ ] `DeskLoadPush.fs`.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.
[ ] Do not write `Order.StartActor`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: `postLoadSaveCommand` builds the git Save `ActorFunction` and puts `Order.ActorOrder` on the mailbox queue.
3. [ ] The closure holds the path and the command node. The Desk Save arm runs no Actor and does not call `postActorOrder`.
4. [ ] Change: `GithubTransportActor.start` for Save uses that same `ActorOrder`. It does not post `CoreMsg.StartPeerActor` for Save.
5. [ ] Contract: Remove the `github-save` `registerPeer` call.
6. [ ] Change: Convert every remaining Save caller of `CoreMsg.StartLoadSaveCommand`. After this step and 10 — Start git Load, nothing sends `StartLoadSaveCommand`.
7. [ ] Do not write `Order.StartActor` or `Order.RunActor`.

## Outputs

[ ] `src/Server/Api.fs`
[ ] `src/Server/GithubTransportActor.fs`
[ ] `src/Server/RouteRegistration.fs`
[ ] `tests/Server.Tests/GithubTransportActorTests.fs` — Save calls of `GithubTransportActor.start`
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs` — Save callers of `startLoadSaveCommand`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/19-dispatch-git-save-stop.md`, then ask.
[ ] Stop when Desk Save must start an Actor.
[ ] Stop when a test file outside Outputs must change.
[ ] Stop when this ticket writes `Order.StartActor`.
[ ] Stop when a send of `StartLoadSaveCommand` remains.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `GithubTransportActorTests`
  - `tests/Server.Tests`: `LoadSaveCommandTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/19-dispatch-git-save-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
