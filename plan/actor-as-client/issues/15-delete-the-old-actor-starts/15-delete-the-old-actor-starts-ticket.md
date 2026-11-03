# 15 — Delete the old Actor starts
**Status**: defined
One job: `startFunction` is the only pool start. `startActor`, `startPeerActor`, their name tables, `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, `CoreMsg.StartLoadSaveCommand`, `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave` are gone.

[ ] `startFunction` stays. Alan, 2026-11-03.
[ ] Alan, 2026-11-03: 10 — Dispatch git Load and Save converts every caller of `CoreMsg.StartLoadSaveCommand` to an actor launch through `ActorDispatch.fs` and `Order.RunActor`. After that ticket, nothing sends `StartLoadSaveCommand`. This ticket deletes the message, with `Order.StartActor` and `Order.LoadSave`.
[ ] `runSchedule` is renamed `launchPending`. It takes the Actor's pending work out of the live table and `Async.Start`s it on the thread pool with the row's cancel token.
[ ] The named outputs below are 9 source files and 2 test files. The size limit is 8 source files and 4 test files. The ninth source file stays so the `CoreMsg` matches leave in one ticket.
[ ] `CoreMailboxLoad.fs` is 75 lines. It joins the outputs when `dispatchStartLoadSaveCommand` has no caller left. That count is 10 source files and 2 test files.

Type:

[ ] coding

Blockers:

[ ] [08 — Dispatch the AI Actor](../08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md)
[ ] [09 — Dispatch the query command](../09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md)
[ ] [10 — Dispatch git Load and Save](../10-dispatch-git-load-and-save/10-dispatch-git-load-and-save-ticket.md)
[ ] [11 — Move door and case tests](../11-move-door-and-case-tests/11-move-door-and-case-tests-ticket.md)
[ ] [12 — Move pool and issue tests](../12-move-pool-and-issue-tests/12-move-pool-and-issue-tests-ticket.md)
[ ] [13 — Move persist and command tests](../13-move-persist-and-command-tests/13-move-persist-and-command-tests-ticket.md)
[ ] [14 — Move runtime and session tests](../14-move-runtime-and-session-tests/14-move-runtime-and-session-tests-ticket.md)
[ ] [80 — Place Actor starts through PlaceOrder](../../../core-refinement/issues/80-place-actor-orders-through-placeorder.md)

## Inputs

### Working

[ ] `startActor`, `startPeerActor`, `register`, and `registerPeer` are on `CoreActorPool`.
[ ] `CoreMsg.StartActor` and `CoreMsg.StartPeerActor` are matched in `CoreMailboxLoop.fs` and `CoreMailboxEvents.fs`.
[ ] `CoreMsg.StartLoadSaveCommand` is `src/Server/Core/CoreMsg.fs` lines 19-24.
[ ] `CoreMailboxLoop.runMsg` matches `StartLoadSaveCommand` at lines 22-24. `messageCaller` matches it at line 72.
[ ] `CoreMailboxEvents.operationContext` matches `StartLoadSaveCommand` at lines 46-47. `replyFailure` matches it at lines 64-65.
[ ] On staging, 2026-11-03, `CoreMailboxApi.startLoadSaveCommand` builds that case at `src/Server/Core/CoreMailboxApi.fs` lines 325-338. That function is the only constructor.
[ ] Callers of that function on staging are `src/Server/RouteRegistration.fs` line 349, `tests/Server.Tests/LoadSaveCommandTests.fs`, and `tests/Server.Tests/ParseThreadLoadTests.fs`.
[ ] This ticket starts after 10 — Dispatch git Load and Save. That ticket owns the caller conversion. After it, nothing sends `StartLoadSaveCommand`. A send that remains is a stop.
[ ] `CoreMailboxLoad.dispatchStartLoadSaveCommand` is `src/Server/Core/CoreMailboxLoad.fs` lines 34-55. Its only caller is the `runMsg` arm at lines 22-24.
[ ] `src/Server/Core/CoreActorPool.fs` is 417 lines.
[ ] `src/Server/Core/CoreMsg.fs` is 116 lines.
[ ] `src/Server/Core/CoreMailboxLoop.fs` is 464 lines.
[ ] `src/Server/Core/CoreMailboxEvents.fs` is 138 lines.
[ ] `src/Server/Core/CoreMailboxActors.fs` is 258 lines.
[ ] `src/Server/Core/CoreMailboxApi.fs` is 400 lines.
[ ] `src/Shared/History.fs` is 571 lines.
[ ] `src/Shared/EventJson.fs` is 454 lines.
[ ] `src/Shared/MailboxLog.fs` is 60 lines.
[ ] `tests/Shared.Tests/EventJsonTests.fs` is 399 lines.
[ ] `tests/Shared.Tests/MailboxLogTests.fs` is 118 lines. The MailboxLog test is `StartPeerActor carries Shared PeerActorName`.
[ ] `Order.StartPeerActor` is encoded in `src/Shared/EventJson.fs` and rejected by `src/Shared/MailboxLog.fs`.

### Reference

[ ] [Actor as client](../../project.md) cutover step three.
[ ] [Size limit](../../context/size-limit.md). Deleting `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, and `CoreMsg.StartLoadSaveCommand` updates every match of those cases. It stays one ticket so the tree builds. `startFunction` stays.

