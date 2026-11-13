# Cutover overlap

[x] This note says how the Actor-start cutover meets the Actor-stop chain. The chain tickets are edited in their own files.
[x] [81 — Log Actor stops as Notices](../../core-refinement/issues/81-log-actor-and-search-stops-as-notices.md) is `coded`.
[x] [21 — Convert CoreMsg.ActorStop](../issues/21-convert-coremsg-actorstop/21-convert-coremsg-actorstop-ticket.md) is superseded. Alan, 2026-10-24 and 2026-11-13. The chain is 113 — Actors return their ActorResult, then 117 — The pool posts Notice.ActorStopped when an Actor ends, then 114 — Remove CoreMsg.ActorStop.
[x] [118 — Test Actors return their result](../../core-refinement/issues/118-test-actors-return-their-result.md) is superseded by 114. Alan, 2026-11-13. The two recording-pool door tests move into 114.

## 112 — Place Cancel through PlaceOrder

[x] 112 — Place Cancel through PlaceOrder is `coded` and landed on staging in PR 522.
[x] Cancel is `Order.CancelActor` of the focus id. The focus id stays the pool key.
[x] 112 shared `CoreMailboxLoop`, `CoreMailboxActors`, and `CoreMailboxEvents` with the cutover. The cancel arms stayed. [15 — Delete the old Actor starts](../issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md) removed `CoreMsg.StartPeerActor`.

## 113 — Actors return their ActorResult

[x] Live starts use `ActorFunction` in `src/Shared/History.fs`.
[x] The pool `ActorFunction` in `src/Server/Core/CoreActorPool.fs` is the wrapper `CoreMailboxLoop.poolActor` adapts onto.
[x] `ActorFn` remains on the pool. `CoreActorPool.register`, `CoreActorPool.startActor`, and `TestActor.actorFn` use it.
[x] `FunctionActor` is not a type. `ActorOrderFromStart.FunctionActorPlacement` is the placement record.
[ ] 113 — Actors return their ActorResult changes the shared `ActorFunction`, the pool `ActorFunction`, and the leftover `ActorFn` to return `Async<ActorResult>`. `runSchedule` ignores the result. The stop calls stay.
[x] 15 — Delete the old Actor starts left `ActorFn` and `runSchedule` in place.

## 117 — The pool posts Notice.ActorStopped when an Actor ends

[x] `runSchedule` is still the name in `src/Server/Core/CoreActorPool.fs`. 15 — Delete the old Actor starts did not rename it to `launchPending`.
[ ] 117 — The pool posts Notice.ActorStopped when an Actor ends edits `runSchedule` and `CoreMailboxLoop.dispatchItem`.
[x] `runMsg` and `dispatchItem` are in `CoreMailboxLoop`.
[x] 117 is blocked by 113 — Actors return their ActorResult. 81 — Log Actor stops as Notices is `coded` and is not a blocker.
[x] Alan, 2026-11-13: the pool-posted stop row keeps `Authority "Actor"`. ``mailbox appends StartActor and ActorStop in lifecycle order`` stays unchanged.
[ ] Each live chain ticket is a strict refactor. The observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results stay the same.

## 114 — Remove CoreMsg.ActorStop

[ ] 114 — Remove CoreMsg.ActorStop removes `CoreMsg.ActorStop`. Its blocker is 117. `RecordSearchStop` stays for [24 — Post Notice.SearchStopped](../../online-search/issues/24-post-searchstopped-notice.md).
[x] 118 — Test Actors return their result is superseded by 114. The two recording-pool tests in `tests/Server.Tests/CoreMailboxDoorTests.fs` move into 114.
[x] 24 — Post Notice.SearchStopped is owned by AmbotX. Its build waits until 114 lands.
[x] [23 — Server search works](../../online-search/issues/23-server-search-works.md) builds in parallel and touches `src/Server/SearchActor.fs`. The chain's `SearchActor` edits are the stop calls in 113 and 117.
