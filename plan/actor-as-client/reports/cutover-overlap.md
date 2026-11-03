# Cutover overlap

[ ] This note says how the 2026-11-03 Actor-start cutover meets three core-refinement tickets. Those tickets are not edited here.
[ ] 08 — Pointer: Core Actor pool, 81 — Log Actor and search stops as Notices, and 118 — Test Actors return their result stay as written. 81 is being built.

## 112 — Place Cancel through PlaceOrder

[ ] Cancel stays `Order.Cancel` of the FocusId.
[ ] The one start keeps that FocusId as the pool key, so the cancel target does not change.
[ ] 112 shares `CoreMailboxLoop`, `CoreMailboxActors`, and `CoreMailboxEvents` with the cutover. The cancel arms stay. `CoreMsg.StartActor` and `CoreMsg.StartPeerActor` leave under [15 — Delete the old Actor starts](../issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md). No new `CoreMsg` case. [06 — Log Order.RunActor](../issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md) adds the log order only.

## 113 — Actors return their ActorResult

[ ] 113 changes both `ActorFn` and `FunctionActor` to return `Async<ActorResult>`.
[ ] The cutover ends on one function. `ActorFn` and the id-bag path are obsolescent.
[ ] 113 comes with or after the merge onto that one function. Before the merge it edits both types, and 15 — Delete the old Actor starts then deletes `ActorFn`.

## 117 — The pool posts Notice.ActorStopped when an Actor ends

[ ] 117 edits `runSchedule`, which starts the body with `Async.Start`.
[ ] 15 — Delete the old Actor starts renames `runSchedule` to `launchPending` before 117. 117 uses that name.
[ ] After the contract, that function runs `startFunction`'s pending work. The id-bag arm is gone.
[ ] 117 stays blocked by 113 — Actors return their ActorResult, so it follows the same merge.
