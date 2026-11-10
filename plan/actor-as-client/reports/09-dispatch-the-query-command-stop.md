# 09 — Start the query command — stop

Process step 6 of [09 — Start the query command](../issues/09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md) stops this run. Steps 2 through 5 match the code. Step 6 does not. Steps 7 through 10 stay unworked. Source files stay as they are after the ticket commit. Status stays `defined`. The folder stage stays `defined`.

## Steps that match

[ ] Process step 1 matches. Each Working input matches the code on this branch.
[ ] `SearchActor.functionActorAt` can live in `src/Server/SearchActor.fs` and return the shared `ActorFunction`.
[ ] `postActorStart` in `src/Server/Core/CoreMailboxActors.fs` can call `SearchActor.isQueryRequest` and `SearchActor.functionActorAt`. The Server project file lists `SearchActor.fs` before `Core/CoreMailboxActors.fs`.
[ ] `startChosen` is `context.pool.startActor`. A query branch in `postActorStart` can leave that function for named starts.
[ ] `SearchActor.searchActorAt` can live in `src/Server/SearchActor.fs`.

## Input that differs

[ ] Process step 6 says `recordSearchStart` builds the Find and Move order with `SearchActor.searchActorAt`.
[ ] `recordSearchStart` is in `src/Server/Core/CoreMailboxApi.fs`.
[ ] The Server project file lists `Core/CoreMailboxApi.fs` before `SearchActor.fs`.
[ ] A call from `recordSearchStart` to `SearchActor.searchActorAt` fails to compile. The compiler reports that `SearchActor` is not defined in that file.
[ ] `CoreMailboxActors.fs` is the mailbox file that already calls `postBuiltOrder` for `?test` and `?ai`, and that file can name `SearchActor`. `CoreMailboxApi.fs` cannot.

## Ask

[ ] Name the file that builds the Find and Move `ActorOrder`, given that `recordSearchStart` cannot name `SearchActor`.
