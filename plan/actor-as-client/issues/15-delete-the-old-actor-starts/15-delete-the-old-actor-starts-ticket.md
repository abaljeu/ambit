# 15 — Delete the old Actor starts
**Status**: defined
One job: `startFunction` is the only pool start. `startActor`, `startPeerActor`, their name tables, `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave` are gone.

[ ] `startFunction` stays. Alan, 2026-11-03.
[ ] `runSchedule` is renamed `launchPending`. It takes the Actor's pending work out of the live table and `Async.Start`s it on the thread pool with the row's cancel token.

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
[ ] [Size limit](../../context/size-limit.md). Deleting `CoreMsg.StartActor` and `CoreMsg.StartPeerActor` updates every match of those cases. It stays one ticket so the tree builds. `startFunction` is not deleted.

## Do not load

[ ] `startFunction`. It is the target start.
[ ] `Order.RunActor`. Ticket 06 adds it.
[ ] `CoreMsg.StartLoadSaveCommand` and `CoreMsg.RecordSearchStart`. Their orders are already `Order.RunActor`. This ticket deletes the log cases, not those `CoreMsg` cases, unless a match must drop because the `Order` case is gone.
[ ] Do not add a `CoreMsg` case.
[ ] Do not keep a reader for an old `Order.StartActor` row or an old `Order.LoadSave` row.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Delete `startActor`, `startPeerActor`, `register`, `registerPeer`, and the two name tables. Leave `startFunction`.
3. [ ] Rename `runSchedule` to `launchPending`. The body still takes the pending work off the live row and `Async.Start`s it on the row cancel token.
4. [ ] Delete `CoreMsg.StartActor` and `CoreMsg.StartPeerActor` and their match arms. Add no `CoreMsg` case.
5. [ ] Delete `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave`, and their codec and log arms. Leave `Order.RunActor`. Delete `PeerActorName` when no case carries it.
6. [ ] The decoder does not read an old `Order.StartActor` row or an old `Order.LoadSave` row.
7. [ ] Search `src/` and `tests/` for `startActor`, `startPeerActor`, `registerPeer`, `StartPeerActor`, `Order.StartActor`, and `Order.LoadSave`. The search is empty. `startFunction` remains.

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
[ ] Stop when the edit deletes `startFunction` or `Order.RunActor`.
[ ] Stop when the edit keeps a reader for an old `Order.StartActor` row or an old `Order.LoadSave` row.
[ ] Stop when the edit adds a `CoreMsg` case.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: `EventJsonTests`
  - `tests/Shared.Tests`: `MailboxLogTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/15-delete-the-old-actor-starts-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
