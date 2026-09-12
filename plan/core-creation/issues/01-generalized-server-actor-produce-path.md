# 01 — Generalized Server Actor produce path

**Context:** Browser Changes and Server-side Actors must share one Core-owned mutation path. Issues 03–06 delivered typed Normal `CoreChanges.postChange` and a test that posts from the test thread. That kernel is not an Actor. Merge and consume after a Change arrives are accepted. The Actor produce protocol below is locked.

**What to build:** One Actor produce protocol on Normal Core Changes. A Server-side Actor hands Change objects into Core without HTTP self-post and without Graph-only. That Change is amended, logged, and Poll-visible like a Browser-posted Change. Core is the sole Server Graph writer.

Protocol:

- An Actor is an F# `Async` work unit, or a .NET `Task`. It runs as lightweight preemptive multitasking on the thread pool, off the FileAgent and DbAgent apply mailbox, so that long work does not hold the Changes queue. It is not a dedicated OS thread per Actor. A thread-per-Actor pool is [[02-core-actor-pool.md]].
- At start the Actor receives a subgraph and the full `CoreChanges` handle. The subgraph is a `Graph` value. It is the Actor's Local Graph and the common prior for the Change it will plan. There is no second Graph type. For this issue the test constructs that `Graph`. Live subgraph extraction at launch waits for Query, Files, or the pool ([[08-define-core-query-contract.md]], [[07-define-core-files-contract.md]], [[02-core-actor-pool.md]]).
- The Actor produces through Normal `CoreChanges.postChange` on that handle. It is not `postGraphOnlyChange`. It is not an HTTP self-post. `getState` and `getChangesSince` are available because the handle is full.
- Core answers `postChange` with `CoreChangesAccepted` or an Error string. That answer is the produce acknowledgement: `revision`, confirmed Changes, `externalChanges`, `message`, and `isReady`. Other Browsers still consume by Poll (`Api.getPoll` / `getChangesSince`). There is no completion push.
- Awaiting the acknowledgement is optional for one-shot Actors (call, do not await, quit). Some Actors will care about errors. Multi-round Actors will care about revisions. The test Actor awaits and asserts the acknowledgement.
- The Actor may do more work before or after it sends.
- One test Actor that follows this protocol is enough for this issue. The existing typed Normal test in [[tests/Server.Tests/CoreChangesTests.fs]] is not that Actor. Production pool, cancel, job identity, and Command stay in [[02-core-actor-pool.md]] and [[09-define-core-command-launch-contract.md]], [[10-define-actor-cancellation-and-output-admission.md]], [[11-define-actor-finish-and-failure-behavior.md]], [[12-define-actor-pool-shutdown-behavior.md]].

**Blocked by:** none remaining. Prior blockers are done or resolved: [[plan/event-sourced-ops/issues/03-server-amends-recoverable-field-collisions.md]], [[plan/event-sourced-ops/issues/04-client-consumes-merge-success-without-reload.md]], [[06-ready-the-initial-core-changes-increment.md|Ready the initial Core Changes increment]]

**See also:** [[plan/core-creation/project.md]], [[plan/core-creation/initial-core-changes-implementation.md|Initial Core Changes implementation (enables this issue)]], [[plan/core-creation/reports/kernel-fsproj.md]], [[plan/core-creation/reports/is-issue-01-produce-path-done.md]], [[plan/core-creation/reports/issue-01-actor-protocol-hypothesis.md]], [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/event-sourced-ops/architecture.md]]

**Status:** done

**Actual:** 1h35m

- [x] A Server-side producer can submit a Change through Core Changes without HTTP self-post.
- [x] That Change is amended, logged, and visible on Poll like a Browser-posted Change.
- [x] A test Actor runs off the apply mailbox, receives a subgraph and the full `CoreChanges` handle, and posts at least one Change through Normal `CoreChanges.postChange`.
- [x] The test Actor awaits and asserts the produce acknowledgement.
- [x] Core is the sole Server Graph writer.
- [x] Auth and malformed failures remain Reject; this issue does not invent multi-job identity or soft-lock UI.

## Comments

- 2026-09-05 — Issues 03–06 delivered the typed Core Changes kernel and Browser HTTP path. That work enables this issue; it does not add a production Server Actor Normal produce path. `needs-info` is stale. See [[plan/core-creation/reports/is-issue-01-produce-path-done.md]].
- 2026-09-05 — Alan offered an Actor protocol (Task, separate thread, subgraph, async send hook, Core responds, more work allowed, one test Actor). The **What to build** block is that hypothesis, refined against existing Normal `postChange`. Named tensions, not silent locks: (1) **Task / Async / thread.** Intent is off the apply mailbox, not a dedicated OS thread. F# `Async` and .NET `Task` use the thread pool. The apply queue is already a `MailboxProcessor`. A thread-per-Actor pool is issue 02. (2) **Send hook vs `postChange`.** The hook is probably `Change list -> Async<Result<CoreChangesAccepted, string>>`, which is the `postChange` signature. A send-only wrapper keeps Graph-only out of the Actor. Passing the full `CoreChanges` handle would also let the Actor `getState` / `getChangesSince`. Not locked. (3) **Who supplies the subgraph.** For this issue the test constructs a small `Graph` and passes it in. Production extraction at launch belongs to Query, Files, or the pool ([[08-define-core-query-contract.md]], [[07-define-core-files-contract.md]], [[02-core-actor-pool.md]]), not here. (4) **Test Actor vs production caller.** Alan's test Actor is enough for 01. A shipped Parse or shell Actor, Command launch, and pool identity are later. (5) **Core responds.** The hook return is the produce acknowledgement, same facts `Api.postChange` encodes today. Poll is how other Browsers consume. Do not invent a completion push. Whether the Actor must use the confirmed (amended) Changes, or only Ok/Error, is not locked.
- 2026-09-05 — Alan locked the four protocol questions: thread-pool `Async`/`Task` (not a dedicated OS thread; pool is 02); full `CoreChanges` handle (produce still Normal `postChange`); acknowledgement is returned, await optional for one-shot Actors, test Actor awaits and asserts; test-constructed subgraph is enough (Query/Files/pool extraction waits). Fire-and-forget: apply still runs once the `postChange` Async is started (`Async.Start`); dropping the await hides Error. An unstarted Async never posts. Process or test exit before the pool runs that work can drop the post. No new lock.
- 2026-09-05 — Implemented the test Actor in [[tests/Server.Tests/CoreChangesTests.fs]]. See [[plan/core-creation/reports/implement-issue-01-actor-produce-path.md]]. Pool and Command stay out.
- 2026-09-11 — Completed-detail pass from [[29-prove-testactor-hello.md]]: produce-path proof boxes stay `[x]`. This delivery is not 29's hello proof.

## Time

- 2026-09-05 30m — refined Actor produce protocol hypothesis (from chat)
- 2026-09-05 15m — folded Alan's four protocol locks (from chat)
- 2026-09-05 45m — test Actor on Normal `CoreChanges.postChange` (from chat)
- 2026-09-11 5m — confirm produce-path proof boxes stay complete (from chat)
