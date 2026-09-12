# 18 — Finish and drop

**Status:** blocked
**Blocked by:** [[plan/core-creation/issues/16-track-running-job.md]]
Actual: 1h40m

## Context

When an Actor reaches a terminal outcome, Core must append the durable result and drop the live Actor without reversing earlier accepted output. The 2026-09-07 delivery used a second pool mailbox, had no durable terminal Event, and did not drop on failed stop. That product was rewound. Rebuild from [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Do not wrap-patch a pool mailbox.

## What to build

Establish terminal, failure, and drop on the pool in [[02-core-actor-pool.md]]. Mailbox order and durable ActorFinished are [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. See [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]].

Succeeded, Failed, and Cancelled are Core-only terminal messages. The first terminal message appends exactly one ActorFinished (safe error when Failed; none when Cancelled). A duplicate terminal is ignored. Then drop the live registry and secret and terminate without waiting. Restart appends ActorFinished Interrupted for unmatched ActorStarted.

- [x] Succeeded, Failed, and Cancelled queue Core-only terminal messages that are not Changes.
- [x] The first terminal message runs after earlier queued Changes and appends exactly one ActorFinished.
- [x] After ActorFinished, the live registry and secret are gone while public Actor identity remains durable.
- [x] Drop async-terminates only if the task is still running; terminate is a no-op if it has already stopped.
- [x] Restart appends ActorFinished Interrupted for unmatched ActorStarted Events.

## See also

[[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]], [[plan/core-creation/issues/02-core-actor-pool.md]], [[plan/core-creation/reports/actor-pool-rewind-review.md]]

## Comments

- 2026-09-11 — Review of the first delivery: failed stop does not enqueue delete-actor; pool is a second mailbox. Do not patch in place. Product rewound. Notes: [[../reports/actor-pool-rewind-review.md]].
- 2026-09-11 — [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] replaced delete-only finish and no-result assumptions with durable ActorFinished, safe failure, Interrupted recovery, and terminal-before-drop order. Proof belongs to [[plan/core-creation/issues/27-prove-core-actor-lifecycle-with-testactor.md]].
- 2026-09-11 — Status is `blocked` by the preceding live-identity registry contract in [[plan/core-creation/issues/16-track-running-job.md]].
- 2026-09-11 — Marked locked finish-and-drop contract facts complete. Status stays `blocked`; the Phase 2 rebuild and 27 catalog are not this mark.

## Time

- 2026-09-07 ~1h — implemented finish-and-drop with tests
- 2026-09-11 30m — rewind set 1 product; tighten this spec (from chat)
- 2026-09-11 5m — drop restated mailbox and TaskPool; keep unique finish and drop (from chat)
- 2026-09-11 5m — mark locked finish-and-drop contract facts complete (from chat)
