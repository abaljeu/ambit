# 10 — Start git Load
**Status**: defined
One job: `postLoadSaveCommand` builds `ActorOrder` for Git Load (transport `ActorFunction`, FocusId, `actorName`) and posts it. A Desk Load runs no Actor and does not call `postActorOrder`.

[ ] This ticket converts git Load. Save callers of `CoreMsg.StartLoadSaveCommand` stay until [19 — Start git Save](../19-dispatch-git-save/19-dispatch-git-save-ticket.md).
[ ] 15 — Delete the old Actor starts deletes the message.
[ ] The source files are 3. The test files are 3. The size limit is 8 source files and 4 test files.

Type:

[ ] coding

Blockers:

[ ] [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `postLoadSaveCommand` is in `src/Server/Api.fs`. The route is `MapPost "/ambit/load-save-command"` in `src/Server/RouteRegistration.fs`.
[ ] `RouteRegistration.fs` calls `pool.registerPeer` for `github-load` and `github-save`, both `GithubTransportActor.actorFn`. This ticket removes the `github-load` call.
[ ] `GithubTransportActor.actorFn` is an `ActorFn`. `GithubTransportActor.start` posts `CoreMsg.StartPeerActor`. This ticket changes `start` for Load.
[ ] `startLoadSaveCommand` in `src/Server/Core/CoreActorPool.fs` returns `Ok None` for Desk and calls `pool.startPeerActor` for Git.
[ ] `CoreMailboxApi.startLoadSaveCommand` is the only send of `CoreMsg.StartLoadSaveCommand`.
[ ] Callers of that send are `src/Server/RouteRegistration.fs`, `tests/Server.Tests/LoadSaveCommandTests.fs`, and `tests/Server.Tests/ParseThreadLoadTests.fs`. The calls in `ParseThreadLoadTests.fs` are Load. Save calls stay until 19 — Start git Save.

### Reference

[ ] [Arch](../../arch.md). `postLoadSaveCommand` is the git Load handler.

## Do not load

[ ] Do not delete `registerPeer`, `startPeerActor`, or the `CoreMsg.StartLoadSaveCommand` case. 15 — Delete the old Actor starts deletes them.
[ ] Find and Move `recordSearchStart`.
[ ] `DeskLoadPush.fs`. Its caller remains until that delete.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.
[ ] Do not write `Order.StartActor`.
[ ] The `github-save` `registerPeer` call. 19 — Start git Save removes it.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: `postLoadSaveCommand` builds `ActorOrder` for Git Load with the transport `ActorFunction`, the FocusId, and `actorName`, and calls `postActorOrder`.
3. [ ] The closure holds the path and the command node. The Desk Load arm runs no Actor and does not call `postActorOrder`.
4. [ ] Change: `GithubTransportActor.start` for Load uses that same `ActorOrder`. It does not post `CoreMsg.StartPeerActor` for Load.
5. [ ] Contract: Remove the `github-load` `registerPeer` call.
6. [ ] Change: Convert each Load caller of `CoreMsg.StartLoadSaveCommand`. Save callers stay until 19 — Start git Save.
7. [ ] Do not write `Order.StartActor` or `Order.RunActor`.

## Outputs

[ ] `src/Server/Api.fs`
[ ] `src/Server/GithubTransportActor.fs`
[ ] `src/Server/RouteRegistration.fs`
[ ] `tests/Server.Tests/GithubTransportActorTests.fs` — Load calls of `GithubTransportActor.start`
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs` — Load callers of `startLoadSaveCommand`
[ ] `tests/Server.Tests/ParseThreadLoadTests.fs` — Load callers of `startLoadSaveCommand`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/10-dispatch-git-load-and-save-stop.md`, then ask.
[ ] Stop when Desk Load must start an Actor.
[ ] Stop when a test file outside Outputs must change.
[ ] Stop when this ticket writes `Order.StartActor`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `GithubTransportActorTests`
  - `tests/Server.Tests`: `LoadSaveCommandTests`
  - `tests/Server.Tests`: `ParseThreadLoadTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/10-dispatch-git-load-and-save-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
