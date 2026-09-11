# 18 — Finish and drop

**Status:** blocked
**Blocked by:** [[plan/core-creation/issues/16-track-running-job.md]]
Actual: 1h30m

## Context

When an Actor reaches a terminal outcome, Core must append the durable result and drop the live Actor without reversing earlier accepted output. The 2026-09-07 delivery used a second pool mailbox, had no durable terminal Event, and did not drop on failed stop. That product was rewound. Rebuild from [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Do not wrap-patch a pool mailbox.

## What to build

Exactly one Core mailbox orders launch, Change, cancel, Succeeded, Failed, Cancelled, and drop. See [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]].

The task runner is a TaskPool (or equivalent). It runs Actors off the apply queue and may release the thread when the Actor returns or is terminated. It is not a mailbox.

The registry (public Actor identity, secret credential, termination handle, and Focus NodeId) is state of that one mailbox. Public Actor identity remains durable in lifecycle Events after the live registry is removed. The secret never persists.

Succeeded, Failed, and Cancelled are Core-only terminal messages, not Changes. Processing the first terminal message after earlier queued Changes durably appends ActorFinished. Failed stores a safe domain error; raw provider details stay in logs. Cancelled has no Error or Change. A duplicate terminal message is ignored.

There is no credential MailboxProcessor. Admit and enqueue are one mailbox message.

After ActorFinished is durable, drop synchronously removes the live registry and revokes the secret. If the task is still running, request asynchronous termination; never wait. Cancel and completion share this path. On restart, append ActorFinished Interrupted for each unmatched ActorStarted.

- [ ] Succeeded, Failed, and Cancelled queue Core-only terminal messages that are not Changes.
- [ ] The first terminal message runs after earlier queued Changes and appends exactly one ActorFinished.
- [ ] After ActorFinished, the live registry, secret credential, and Focus NodeId are gone while public Actor identity remains durable.
- [ ] Drop async-terminates only if the task is still running; terminate is a no-op if it has already stopped.
- [ ] Registry and admit-and-enqueue live on the one apply mailbox; the runner is not a mailbox.
- [ ] Restart appends ActorFinished Interrupted for unmatched ActorStarted Events.

## See also

[[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]], [[plan/core-creation/issues/02-core-actor-pool.md]], [[plan/core-creation/reports/actor-pool-rewind-review.md]]

## Comments

- 2026-09-11 — Review of the first delivery: failed stop does not enqueue delete-actor; pool is a second mailbox. Do not patch in place. Product rewound. Notes: [[../reports/actor-pool-rewind-review.md]].
- 2026-09-11 — [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] replaced delete-only finish and no-result assumptions with durable ActorFinished, safe failure, Interrupted recovery, and terminal-before-drop order. Proof belongs to [[plan/core-creation/issues/27-prove-core-actor-lifecycle-with-testactor.md]].
- 2026-09-11 — Status is `blocked` by the preceding live-identity registry contract in [[plan/core-creation/issues/16-track-running-job.md]].

## Time

- 2026-09-07 ~1h — implemented finish-and-drop with tests
- 2026-09-11 30m — rewind set 1 product; tighten this spec (from chat)
