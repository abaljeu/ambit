# 113 — Actors return their ActorResult

Ticket: [113 — Actors return their ActorResult](113-actors-return-their-actorresult.md)

The shared `ActorFunction`, the pool `ActorFunction`, and `ActorFn` return `Async<ActorResult>`.

`CoreActorPool.runSchedule` starts `work |> Async.Ignore` on the row cancellation token. The pool does not read the result.

Each stop call stays. The producer returns the same `ActorResult` it passes to that stop.

`SearchActor` edits are `functionStart`, `functionActorAt`, and `searchActorAt`.

- `functionStart` and `functionActorAt` call `queryActorBody`, keep that stop, and return the result passed to the stop.
- `searchActorAt` calls `changes.actorStop` with `ActorSucceeded` and returns `ActorSucceeded`.
- `SearchActor.reply`, `SearchActor.actorStart`, `SearchActor.oneReply`, and `queryActorBody` stay.

[23 — Server search works](../../online-search/issues/23-server-search-works.md) has landed. `SearchActor.reply` still starts the walk at `request.startId`.

``postSearch records ActorStart at the root`` returns the result of `SearchActor.searchActorAt`. The assertions stay.

A started test body that does not assert a stop waits on `Async.CancellationToken` after its observation.

`origin/staging` at `2b3ab7f3` was already the branch base. The merge was already up to date.

Check: pass

- `scripts/agent-test.sh` with no arguments passed Server 77 and Shared 80. Failed 0.
- `scripts/agent-test.sh --filter` for the five named tests passed inside the Server run of 29 and the Shared run of 1. Failed 0.
- `scripts/agent-test.sh --full` with `AMBIT_FULL_SUITE=1` passed Server 774, skipped 1, and Shared 1944, skipped 3. Failed 0.
