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
[x] `AiInstall.search` carries the actor. `RouteRegistration.CreateBoot` sets it. `searchDoor` passes `pool.aiInstall.search`.
[x] `replyActorOrder` calls `admitCaller` before it appends the order.
[x] `Api.searchReply` no longer calls `recordStop`.
[x] `CoreMsg.RecordSearchStart` and `CoreEventDispatch.actorStart` are deleted.

## Check

[x] Verdict is in [09 — Start the query command check](../../reports/09-dispatch-the-query-command-check.md).

## Status

[x] Ticket status is `coded`.
