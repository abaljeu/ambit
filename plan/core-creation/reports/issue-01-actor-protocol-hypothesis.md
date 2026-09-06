# Issue 01 Actor protocol hypothesis

Date: 2026-09-05

Refined [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] from Alan's Actor sketch, then locked the four open questions. Status stays `ready-for-agent`. Project Stage unchanged.

## What the issue now says

Issue 01 builds a small Actor produce protocol on Normal `CoreChanges.postChange`. An Actor is thread-pool `Async` or `Task`, off the apply mailbox, not a dedicated OS thread. It receives a subgraph (`Graph` / Local Graph) and the full `CoreChanges` handle. It produces through Normal `postChange`. Core answers with `CoreChangesAccepted` or Error. Awaiting that acknowledgement is optional for one-shot Actors; the test Actor awaits and asserts. Other Browsers consume by Poll. One test Actor that follows this protocol is the demo. Pool, cancel, job identity, and Command stay in [[plan/core-creation/issues/02-core-actor-pool.md]] and tickets 09–12.

The existing typed Normal test in [[tests/Server.Tests/CoreChangesTests.fs]] is not that Actor. It posts from the test thread with no subgraph and no handle handoff.

## Main refinements

- **Normal, not Graph-only.** Parse and reconciliation already call `postGraphOnlyChange`. Issue 01 must not use that path.
- **No HTTP self-post.** Browser HTTP remains `Api.postChange`. The Actor calls Core in-process.
- **Test Actor is enough.** The earlier "production helper" idea in [[is-issue-01-produce-path-done.md]] is dropped for this issue. Alan asked for a test Actor. That is the 01 demo.
- **Subgraph in 01 is a value, not a Query.** The test passes a `Graph`. Who extracts a live subgraph at launch is out of 01.
- **"Separate thread" means off the apply mailbox.** Thread-pool `Async`/`Task`. Not a dedicated OS thread. Not a pool.
- **Full `CoreChanges` handle.** Not a send-only wrapper. Produce still Normal `postChange`.
- **Core "responds" is the `postChange` acknowledgement.** Poll stays the consume channel for Browsers. Await is optional except in the test Actor.

## Alan's locks (2026-09-05)

These close the four questions that were named on the issue Comments.

1. **Task vs dedicated OS thread.** Lightweight preemptive multitasking: thread-pool `Async`/`Task`, off the apply mailbox. Not a dedicated OS thread per Actor. No thread-per-Actor pool here (that is [[plan/core-creation/issues/02-core-actor-pool.md]]).
2. **Send hook vs full handle.** Full `CoreChanges` handle. Produce still goes through Normal `postChange`, not `postGraphOnlyChange`, and not HTTP self-post. `getState` / `getChangesSince` are available because the handle is full.
3. **Ack usage.** The protocol returns the acknowledgement. Awaiting it is optional for one-shot Actors. The test Actor awaits and asserts. Some Actors will care about errors; multi-round Actors will care about revisions. No job identity or cancel.
4. **Subgraph.** A test-constructed `Graph` is enough for this issue. Live extraction waits (Query / Files / pool).

Fire-and-forget hazard (named, not a lock): apply still runs once the `postChange` Async is started; dropping the await hides Error. An unstarted Async never posts. Process or test exit before the pool runs that work can drop the post.

## Sources

- [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]], [[plan/core-creation/issues/02-core-actor-pool.md]], [[plan/core-creation/project.md]], [[plan/core-creation/map.md]]
- [[plan/core-creation/reports/is-issue-01-produce-path-done.md]]
- [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/event-sourced-ops/architecture.md]]
- [[src/Server/Core/CoreChanges.fs]], [[src/Server/Api.fs]], [[src/Server/FileAgent.fs]], [[tests/Server.Tests/CoreChangesTests.fs]]
