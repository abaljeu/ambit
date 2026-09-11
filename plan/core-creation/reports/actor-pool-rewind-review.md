# Actor pool rewind review

Date: 2026-09-11. Interactive review of the last three agent sets, starting at Core 18. No product patch on the current head. After the review, reset the working head to before those commits and reimplement from a tighter spec.

## How we work

Accumulate confirmed misses here. Do not file `ready-for-agent` patch tickets against the current pool mailbox. Tighten [[../issues/18-finish-and-drop.md]], [[../issues/02-core-actor-pool.md]], and [[../issues/11-define-actor-finish-and-failure-behavior.md]] only after this review names the intended shape. Then reset and rebuild.

## Rewind pin

Set 1 (Core 18) parent: `e0f92b9`. Commits to drop with that set: `f3eb420`, `351ce6c`, `0c85dff`, merge `05eb06f`.

Sets 2 and 3 sit later on `ready` (CloudAgents merge `4f974f0`; Create actor is still draft PR 4). Intervening `ready` work after 18 includes the fast-clear decision. Confirm at reset whether the pin is only set 1 or all three agent sets.

## Intended shape (confirmed)

- Exactly one Core mailbox: the Changes apply queue. Posts, cancel, delete-actor, launch, query, and admit-and-enqueue are fast messages on that mailbox. See [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]].
- Distinguish the task runner from the registry. The runner is a TaskPool (or equivalent): it runs Actors off the apply queue and may release the thread when the Actor returns or is terminated. It is not a mailbox.
- The registry (public number, lock-present, credential, handle to terminate) is state of that one mailbox. The addressable ID stays until the mailbox processes delete-actor. The task ending does not remove the ID.
- Any Actor stop, including a failed stop, enqueues delete-actor on that mailbox. Callers do not get a job Error.
- FIFO is the mailbox order, not “the Actor awaited `postChange`.” Awaiting Post is Actor-specific. Core must not rely on it.
- There is no credential `MailboxProcessor`. Admit and enqueue are one mailbox message.
- Drop removes the Actor from the registry (public number, lock-present, credential). If the task is still running, async terminate; do not wait. If it has already stopped, terminate is a no-op. Cancel and finish share this drop.

## Current implementation (to discard)

- [[src/Server/Core/CoreActorPool.fs]] is a `MailboxProcessor`. `Register`, `TryLaunch`, `Query`, `GetLocked`, and `DeleteActor` wait on that inbox. That is a second queue.
- The Actor body is `Async.Start`. Posts go to the FileAgent or DbAgent mailbox. `DeleteActor` is posted to the pool inbox after `do! plan.actor`.
- Failed Actor stop never reaches `DeleteActor`. The wrap has no catcher. Query, credential, and lock-present stay live. [[../issues/26-failed-actor-stop-still-drops.md]] recorded this, then was cancelled as a patch.
- The FIFO test awaits Post inside the test Actor, then sleeps. It does not put Post and delete-actor on one mailbox.
- `DeleteActor` does `do! credentials.remove` on a third `MailboxProcessor` ([[src/Server/Core/CoreCredentials.fs]]). Admit is `contains` on that processor, then enqueue on FileAgent or DbAgent. There is no reason for that third inbox. Drop should remove the job from the pool and async-terminate only if the task is still running. The current wrap waits for the Actor, then hops to remove the credential, and never terminates a live task.
- Standards on this delivery: `startMailbox` is 41 lines; four finish tests use `Task.Delay(100)`; [[tests/Server.Tests/CoreActorPoolTests.fs]] grew to 550 lines.

## Set 1 closed

No further independent Core 18 misses. Redo is the pool/mailbox shape above, not a wrap patch.

## Set 2 — CloudAgents stack

PR 3, merged `4f974f0`. Commits: `1b9874e` Add standalone CloudAgents stack; `2c521f7` Lead with no-repo agents in documentation. Spec: [[plan/llm-connector/reports/first-agent-cursor-cloud-agents.md]].

**Miss (confirmed):** [[tests/CloudAgents.Tests/]] does not prove the stack. `AgentRunner.start` hits live `api.cursor.com`. Facts accept any Error or Ok. `PublicTypesTests` only construct records. CloudAgents is the Cursor connector; it needs its own tests (HTTP seam, no live key). TestActor does not replace those tests.

**Three test layers (confirmed):**

1. CloudAgents library — async start/poll/cancel; fake HTTP; no Ambit refs.
2. TestActor — Actor *system* only. No Cursor. Existing Server Core tests (`dotnet test` on FileAgent / Core).
3. CloudAgent Actor — optional later facts for that Actor’s pack/reply path. Not TestActor.

**TestActor (not implemented):** ActorName `test`. Focus Header is the case id (not `?`). TestActor switches on that line, does only what the case needs, and `postChange`s Owned children under Focus. It does not Assert and does not return a job result. Outer fact: launch, wait until the public number is gone, match Graph to a table for that case. First cases: `echo`, `fail`, `post-twice`. No `cloud-stub` in this Actor.

**Confirmed algorithm:** the Actor is not a foreground worker. Await POST (ids), then await each poll GET; between polls `do! Async.Sleep` (or later stream). Wake when that HTTP response arrives. Do not `Thread.Sleep` or `RunSynchronously` on the Actor path. Sync `waitUntilComplete` is console-only, or drop it. CloudAgents must not leak exceptions: HTTP failures are `AgentError`. Keep `cancel` in the library now (Core/Actor cancel protocol can stay later).

**Set 2 leftovers dropped:** `try`/`with` at the HTTP edge is the no-leak rule, already present. Library `cancel` is in scope.

## Still open

Set 3 (Create `cloud-agent` posts reply) is for a **new main** agent. Handoff: [[plan/core-creation/reports/set3-review-handoff.md]]. This run does not start set 3.

## Time

- 2026-09-11 — Core 18 interactive review; rewind/redo plan (from chat)
