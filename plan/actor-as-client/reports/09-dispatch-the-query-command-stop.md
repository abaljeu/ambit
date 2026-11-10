# 09 — Start the query command — stop

Process step 1 of [09 — Start the query command](../issues/09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md) stops this run. The Working input for `startChosen` differs from the code. Steps 2 through 6 stay unworked. Source files stay as they are on `staging`. Status stays `defined`. The folder stage stays `defined`.

## Input that differs

[ ] The ticket says `startChosen` calls `SearchActor.functionStart` and then `pool.startFunction` when `SearchActor.isQueryRequest` is true.
[ ] `startChosen` is `src/Server/Core/CoreMailboxActors.fs`.
[ ] `startChosen` calls `context.pool.startActor request _getState` and maps the result with `Result.map Some`.
[ ] The comment on `startChosen` says query starts and named starts both use the ID-bag start, and `startFunction` stays unused on this path.
[ ] `src/Server/Core/CoreMailboxActors.fs` contains no call to `SearchActor.functionStart`.
[ ] `src/Server/Core/CoreMailboxActors.fs` contains no call to `SearchActor.isQueryRequest`.
[ ] `SearchActor.functionStart` in `src/Server/SearchActor.fs` still builds a server `FunctionStart` with `getGraph` and `actorNodeId` set to `request.focusId`.
[ ] `SearchActor.isQueryRequest` still lives in `src/Server/SearchActor.fs`.
[ ] Process step 3 says `startChosen` no longer starts a query, and other names still call `startActor`. That step uses the input above. The code already sends the query start and the named start through `startActor` inside `startChosen`.

## Working inputs that match

[ ] `postSearchRequest` in `src/Server/RouteRegistration.fs` calls `Api.postSearch`.
[ ] `searchDoor` calls `CoreMailboxApi.recordSearchStart`.
[ ] `SearchActor.functionStart` builds `FunctionStart` with `getGraph` and `actorNodeId` equal to `request.focusId`. `FunctionStart` is in `src/Server/Core/CoreActorPool.fs`.
[ ] `CoreMailboxApi.recordSearchStart` posts `CoreMsg.RecordSearchStart`. That function is the only constructor.
[ ] `CoreMailboxLoop.runMsg` calls `CoreEventDispatch.actorStart` for `RecordSearchStart`. That `runMsg` arm is the only caller of `CoreEventDispatch.actorStart`.
[ ] `CoreMailboxLoop.messageCaller`, `CoreMailboxEvents.operationContext`, and `CoreMailboxEvents.replyFailure` match `RecordSearchStart`.
[ ] `tests/Server.Tests/SearchActorTests.fs` calls `CoreMailboxApi.recordSearchStart` in ``postSearch records ActorStart at the root``.

## Ask

[ ] Correct the `startChosen` input, then say how the next run should treat the query start.
