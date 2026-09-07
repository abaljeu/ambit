# Implement issue 01 Actor produce path

Date: 2026-09-05

Delivered the locked Actor produce protocol on Normal Core Changes. Issue: [[../issues/01-generalized-server-actor-produce-path.md]]. Project Stage stays `active`. Issue 02 was not started.

## Outcome

A Server-side test Actor submits a Change through Normal `CoreChanges.postChange` without HTTP self-post and without Graph-only. The Actor is thread-pool `Async` started off the FileAgent apply mailbox. It receives a test-constructed `Graph` (Local Graph / common prior) and the full `CoreChanges` handle. It `return!`s `postChange` so the cold Async actually posts. It awaits `CoreChangesAccepted` or Error. Poll (`Api.getPoll`) returns the same accepted Change and Revision as a Browser-posted Change.

No production Actor, pool, job identity, Command, cancel, finish, shutdown, or live subgraph extraction was added. Agent `postChange` stays private. Auth and malformed Reject are unchanged.

## Test Actor

Lives in [[tests/Server.Tests/CoreChangesTests.fs]] next to the existing typed Normal caller.

- `runActor` starts `Graph -> CoreChanges -> Async<'a>` on the thread pool (`Async.StartAsTask`).
- `produceFromSubgraph` reads ROOT children from the passed subgraph, plans one Normal Change, and `return!`s `handle.postChange`.
- Fact `test Actor posts Normal Change off apply mailbox and Poll sees it` constructs `Graph.create ()`, hands the FileAgent `CoreChanges` handle, awaits the acknowledgement, and checks Poll.

The existing `typed Normal caller publishes accepted Change to Poll` test is unchanged. It is not the Actor.

## Focused verification

- `dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~CoreChangesTests"` — 3 passed, 0 failed.

No Shared or Browser file changed, so the Browser compile gate was not required.

## Out of scope (unchanged)

Pool, job identity, Command launch, cancel, finish, shutdown, production Actors, live Query/Files extraction, Parse-as-Actor, and soft-lock UI. Those wait for [[../issues/02-core-actor-pool.md]] and tickets 09–12.
