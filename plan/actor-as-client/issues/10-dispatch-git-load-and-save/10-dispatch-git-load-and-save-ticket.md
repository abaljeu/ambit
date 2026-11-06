# 10 — Start git Load and Save
**Status**: defined
One job: `postLoadSaveCommand` builds the git Load or Save `FunctionActor` and puts `Order.ActorOrder` on the mailbox queue. A Desk Load or Save runs no Actor.

[ ] This ticket converts every caller of `CoreMsg.StartLoadSaveCommand`. After this ticket, nothing sends `StartLoadSaveCommand`. 15 — Delete the old Actor starts deletes the message.
[ ] The source files are 3. The test files are 4. The size limit is 8 source files and 4 test files.

Type:

[ ] coding

Blockers:

[ ] [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `postLoadSaveCommand` is in `src/Server/Api.fs`. The route is `MapPost "/ambit/load-save-command"` in `src/Server/RouteRegistration.fs`.
[ ] `RouteRegistration.fs` calls `pool.registerPeer` for `github-load` and `github-save`, both `GithubTransportActor.actorFn`.
[ ] `GithubTransportActor.actorFn` is an `ActorFn`. `GithubTransportActor.start` posts `CoreMsg.StartPeerActor`. Production HTTP uses `startLoadSaveCommand`.
[ ] `startLoadSaveCommand` in `src/Server/Core/CoreActorPool.fs` returns `Ok None` for Desk and calls `pool.startPeerActor` for Git.
[ ] `CoreMailboxApi.startLoadSaveCommand` is the only send of `CoreMsg.StartLoadSaveCommand`.
[ ] Callers of that send are `src/Server/RouteRegistration.fs`, `tests/Server.Tests/LoadSaveCommandTests.fs`, and `tests/Server.Tests/ParseThreadLoadTests.fs`.

### Reference

[ ] [Arch](../../arch.md). `postLoadSaveCommand` is the git Load and Save handler.

## Do not load

[ ] Do not delete `registerPeer`, `startPeerActor`, or the `CoreMsg.StartLoadSaveCommand` case. 15 — Delete the old Actor starts deletes them after no caller remains.
[ ] Find and Move `recordSearchStart`.
[ ] `DeskLoadPush.fs`. Its caller remains until that delete.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.
[ ] Do not write `Order.StartActor`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: `postLoadSaveCommand` builds `ActorOrder` for Git with the transport `FunctionActor`, the FocusId, and `actorName`, and calls `postActorOrder`.
3. [ ] The closure holds the path and the command node. The Desk arm runs no Actor and does not call `postActorOrder`.
4. [ ] Change: `GithubTransportActor.start` uses that same `ActorOrder`. It does not post `CoreMsg.StartPeerActor`.
5. [ ] Contract: Remove the two `registerPeer` calls.
6. [ ] Change: Convert every caller of `CoreMsg.StartLoadSaveCommand`. After this step, nothing sends `StartLoadSaveCommand`.
7. [ ] Do not write `Order.StartActor` or `Order.RunActor`.

## Outputs

[ ] `src/Server/Api.fs`
[ ] `src/Server/GithubTransportActor.fs`
[ ] `src/Server/RouteRegistration.fs`
[ ] `tests/Server.Tests/GithubTransportActorTests.fs`
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs`
[ ] `tests/Server.Tests/ParseThreadLoadTests.fs` — callers of `startLoadSaveCommand`
[ ] `tests/Server.Tests/AppGithubTransportBoundaryTests.fs` — the assertion that names `registerPeer`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/10-dispatch-git-load-and-save-stop.md`, then ask.
[ ] Stop when Desk must start an Actor.
[ ] Stop when a test file outside Outputs must change.
[ ] Stop when this ticket writes `Order.StartActor`.
[ ] Stop when a send of `StartLoadSaveCommand` remains.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `GithubTransportActorTests`
  - `tests/Server.Tests`: `LoadSaveCommandTests`
  - `tests/Server.Tests`: `ParseThreadLoadTests`
  - `tests/Server.Tests`: `AppGithubTransportBoundaryTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/10-dispatch-git-load-and-save-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
