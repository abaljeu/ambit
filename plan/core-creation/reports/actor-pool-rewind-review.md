# Actor pool rewind review

Date: 2026-09-11. Interactive review of the last three agent sets, starting at Core 18. No product patch on the current head. After the review, reset the working head to before those commits and reimplement from a tighter spec.

## How we work

Accumulate confirmed misses here. Do not file `ready-for-agent` patch tickets against the current pool mailbox. Tighten [[../issues/18-finish-and-drop.md]], [[../issues/02-core-actor-pool.md]], and [[../issues/11-define-actor-finish-and-failure-behavior.md]] only after this review names the intended shape. Then reset and rebuild.

## Rewind pin

Set 1 (Core 18) parent: `e0f92b9`. Commits to drop with that set: `f3eb420`, `351ce6c`, `0c85dff`, merge `05eb06f`.

Sets 2 and 3 sit later on `ready` (CloudAgents merge `4f974f0`; Create actor is still draft PR 4). Intervening `ready` work after 18 includes the fast-clear decision. Confirm at reset whether the pin is only set 1 or all three agent sets.

## Intended shape (confirmed)

- Exactly one Core mailbox: the Changes apply queue. Posts, cancel, and delete-actor are fast messages on that mailbox. See [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]].
- The Actor pool is a TaskPool (or equivalent). It runs Actors off the apply queue. It is not a mailbox and does not serialize launch, query, lock, or drop on a second inbox.
- Any Actor stop, including a failed stop, enqueues delete-actor on that one mailbox. Callers do not get a job Error.
- FIFO is the mailbox order, not “the Actor awaited `postChange`.” Awaiting Post is Actor-specific. Core must not rely on it.
- Job registry, lock set, and live credentials are state of that one mailbox. There is no credential `MailboxProcessor`. Admit and Post see that same state.
- Drop removes the Actor from the pool (public number, lock-present, credential). If the Actor task is still running, async terminate; do not wait. If it has already stopped, terminate is a no-op. Cancel and finish share this drop.

## Current implementation (to discard)

- [[src/Server/Core/CoreActorPool.fs]] is a `MailboxProcessor`. `Register`, `TryLaunch`, `Query`, `GetLocked`, and `DeleteActor` wait on that inbox. That is a second queue.
- The Actor body is `Async.Start`. Posts go to the FileAgent or DbAgent mailbox. `DeleteActor` is posted to the pool inbox after `do! plan.actor`.
- Failed Actor stop never reaches `DeleteActor`. The wrap has no catcher. Query, credential, and lock-present stay live. [[../issues/26-failed-actor-stop-still-drops.md]] recorded this, then was cancelled as a patch.
- The FIFO test awaits Post inside the test Actor, then sleeps. It does not put Post and delete-actor on one mailbox.
- `DeleteActor` does `do! credentials.remove` on a third `MailboxProcessor` ([[src/Server/Core/CoreCredentials.fs]]). Admit is `contains` on that processor, then enqueue on FileAgent or DbAgent. There is no reason for that third inbox. Drop should remove the job from the pool and async-terminate only if the task is still running. The current wrap waits for the Actor, then hops to remove the credential, and never terminates a live task.
- Standards on this delivery: `startMailbox` is 41 lines; four finish tests use `Task.Delay(100)`; [[tests/Server.Tests/CoreActorPoolTests.fs]] grew to 550 lines.

## Still open on set 1

Next: any leftover Core 18 items, then agent sets 2 and 3.

## Time

- 2026-09-11 — Core 18 interactive review; rewind/redo plan (from chat)
