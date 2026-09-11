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

## Set 3 — Create cloud-agent posts reply under Focus

PR 4 DRAFT. Branch `cursor/cloud-agent-actor-posts-reply-0b7d`. Commits vs `ready`: `eb5c4bf` Add cloud-agent actor with POST /ambit/actors endpoint; `68d14a2` Add issue 05 and update project stage to slice; `d79c706` Update llm-connector project stage to slice; `8ba1e97` Merge origin/ready. Spec: [[plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md]], [[plan/llm-connector/reports/grill-run-agent-actor-2026-09-08.md]].

**Miss (confirmed):** Identify work by name, not by a number. `68d14a2` subject is “Add issue 05…”. The merge `8ba1e97` body says “authoritative issue 05”. The name is Create cloud-agent posts a reply under Focus ([[plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md]]).

**Miss (confirmed):** [[tests/Server.Tests/CloudAgentActorTests.fs]] does not prove Create → launch → reply under Focus. The spec asks a Server test with cookie, public number, and Owned children when the job finishes. The facts do not POST `/ambit/actors`. Registration accepts any Error except `unknown actor`. Extract copies the `?` trim locally and does not call the Actor. Nothing waits until the public number is gone or asserts reply children. Same family as set 2: the tests do not prove the path. TestActor does not replace CloudAgents HTTP-seam tests or CloudAgent Actor tests.

**Miss (confirmed):** Focus, lock, and extract/pack are three different things. Focus is the write-back parent: the parent of the nodes that the Actor replaces. Usually that child list is empty. Lock is Focus, not the extract set. Extract/pack is rootnode plus nodelist (the set can be larger than Focus). Map extract/pack at the edge. Do not revise Define the Core Command launch contract ([[plan/core-creation/issues/09-define-core-command-launch-contract.md]]) in this slice. Delivery mashed extract, lock, and replace-parent onto one Core span. [[src/Server/Api.fs]] `payloadToLaunchRequest` (PR 4 branch `cursor/cloud-agent-actor-posts-reply-0b7d`) requires nodelist items as children of Focus, and it errors on an empty list. Core then locks `spanIds` of that child range. [[src/Server/CloudAgentActor.fs]] treats `subgraph.root` as Focus. Consequence: the usual case (Focus with empty children) cannot launch.

**Miss (confirmed):** Creating reply nodes has the same shape as paste, but Focus’s old children are dropped. Replace the child list; do not append. Delivery `addChildrenToFocus` in [[src/Server/CloudAgentActor.fs]] does `Op.Replace(..., focusNode.children @ newChildRefs)`, which appends.

**Miss (confirmed):** The agent reply is an Md tree. Alan’s example:

```md
## Section A
Section A text

## Section B
### Subsection C
Section D text
### Subsection D
Subsection D text
```

Section A and Section B are Owned children of Focus. Subsection C and Subsection D nest under Section B. Delivery `markdownToGraph` in [[src/Server/CloudAgentActor.fs]] keeps only the Md document-root’s direct children, then `Op.NewNode(id, header text)` plus optional CSS. Nested Owned from the Md read never get ops. Redo: paste-shaped create of that whole reply tree.

## Still open

Set 3 interactive review continues. Confirmed misses stay in this file. No product patch. No wrap tickets.

## Time

- 2026-09-11 — Core 18 interactive review; rewind/redo plan (from chat)
- 2026-09-11 — Set 3 pin confirmed; first miss: name, not number (from chat)
- 2026-09-11 — Set 3 miss: tests do not prove Create → reply (from chat)
- 2026-09-11 — Set 3 miss: Focus is replace parent, lock Focus (from chat)
- 2026-09-11 — Set 3 miss: paste-replace Focus children, do not append (from chat)
- 2026-09-11 — Set 3 miss: reply is Md tree; nested Owned need ops (from chat)
