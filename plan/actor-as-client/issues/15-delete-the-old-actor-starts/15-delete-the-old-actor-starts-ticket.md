# 15 — Delete the old Actor starts
**Status**: defined
One job: `startFunction` is the only pool start. `startActor`, `startPeerActor`, their name tables, `CoreMsg.StartPeerActor`, and `CoreMsg.StartLoadSaveCommand` are gone.

[ ] `startFunction` stays.
[ ] 10 — Start git Load converts Load. 19 — Start git Save converts Save. After both, nothing sends `CoreMsg.StartLoadSaveCommand`. This ticket deletes the message.
[ ] `runSchedule` is renamed `launchPending`. It takes the Actor's pending work out of the live table and `Async.Start`s it on the thread pool with the row's cancel token.
[ ] The Order cases stay until 16 — Delete the old Order cases. Putting those files in this ticket made 10 source files. The size limit is 8.
[ ] This ticket starts after 112 — Place Cancel through PlaceOrder has landed. The cancel case is `Order.CancelActor`. This ticket does not edit it.

Type:

[ ] coding

Blockers:

[ ] [08 — Start the AI Actor](../08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md)
[ ] [09 — Start the query command](../09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md)
[ ] [10 — Start git Load](../10-dispatch-git-load-and-save/10-dispatch-git-load-and-save-ticket.md)
[ ] [19 — Start git Save](../19-dispatch-git-save/19-dispatch-git-save-ticket.md)
[ ] [11 — Move door and case tests](../11-move-door-and-case-tests/11-move-door-and-case-tests-ticket.md)
[ ] [12 — Move pool and issue tests](../12-move-pool-and-issue-tests/12-move-pool-and-issue-tests-ticket.md)
[ ] [13 — Move persist and command tests](../13-move-persist-and-command-tests/13-move-persist-and-command-tests-ticket.md)
[ ] [14 — Move runtime and session tests](../14-move-runtime-and-session-tests/14-move-runtime-and-session-tests-ticket.md)
[ ] [18 — Place ActorOrder through PlaceOrder](../18-place-actororder-through-placeorder/18-place-actororder-through-placeorder-ticket.md)
[ ] [112 — Place Cancel through PlaceOrder](../../../core-refinement/issues/112-place-cancel-through-placeorder.md)

## Inputs

### Working

[ ] `startActor`, `startPeerActor`, `register`, and `registerPeer` are on `CoreActorPool`.
[ ] `CoreMsg` in `src/Server/Core/CoreMsg.fs` is `StartPeerActor`, `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `ActorStop`, and `PostEvent`.
[ ] `CoreMsg.StartPeerActor` is matched in `src/Server/Core/CoreMailboxLoop.fs` and `src/Server/Core/CoreMailboxEvents.fs`.
[ ] `CoreMsg.StartLoadSaveCommand` is built by `CoreMailboxApi.startLoadSaveCommand`. After 10 — Start git Load and 19 — Start git Save, nothing sends it.
[ ] `CoreMailboxLoad.dispatchStartLoadSaveCommand` runs from the `StartLoadSaveCommand` arm of `runMsg`.
[ ] `runSchedule` is private in `src/Server/Core/CoreActorPool.fs`. The pool field that calls it is `schedule`.

### Reference

[ ] [Arch](../../arch.md).
[ ] [Size limit](../../context/size-limit.md).

## Do not load

[ ] `startFunction`. It is the target start.
[ ] `Order.StartActorDeprecated` and `Order.StartActor` of `ActorOrder`. 16 — Delete the old Order cases deletes `Order.StartActorDeprecated` and leaves `Order.StartActor`.
[ ] `CoreMsg.RecordSearchStart` and `CoreEventDispatch.actorStart`. 09 — Start the query command deletes them.
[ ] A new `CoreMsg` case.
[ ] `ActorDispatch.fs`. Leave `Order` as the cases in `src/Shared/History.fs`.
[ ] Remote starts.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Contract: Delete `startActor`, `startPeerActor`, `register`, `registerPeer`, and the two name tables. Leave `startFunction`.
3. [ ] Change: Rename `runSchedule` to `launchPending`. The body still takes the pending work off the live row and `Async.Start`s it on the row cancel token.
4. [ ] Contract: Delete `CoreMsg.StartPeerActor` and `CoreMsg.StartLoadSaveCommand`, and their match arms in `CoreMailboxLoop` and `CoreMailboxEvents`.
5. [ ] Contract: Delete `CoreMailboxLoad.dispatchStartLoadSaveCommand` when that `runMsg` arm was its only caller.
6. [ ] Search `src/` and `tests/` for `startActor`, `startPeerActor`, `registerPeer`, and `StartLoadSaveCommand`. The search is empty. `startFunction` remains.

## Outputs

[ ] `src/Server/Core/CoreActorPool.fs`
[ ] `src/Server/Core/CoreMsg.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxEvents.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Server/Core/CoreMailboxLoad.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/15-delete-the-old-actor-starts-stop.md`, then ask.
[ ] Stop when a search hit remains outside Outputs.
[ ] Stop when the edit deletes `startFunction`.
[ ] Stop when the edit adds a `CoreMsg` case or an `Order` case.
[ ] Stop when a send of `StartLoadSaveCommand` remains.
[ ] Stop when 112 has not landed.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names.
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/15-delete-the-old-actor-starts-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
