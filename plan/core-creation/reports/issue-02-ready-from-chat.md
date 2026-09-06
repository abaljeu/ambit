# Issue 02 ready from chat?

Date: 2026-09-05

Question: After the chat locked the Actor produce protocol for [[../issues/01-generalized-server-actor-produce-path.md]], is [[../issues/02-core-actor-pool.md]] specified enough for an AFK agent to build?

Verdict: **waits on 09–12.** Full issue 02 stays **needs-info**. It is not ready to build. On 2026-09-05 Alan locked the slice: wait on the 09–12 grill. Do not start a launch-plus-identity increment. Questions 2–6 stay unanswered; they belong to those grilling tickets.

Issue 01 Status is now `done` ([[implement-issue-01-actor-produce-path.md]]). That does not unlock 02. The produce protocol is a given. It is not a pool spec.

## What 02 currently says

Status: `needs-info`.

Blocked by: [[../issues/09-define-core-command-launch-contract.md]], [[../issues/10-define-actor-cancellation-and-output-admission.md]], [[../issues/11-define-actor-finish-and-failure-behavior.md]], and [[../issues/12-define-actor-pool-shutdown-behavior.md]]. All four are Type grilling, Status `open`. Issue 01 is `done` and is no longer a blocker. Alan locked 2026-09-05: 02 waits on 09–12, not a launch-plus-identity increment.

What to build: Core-owned Actor pool machinery. Launch long-running work off the apply queue. Assign Core-owned job identity. Cancel further output from a job. Finish through Core Changes and inner apply. The apply queue stays available while the Actor runs.

Checklist: launch returns job identity without holding the apply queue; cancel does not Undo merged Changes; a finishing Actor posts through Core Changes; cancellation and cancel-after-enqueue are specified before code; Actor definitions, Browser chrome, and advisory soft-lock stay out.

Exact pool packaging and API are not specified. Actor definitions stay outside Core. Soft-lock status is later.

## What this chat locked (01 only)

These locks apply to the produce path. They do not specify the pool.

- An Actor is thread-pool `Async` or `Task`, off the FileAgent and DbAgent apply mailbox. It is not a dedicated OS thread. Issue 01 still names a thread-per-Actor pool as 02's possible packaging. That packaging is not locked.
- The Actor receives a subgraph (`Graph` / Local Graph) and the full `CoreChanges` handle. Produce is Normal `postChange`. Not `postGraphOnlyChange`. Not HTTP self-post.
- Core answers `postChange` with `CoreChangesAccepted` or Error. The test Actor awaits that acknowledgement. One-shot Actors may fire-and-forget.
- For 01 the test constructs the `Graph`. Live subgraph extraction waits on Query, Files, or the pool.
- Apply is mailbox push plus a one-shot `AsyncReplyChannel`. There is no Actor response queue.
- One test Actor is enough for 01. Production Actors, Command, cancel, job identity, finish job state, and shutdown stay out of 01.

Issue 02 must reuse that produce path. It must not invent a second one.

## What 02 still needs

[[plan/event-sourced-ops/details/actors-and-jobs.md]] is assessment, not a lock. It sketches launch, job identity mapped to a cancellation source, finish by posting Changes, Poll for consume, and no cancel-after-enqueue. Issue 02's own checklist requires that cancel design before implementation. Ticket 10 still owns the cancel question.

| Concern | Chat / 01 | Issue 02 / 09–12 |
| --- | --- | --- |
| Produce path | Locked. 01 `done`. | Reuse. Do not redo. |
| Pool identity | Out of 01. | Named. Shape not specified. Ticket 09 asks what launch returns and what Core retains. |
| Launch surface | Test Actor started in the test. | 02 wants launch off the apply queue. Ticket 09 asks what typed Command input selects and launches an Actor definition. |
| Subgraph extraction | Test-constructed `Graph`. | Live extraction waits on [[../issues/08-define-core-query-contract.md]], [[../issues/07-define-core-files-contract.md]], or the pool. Not locked for 02. |
| Cancel | Out of 01. | In 02's checklist. Ticket 10 is open (cutoff, admit/refuse through Changes, cancel-after-enqueue). |
| Command | Out of 01. | Ticket 09 open. [[asynchronous-core-task-manager-facts.md]] recommends Command as the launch seam. That is not a product lock. |
| Finish | Last `postChange` in 01. | 02 says finish through Core Changes. Ticket 11 asks terminal job state, caller-visible result, and effect of accepted, deduplicated, or rejected final Changes. |
| Shutdown | Out of 01. | 02 waits on 09–12. Ticket 12 is open. |
| Production Actors | Out. Definitions stay outside Core. | Still out of 02. First definition is [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]]. |
| Concurrent Actors | One test Actor. | No max, no fairness, no cap. Shell needs several concurrent jobs; that is later. |
| Packaging | Thread-pool `Async`/`Task` for 01. | Thread-per-Actor still named as 02. Unresolved. |

Tickets 09–12 exist. Each is Type grilling, Status `open`, Question only. None is resolved. Map topology: Command (09) gates cancel (10) gates finish (11) gates shutdown (12) gates issue 02.

## Smallest questions before an AFK agent implements

1. **Slice.** **Answered 2026-09-05.** Wait on 09–12. Alan: "oh i see we need 9-12 first." No launch-plus-identity increment now. Keep 12 as a blocker. Status stays `needs-info`.

Questions 2–6 stay unanswered. Do not invent packaging, launch, subgraph, concurrency, or job identity. They belong to 09–12.

2. **Packaging.** Is the pool thread-pool `Async`/`Task` (same as 01), or a dedicated OS thread per Actor?
3. **Launch without Command.** May the test construct the pool launch (same style as the 01 test Actor), or must launch wait for the Command contract (09)?
4. **Subgraph.** Is a test-constructed `Graph` still enough, or must this increment extract a live subgraph (blocked by 07/08)?
5. **Concurrency.** How many concurrent Actors must this increment prove: one, or more than one? No cap is specified.
6. **Job identity shape.** What does launch return (opaque Core-owned id)? What does Core retain (map to a running task and a cancellation source even if cancel waits)?

Do not grill Parse, shell, Agent, Browser chrome, or advisory soft-lock in this lock. Those stay out of 02.

## Sources

- [[../issues/02-core-actor-pool.md]], [[../issues/01-generalized-server-actor-produce-path.md]], [[../project.md]], [[../map.md]]
- [[../issues/09-define-core-command-launch-contract.md]], [[../issues/10-define-actor-cancellation-and-output-admission.md]], [[../issues/11-define-actor-finish-and-failure-behavior.md]], [[../issues/12-define-actor-pool-shutdown-behavior.md]]
- [[issue-01-vs-02-what-waits.md]], [[issue-01-actor-protocol-hypothesis.md]], [[implement-issue-01-actor-produce-path.md]], [[asynchronous-core-task-manager-facts.md]]
- [[plan/event-sourced-ops/details/actors-and-jobs.md]]
