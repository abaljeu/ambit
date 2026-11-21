# 09 — Start the query command build

Ticket: [09 — Start the query command](09-dispatch-the-query-command-ticket.md)

## Stage

[x] Stage is build in [09 — Start the query command](CONTEXT.md).

## Edits

[x] `SearchActor.functionActorAt` returns the shared `ActorFunction`. The body posts `ActorQuery`, then `ActorChanges.actorStop`.
[x] `postActorStart` queues a query Run through `queueAdmittedQuery` when `SearchActor.isQueryRequest` is true.
[x] `startChosen` stays `context.pool.startActor`.
[x] `SearchActor.searchActorAt` posts `ActorSucceeded`.
[x] `recordSearchStart` takes that `ActorFunction` and posts `Order.StartActor`.
[x] `BootActors.search` carries the actor. `RouteRegistration.CreateBoot` sets it. `searchDoor` passes `pool.bootActors.search`.
[x] `replyActorOrder` calls `admitCaller`, then the pool. It appends `Order.StartActor` after the pool affirms.
[x] `Api.searchReply` no longer calls `recordStop`.
[x] `CoreMsg.RecordSearchStart` and `CoreEventDispatch.actorStart` are deleted.

## Check

[x] Verdict is in [09 — Start the query command check](../../reports/09-dispatch-the-query-command-check.md).

## Correction

[x] A start request goes to the pool first. `startFunction` admits the actor. `schedule` launches it. The `Ok` credential is the affirmation.
[x] `replyActorOrder` appends `Order.StartActor` only after that affirmation.
[x] A refusal from `admitFunctionStart` posts no `Order.StartActor` and no `ActorFailed`. The caller reply is the failure. The edit stays.
[x] The fact is ``query start error posts no rows, fails the reply, and keeps the edit``.

## Status

[x] Ticket status is `coded`.