## Do not load

[ ] `startFunction`. It is the target start.
[ ] `Order.RunActor`. 06 — Log Order.RunActor adds it.
[ ] `CoreMsg.RecordSearchStart`. 09 — Dispatch the query command deletes that case.
[ ] `CoreEventDispatch.actorStart`. 09 — Dispatch the query command deletes its only caller and leaves the function.
[ ] A new `CoreMsg` case.
[ ] A reader for an old `Order.StartActor` row or an old `Order.LoadSave` row.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Delete `startActor`, `startPeerActor`, `register`, `registerPeer`, and the two name tables. Leave `startFunction`.
3. [ ] Rename `runSchedule` to `launchPending`. The body still takes the pending work off the live row and `Async.Start`s it on the row cancel token.
4. [ ] Delete `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, and `CoreMsg.StartLoadSaveCommand`, and their match arms in `CoreMailboxLoop` and `CoreMailboxEvents`. The `StartLoadSaveCommand` constructor is already gone.
5. [ ] Delete `CoreMailboxLoad.dispatchStartLoadSaveCommand` when that `runMsg` arm was its only caller.
6. [ ] Delete `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave`, and their codec and log arms. Leave `Order.RunActor`. Delete `PeerActorName` when no case carries it.
7. [ ] The decoder has no arm for an old `Order.StartActor` row or an old `Order.LoadSave` row.
8. [ ] Search `src/` and `tests/` for `startActor`, `startPeerActor`, `registerPeer`, `StartPeerActor`, `StartLoadSaveCommand`, `Order.StartActor`, and `Order.LoadSave`. The search is empty. `startFunction` remains.

## Outputs

[ ] `src/Server/Core/CoreActorPool.fs`
[ ] `src/Server/Core/CoreMsg.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxEvents.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Server/Core/CoreMailboxLoad.fs` — when `dispatchStartLoadSaveCommand` has no caller left
[ ] `src/Shared/History.fs`
[ ] `src/Shared/EventJson.fs`
[ ] `src/Shared/MailboxLog.fs`
[ ] `tests/Shared.Tests/EventJsonTests.fs`
[ ] `tests/Shared.Tests/MailboxLogTests.fs` — remove `StartPeerActor carries Shared PeerActorName`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/15-delete-the-old-actor-starts-stop.md`, then ask.
[ ] Stop when a search hit remains outside Outputs.
[ ] Stop when the edit deletes `startFunction` or `Order.RunActor`.
[ ] Stop when the edit keeps a reader for an old `Order.StartActor` row or an old `Order.LoadSave` row.
[ ] Stop when the edit adds a `CoreMsg` case.
[ ] Stop when the edit adds an `Order` case.
[ ] Stop when a send of `StartLoadSaveCommand` remains. 10 — Dispatch git Load and Save owns that conversion, including `ParseThreadLoadTests.fs`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: `EventJsonTests`
  - `tests/Shared.Tests`: `MailboxLogTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/15-delete-the-old-actor-starts-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
