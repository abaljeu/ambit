# 10 — Start git Load
**Status**: defined
One job: `GithubTransportActor.start` builds `ActorOrder` for Git Load (`loadActorFunction`, `actorNodeId`, `actorName`) and posts `Order.StartActor`. `postLoadSaveCommand` reaches that post through `router.startCommand`. A Desk Load runs no Actor. `ActorOrder` in `src/Shared/History.fs` is `actor`, `actorNodeId`, and `actorName`.

The Git Load post is already in the code. Keep that post.

[x] `RouteRegistration.registerStateRoutes` calls `GithubTransportActor.start` when the path is Git.
[x] `GithubTransportActor.start` calls `loadActorFunction` for Load.
[x] `GithubTransportActor.start` calls `CoreMailboxApi.coreMailboxStartActorFromFunction`.
[x] `ActorOrderFromStart.orderFromPlacement` builds the `ActorOrder`.
[x] `MailboxHost.postActorOrder` posts `Order.StartActor`.
[x] `CoreMailboxLoop.scheduleActorOrder` runs that order through `CoreActorPool.startFunction`.
[x] `GithubTransportActor.transportActorName` returns `github-load` for Load.
[x] `CoreActorPool.startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk` and for `LoadSavePath.Git`.
[x] [15 — Delete the old Actor starts](../15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md) deleted `startPeerActor` and `registerPeer` from `CoreActorPool` in `src/Server/Core/CoreActorPool.fs`.

This remains.

[ ] `CoreMailboxApi.startLoadSaveCommand` is the only builder of `CoreMsg.StartLoadSaveCommand`.
[ ] `RouteRegistration.registerStateRoutes` calls that builder when the path is Desk.
[ ] ``plain Load of a File or Directory ignores a Git path`` in `tests/Server.Tests/ParseThreadLoadTests.fs` calls that builder with `LoadSavePath.Git`.
[ ] `DeskLoadPush.runLoadSave` calls `PathPick.effectiveLoadPath`. That plain Load becomes Desk inside the handler.
[ ] Desk Load tests in `tests/Server.Tests/ParseThreadLoadTests.fs` call the builder.
[ ] Desk Load tests in `tests/Server.Tests/LoadSaveCommandTests.fs` call the builder.

[ ] This ticket keeps the Git Load post. Save callers of `CoreMsg.StartLoadSaveCommand` stay until [19 — Start git Save](../19-dispatch-git-save/19-dispatch-git-save-ticket.md).
[ ] [20 — Move Desk Load and Desk Save](../20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md) moves Desk off the message and deletes `CoreMsg.StartLoadSaveCommand` at the end.
[ ] The Git Load post and the Desk send already match this ticket. The edit count is 0 while those inputs match. The size limit is 8 source files and 4 test files.

Type:

[ ] coding

Blockers:

[ ] [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `postLoadSaveCommand` is in `src/Server/Api.fs`. It calls `router.startCommand`. The route is `MapPost "/ambit/load-save-command"` in `src/Server/RouteRegistration.fs`.
[ ] The Git arm of `RouteRegistration.registerStateRoutes` calls `GithubTransportActor.start`. The Desk arm calls `CoreMailboxApi.startLoadSaveCommand`.
[ ] `GithubTransportActor.loadActorFunction` builds the Load `ActorFunction`. `GithubTransportActor.start` calls `CoreMailboxApi.coreMailboxStartActorFromFunction` in `src/Server/Core/CoreMailboxApi.fs`. That call places `Order.StartActor`. This ticket keeps that Load post.
[ ] `CoreActorPool.startLoadSaveCommand` in `src/Server/Core/CoreActorPool.fs` returns `Ok None` for `LoadSavePath.Desk` and for `LoadSavePath.Git`.
[ ] `DeskLoadPush.runLoadSave` in `src/Server/Core/DeskLoadPush.fs` calls `PathPick.effectiveLoadPath` in `src/Shared/PathPick.fs` before `CoreActorPool.startLoadSaveCommand`.
[ ] `CoreMailboxApi.startLoadSaveCommand` is the only send of `CoreMsg.StartLoadSaveCommand`.
[ ] Callers of that send are `src/Server/RouteRegistration.fs`, `tests/Server.Tests/LoadSaveCommandTests.fs`, and `tests/Server.Tests/ParseThreadLoadTests.fs`. Save calls stay until 19 — Start git Save.

### Reference

[ ] [Arch](../../arch.md) **Load and save**. `GithubTransportActor.start` is the Git Load post. `postLoadSaveCommand` calls the route's `startCommand`.

## Do not load

[ ] `startPeerActor` and `registerPeer` stay deleted. 20 — Move Desk Load and Desk Save deletes `CoreMsg.StartLoadSaveCommand`.
[ ] Find and Move `recordSearchStart`.
[ ] An edit to `src/Server/Core/DeskLoadPush.fs`. The file stays as `runLoadSave` is now.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.
[ ] Do not write `Order.StartActorDeprecated`.
[ ] The Git Save post. 19 — Start git Save keeps `GithubTransportActor.saveActorFunction`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Keep: `GithubTransportActor.start` for Load calls `loadActorFunction` and `CoreMailboxApi.coreMailboxStartActorFromFunction`. `postLoadSaveCommand` reaches that post through `router.startCommand`.
3. [ ] Keep: the Desk arm of `RouteRegistration.registerStateRoutes` calls `CoreMailboxApi.startLoadSaveCommand`. `CoreActorPool.startLoadSaveCommand` returns `Ok None`. That Desk Load runs no Actor.
4. [ ] Keep: ``plain Load of a File or Directory ignores a Git path`` sends `LoadSavePath.Git` to `CoreMailboxApi.startLoadSaveCommand`. `PathPick.effectiveLoadPath` makes that plain Load Desk. The push stays a Desk Load.
5. [ ] Keep: `src/Server/RouteRegistration.fs` has no `registerPeer` call. `startPeerActor` and `registerPeer` stay deleted.
6. [ ] Save callers of `CoreMsg.StartLoadSaveCommand` stay until 19 — Start git Save.
7. [ ] Leave `Order` as the cases in `src/Shared/History.fs`. `Order.StartActor` of `ActorOrder` stays the Git Load post.

## Outputs

[ ] No source file changes while the Working inputs match.
[ ] `tests/Server.Tests/GithubTransportActorTests.fs` stays the Load check of `GithubTransportActor.start`.
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs` stays the Desk Load check of `startLoadSaveCommand`.
[ ] `tests/Server.Tests/ParseThreadLoadTests.fs` stays the plain-Load check of `startLoadSaveCommand`.

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/10-dispatch-git-load-and-save-stop.md`, then ask.
[ ] Stop when Desk Load must start an Actor.
[ ] Stop when a test file outside Outputs must change.
[ ] Stop when this ticket writes `Order.StartActorDeprecated`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `GithubTransportActorTests`
  - `tests/Server.Tests`: `LoadSaveCommandTests`
  - `tests/Server.Tests`: `ParseThreadLoadTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/10-dispatch-git-load-and-save-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
