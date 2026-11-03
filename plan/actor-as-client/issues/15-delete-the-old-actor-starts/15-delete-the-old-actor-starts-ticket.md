# 15 — Delete the old Actor starts
**Status**: defined
One job: The pool has one start. The old name tables, `startActor`, `startPeerActor`, `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, and `Order.StartPeerActor` are gone.

[ ] `startFunction` leaves in the same delete. It is a second function start, and its record still carries `getGraph` and `commandId`. Alan's delete list did not name it. This ticket includes it because the decision is one start. Stop if Alan keeps it.

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
[ ] `Order.StartPeerActor` is encoded in `src/Shared/EventJson.fs` and rejected by `src/Shared/MailboxLog.fs`.
[ ] `tests/Shared.Tests/EventJsonTests.fs` and `tests/Shared.Tests/MailboxLogTests.fs` name `Order.StartPeerActor`. The MailboxLog test is `StartPeerActor carries Shared PeerActorName`.

### Reference

[ ] [Actor as client](../../project.md) cutover step three.
[ ] [Size limit](../../context/size-limit.md). The delete touches the same exhaustive matches as ticket 06. It stays one ticket so the tree builds.

## Do not load

[ ] `Order.StartActor`. Alan's delete list does not name it.
[ ] `CoreMsg.StartLoadSaveCommand` and `Order.LoadSave`.
[ ] Find and Move `RecordSearchStart`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Delete `startActor`, `startPeerActor`, `register`, `registerPeer`, and the two name tables.
3. [ ] Delete `startFunction` and `FunctionStart` when no caller remains.
4. [ ] Delete `CoreMsg.StartActor` and `CoreMsg.StartPeerActor` and their match arms.
5. [ ] Delete `Order.StartPeerActor` and its codec and log arms.
6. [ ] Search `src/` and `tests/` for `startActor`, `startPeerActor`, `registerPeer`, `StartPeerActor`, and `startFunction`. The search is empty.

## Outputs

[ ] `src/Server/Core/CoreActorPool.fs`
[ ] `src/Server/Core/CoreMsg.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxEvents.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Shared/History.fs`
[ ] `src/Shared/EventJson.fs`
[ ] `src/Shared/MailboxLog.fs`
[ ] `tests/Shared.Tests/EventJsonTests.fs`
[ ] `tests/Shared.Tests/MailboxLogTests.fs` — remove `StartPeerActor carries Shared PeerActorName`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/15-delete-the-old-actor-starts-stop.md`, then ask.
[ ] Stop when a search hit remains outside Outputs.
[ ] Stop when Alan keeps `startFunction`.
[ ] Stop when the edit deletes `Order.StartActor` or `Order.LoadSave`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: `EventJsonTests`
  - `tests/Shared.Tests`: `MailboxLogTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/15-delete-the-old-actor-starts-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
