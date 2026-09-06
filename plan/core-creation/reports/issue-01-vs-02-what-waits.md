# Issue 01 vs 02: what waits

Date: 2026-09-05

Question: On the Actor produce path, what exists in code now, what [[../issues/01-generalized-server-actor-produce-path.md]] still builds, and what waits for [[../issues/02-core-actor-pool.md]]?

Answer: Issues 03–06 delivered the typed Core Changes seam, the FileAgent and DbAgent apply mailbox, Browser HTTP, and Graph-only Parse. Issue 01 is still the first test Actor that uses that Normal seam. The pool, Command, cancel, job identity, shutdown, production Actors, and live subgraph extraction wait.

Issue 01 Status: `ready-for-agent`. Issue 02 Status: `needs-info`. Project Stage: `active`. No issue file change.

## Already implemented

Issues 03–06 are Status `resolved`. Code today:

- Typed [[src/Server/Core/CoreChanges.fs]] with Normal `postChange` and Parse-reserved `postGraphOnlyChange`. Both return `CoreChangesAccepted` or Error. The handle also exposes `getState` and `getChangesSince`.
- Production selection in [[src/Server/Core/CoreRuntime.fs]]. Routes take `CoreChanges`. Agent `postChange` functions are private.
- FileAgent and DbAgent `MailboxProcessor` is the sole apply writer. `postChange` is `PostAndAsyncReply`: mailbox push plus a one-shot `AsyncReplyChannel`. There is no Actor inbox. See [[issue-01-cross-thread-postchange.md]].
- Browser HTTP: `Api.postChange` decodes JSON, calls Normal `postChange`, encodes the acknowledgement.
- Parse, `GraphOnlyChangePost`, and lazy-load reconciliation call `postGraphOnlyChange` in-process. That path is not the Actor produce path.
- Amend, log, and Poll (`getChangesSince` / `Api.getPoll`) already run once a Change reaches Normal apply. Event-sourced-ops issues 03 and 04 are Status `done`.
- A test-only Normal caller in [[tests/Server.Tests/CoreChangesTests.fs]] posts from the test thread with no subgraph and no handle handoff. That test is not an Actor.

The only production Normal caller is Browser HTTP. There is no Server-side Actor.

## Issue 01 still to build

The protocol on the issue is locked. The remaining work is one test Actor that follows it:

- Run as thread-pool `Async` or `Task`, off the FileAgent and DbAgent apply mailbox. Not a dedicated OS thread.
- Receive a test-constructed `Graph` (Local Graph / common prior) and the full `CoreChanges` handle.
- Produce through Normal `postChange`. Not `postGraphOnlyChange`. Not HTTP self-post.
- Await and assert the produce acknowledgement (`CoreChangesAccepted` or Error). Await is optional for later one-shot Actors; this test awaits.
- Prove the Change is amended, logged, and Poll-visible like a Browser Change. Core stays the sole Server Graph writer. Auth and malformed stay Reject.

That is the whole issue. The existing typed Normal test does not satisfy it.

## Waits for issue 02 (and 09–12)

Issue 02 is blocked by issue 01 and by [[../issues/12-define-actor-pool-shutdown-behavior.md]]. Pool packaging is still unspecified (`needs-info`). Tickets 09–12 are open grilling.

Do not put these in issue 01:

- Actor pool, long-running launch off the apply queue, Core-owned job identity, and a thread-per-Actor pool if that is the chosen packaging ([[../issues/02-core-actor-pool.md]]).
- Command launch of an Actor definition ([[../issues/09-define-core-command-launch-contract.md]]).
- Cancel and output admission ([[../issues/10-define-actor-cancellation-and-output-admission.md]]).
- Finish and failure job state ([[../issues/11-define-actor-finish-and-failure-behavior.md]]).
- Pool shutdown ([[../issues/12-define-actor-pool-shutdown-behavior.md]]).
- Production Actor definitions (Parse realignment, shell). Definitions stay outside Core. First definition is [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]].
- Live subgraph extraction at launch: Query ([[../issues/08-define-core-query-contract.md]]), Files ([[../issues/07-define-core-files-contract.md]]), or the pool. Issue 01 uses a test-constructed `Graph`.
- Advisory soft-lock UI ([[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]]).

Issue 02's "finishing Actor submits Change objects through Core Changes" uses the produce path that issue 01 proves. It is not a second produce protocol.

## Sources

- [[../issues/01-generalized-server-actor-produce-path.md]], [[../issues/02-core-actor-pool.md]], [[../project.md]], [[../map.md]]
- [[is-issue-01-produce-path-done.md]], [[issue-01-actor-protocol-hypothesis.md]], [[issue-01-cross-thread-postchange.md]]
- [[plan/event-sourced-ops/details/actors-and-jobs.md]]
- [[src/Server/Core/CoreChanges.fs]], [[src/Server/Core/CoreRuntime.fs]], [[src/Server/Api.fs]], [[src/Server/FileAgent.fs]], [[tests/Server.Tests/CoreChangesTests.fs]]
