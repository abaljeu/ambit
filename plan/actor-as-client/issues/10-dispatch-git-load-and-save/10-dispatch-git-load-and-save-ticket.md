# 10 — Dispatch git Load and Save
**Status**: defined
One job: The dispatcher maps git Load and git Save to the GitHub transport function, and the pool starts it with `startFunction`.

[ ] A Desk Load or Save still runs no Actor.
[ ] This ticket converts every caller of `CoreMsg.StartLoadSaveCommand` to an actor launch through `ActorDispatch.fs` and `Order.RunActor`. After this ticket, nothing sends `StartLoadSaveCommand`. [15 — Delete the old Actor starts](../15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md) deletes the message.
[ ] The source files are 6. The test files are 4. The size limit is 8 source files and 4 test files.

Type:

[ ] coding

Blockers:

[ ] [07 — Dispatch the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `RouteRegistration.fs` calls `pool.registerPeer` for `github-load` and `github-save`, both `GithubTransportActor.actorFn`.
[ ] `GithubTransportActor.actorFn` is an `ActorFn`. `GithubTransportActor.start` posts `CoreMsg.StartPeerActor`. Tests call `start`. Production HTTP uses `startLoadSaveCommand`.
[ ] `startLoadSaveCommand` in `src/Server/Core/CoreActorPool.fs` returns `Ok None` for Desk and calls `pool.startPeerActor` for Git.
[ ] `dispatchStartLoadSaveCommand` in `src/Server/Core/CoreMailboxLoad.fs` calls `DeskLoadPush.runLoadSave`.
[ ] `CoreMailboxApi.startLoadSaveCommand` in `src/Server/Core/CoreMailboxApi.fs` lines 325-338 is the only send of `CoreMsg.StartLoadSaveCommand`.
[ ] Callers of that send are `src/Server/RouteRegistration.fs` line 349, `tests/Server.Tests/LoadSaveCommandTests.fs`, and `tests/Server.Tests/ParseThreadLoadTests.fs`.

### Reference

[ ] [Actor as client](../../project.md). Alan named git Load and Save as dispatcher names.
[ ] [80 — Place Actor starts through PlaceOrder](../../../core-refinement/issues/80-place-actor-orders-through-placeorder.md) keeps the Desk no-Actor log.

## Do not load

[ ] Do not delete `registerPeer` or `startPeerActor` while another caller remains.
[ ] Find and Move `recordSearchStart`.
[ ] The `CoreMsg.StartLoadSaveCommand` case. This ticket stops the sends. 15 — Delete the old Actor starts deletes the message.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Map `github-load` and `github-save` in `ActorDispatch.fs`. Each closure holds the path and the command node.
3. [ ] The Git arm of `startLoadSaveCommand` calls `pool.startFunction`. The closure holds the path and the command node. The Desk arm stays `Ok None`.
4. [ ] `GithubTransportActor.start` uses `startFunction`. It does not post `CoreMsg.StartPeerActor`.
5. [ ] Git Load and Save log `Order.RunActor` of the command name and the FocusId. This ticket does not write `Order.LoadSave` or `Order.StartActor`.
6. [ ] Remove the two `registerPeer` calls.
7. [ ] Convert every caller of `CoreMsg.StartLoadSaveCommand` to an actor launch through `ActorDispatch.fs` and `Order.RunActor`. After this step, nothing sends `StartLoadSaveCommand`.

## Outputs

[ ] `src/Server/ActorDispatch.fs`
[ ] `src/Server/GithubTransportActor.fs`
[ ] `src/Server/RouteRegistration.fs`
[ ] `src/Server/Core/CoreActorPool.fs`
[ ] `src/Server/Core/DeskLoadPush.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs` — `startLoadSaveCommand` stops sending `CoreMsg.StartLoadSaveCommand`
[ ] `tests/Server.Tests/GithubTransportActorTests.fs`
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs`
[ ] `tests/Server.Tests/ParseThreadLoadTests.fs` — callers of `startLoadSaveCommand`
[ ] `tests/Server.Tests/AppGithubTransportBoundaryTests.fs` — the assertion that names `registerPeer`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/10-dispatch-git-load-and-save-stop.md`, then ask.
[ ] Stop when Desk must start an Actor.
[ ] Stop when a test file outside Outputs must change.
[ ] Stop when this ticket writes `Order.LoadSave` or `Order.StartActor`.
[ ] Stop when a send of `StartLoadSaveCommand` remains.
[ ] Deleting the `CoreMsg.StartLoadSaveCommand` case is 15 — Delete the old Actor starts.

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
