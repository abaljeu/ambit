# 18 — Finish and drop

**Status:** ready-for-agent
**Blocked by:** [[15-launch-actor-and-hold-span.md|15 Launch an Actor and hold the span]]
Actual: 1h30m

## Context

When an Actor stops, Core must drop the job so later query fails and lock-present goes off. Earlier Posts from that sender must still apply. The 2026-09-07 delivery used a second pool mailbox and did not drop on failed stop. That product was rewound. Rebuild from this spec. Do not wrap-patch a pool mailbox.

## What to build

Exactly one Core mailbox: the Changes apply queue. Posts, cancel, delete-actor, launch, query, and admit-and-enqueue are fast messages on that mailbox. See [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]].

The task runner is a TaskPool (or equivalent). It runs Actors off the apply queue and may release the thread when the Actor returns or is terminated. It is not a mailbox.

The registry (public number, lock-present, credential, handle to terminate) is state of that one mailbox. The addressable ID stays until the mailbox processes delete-actor. The task ending does not remove the ID.

Any Actor stop, including a failed stop, enqueues a Core-only delete-actor item on that mailbox. It is not a Change. Callers do not get a job Error. FIFO is the mailbox order, not “the Actor awaited `postChange`.”

There is no credential MailboxProcessor. Admit and enqueue are one mailbox message.

Drop removes the Actor from the registry (public number, lock-present, credential). If the task is still running, async terminate; do not wait. If it has already stopped, terminate is a no-op. Cancel and finish share this drop.

- [ ] Any Actor stop, including failure, enqueues a Core-only delete-actor item that is not a Change.
- [ ] delete-actor applies after that sender's earlier Posts (FIFO is mailbox order).
- [ ] After delete-actor, the public number is gone, the credential is out of the set, and lock-present is off.
- [ ] Drop async-terminates only if the task is still running; terminate is a no-op if it has already stopped.
- [ ] Registry and admit-and-enqueue live on the one apply mailbox; the runner is not a mailbox.

## See also

[[11-define-actor-finish-and-failure-behavior.md]], [[02-core-actor-pool.md]], [[../reports/actor-pool-rewind-review.md]]

## Comments

- 2026-09-11 — Review of the first delivery: failed stop does not enqueue delete-actor; pool is a second mailbox. Do not patch in place. Product rewound. Notes: [[../reports/actor-pool-rewind-review.md]].

## Time

- 2026-09-07 ~1h — implemented finish-and-drop with tests
- 2026-09-11 30m — rewind set 1 product; tighten this spec (from chat)
