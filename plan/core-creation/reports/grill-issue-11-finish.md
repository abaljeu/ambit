# Grill issue 11 — Actor finish and failure behavior

Date: 2026-09-05

Session: closed. Q1–Q8 recorded. Q9/Q10 withdrawn. Q11=A. Alan locked the contract. [[../issues/11-define-actor-finish-and-failure-behavior.md]] is Status resolved. Facts: [[grill-issue-11-facts.md]].

## What I read

Project files: [[plan/core-creation/project.md]], [[plan/core-creation/map.md]], issues 01–03, 08–12. Glossary: [[CONTEXT.md]]. Assessment: [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/event-sourced-ops/details/soft-lock.md]], [[plan/event-sourced-ops/details/completing-ops.md]]. Locked: [[../issues/03-define-typed-core-changes-contract.md]], [[../issues/09-define-core-command-launch-contract.md]], [[../issues/10-define-actor-cancellation-and-output-admission.md]]. Facts: [[grill-issue-11-facts.md]].

## Blockers 03, 09, 10

All three are Type grilling, Status resolved. Issue 11 Blocked by those is therefore clear. Status was open; this session claims it as grilling. Shutdown stays [[../issues/12-define-actor-pool-shutdown-behavior.md]]. Issue 02 stays needs-info.

## Weakest assumption

The ticket assumes Core owns distinct success and failure transitions, retains terminal job state, exposes a result or error to callers, and lets accepted, deduplicated, or rejected final Changes change that terminal state.

At grill start, [[../issues/09-define-core-command-launch-contract.md]] said: after the task ends, query by the public number fails because the number is gone. Later, 09 Answer matches this ticket: query fails after delete-actor applies. [[plan/event-sourced-ops/details/actors-and-jobs.md]] says the launch request is long since complete; the Browser Polls; there is no completion push. Issue 01 already returns `Ok` / `Error` from `postChange` to the Actor, not to the Command caller. If those stand, there is no Core job result to retain, and final Changes affect Graph and History only.

ESO **Complete** is Server fill-in of missing Ops inside one Change, not job end. Do not use Complete for finish.

## Design tree

Locked. See ## Locked contract. Q11=A. Shutdown stays [[../issues/12-define-actor-pool-shutdown-behavior.md]].

## Why this question first

Identity, cancel, and admission are locked. The rotting joint is whether "finish" amends 09 (keep a terminal record) or only drops the map entry. Asking how Rejected Changes affect terminal state first would invent a record 09 already deleted.

## Round 1

❓ **Q1** - **Does Core keep a job result after the Actor stops?**

A. No. When the Actor task stops, Core drops the 09 number and clears the lock. Query fails. Callers see Graph and History only through Poll. `postChange` `Ok` / `Error` stays the Actor's Changes result, not a job terminal. Success and failure are not Core job states.

B. Yes. Core retains a queryable terminal (success / fail, maybe cancel) after the task stops. This amends 09 ("number is gone").

C. Core retains terminal facts internally only. Query still fails. Callers still Poll.

➡️ A. Matches 09, ESO "no completion push", and issue 01. Soft-lock already says job completion clears the lock. When "stops" means (return vs mailbox drain) waits on this.

**Answer (Alan, 2026-09-05):** Discard that. "Job result" as completed/aborted is not the question. No such status.

## Round 2

❓ **Q2** - **When does Core drop the 09 number and clear the lock?**

A. When the Actor Async/Task returns or faults, even if that sender still has a Change in the mailbox.

B. After the Actor Async/Task returns or faults, and after that sender's already-enqueued Changes have applied or Rejected.

C. Name another moment.

➡️ B. 10 already applies mailbox messages. If A, a new launch can take those NodeIds while this sender's Change still applies. Shutdown drain stays in 12.

**Answer (Alan, 2026-09-05):** Neither A nor B. When an Actor has stopped, queue a lock-clear request on the mailbox. FIFO then applies earlier posts from that sender before the lock clears.

## Round 3

❓ **Q3** - **What is that lock-clear mailbox item?** After stop, 10 has already removed a cancelled source. A post that uses the 09 send credential would be Unauthorized.

A. A Core-only control message. Core enqueues it. It is not a Change. It does not use the Actor sender id.

B. A Graph Change through Core Changes (Poll-visible). Amends 10: lock was a retained flag, not a Graph field.

C. Name another object.

➡️ A. 10 lock is a Core flag. A Change would need admission after the Actor is gone. History should not record lock chrome.

**Answer (Alan, 2026-09-05):** A. Lock status is on the Node, communicated to clients, but not recorded in History. He added: you may record it but never act on that fact. This amends 10 Q6 (no Node field; span lookup) toward a Node field. 10's lock-present flag may become that field.

## Round 4

❓ **Q4** - **May History contain lock status?**

A. Never. Core writes lock status on the Node. Clients see it through state, Fetch, or Query. No Action. Poll does not carry lock as a Change.

B. History may contain it. Apply, merge, amend, and Undo never act on it.

➡️ A. B still Polls lock chrome and invites Undo of a lock. 10 already said cancel is not Undo. "Never act" is a second unused log.

**Answer (Alan, 2026-09-05):** A. History never contains lock status.

## Round 5

❓ **Q5** - **When does the 09 number leave the map?**

A. When the Actor has stopped and Core queues lock-clear. Query then fails while Nodes are still locked.

B. When lock-clear applies and Core writes lock off the Node. Query works until then.

➡️ B. If A, cancel(NodeId) during drain has a lock and no job. 09 "after the task ends" becomes "after lock-clear applies."

❓ **Q6** - **When does Core write lock onto the Node at launch?**

A. Immediately in Command launch. Not a mailbox item.

B. A Core-only mailbox message, same kind as lock-clear.

➡️ A. 10 already creates the lock at launch. A mailbox set can sit behind a Browser Change, so clients would see an unlocked Node while the job already runs.

**Answer (Alan, 2026-09-05):** 5B, 6A.

## Round 6

❓ **Q7** - **What does the Node carry?** This amends 10's span-lookup may-change.

A. Lock-present only. Cancel still finds the job by span membership. The public number stays the query key.

B. The public number. Cancel reads that field.

C. Name another value.

➡️ A. 10 already finds by span. A number on every locked Node duplicates the map. Chrome can still show locked.

❓ **Q8** - **On natural stop (not cancel), when does Core remove the send credential from the active set?**

A. When Core queues lock-clear. Later posts are Unauthorized. Already-enqueued Changes still apply, then lock-clear.

B. When lock-clear applies.

C. Only on cancel. Natural stop leaves the source active.

➡️ A. C leaves a live sender after the Actor is gone. B lets a late post enqueue until clear applies.

**Answer (Alan, 2026-09-05):** 7A, 8B.

## Round 7

❓ **Q9** - **8B plus 10 (match at Post): a Post after stop can enqueue after lock-clear is already queued, then apply after lock-off.** Accept that?

A. Yes. Keep 8B. That Change applies.

B. No. Use 8A: remove the source when Core queues lock-clear.

C. No. Refuse at apply if lock is already off. Amends 10.

➡️ B. A Change after lock-off is a finish hole. C reopens cancel-after-enqueue.

❓ **Q10** - **Do exception and cancel-then-stop queue the same lock-clear as a normal return?**

A. Yes. Stop means the Actor Async/Task ended. Same queue lock-clear. No job status.

B. No. Name the difference.

➡️ A. Q1 discarded status. Cancel already removed the source (10). Lock-clear still needed to write the Node off and drop the number.

**Answer (Alan, 2026-09-05):** Too picky. The reason to queue Clear is that the Actor stays registered until all Actor-sent Changes have been processed. Then the Actor is removed from the registry. Regardless of why or how it stopped, put a delete-actor message on the queue.

## Locked contract

No completed/aborted job result. Callers do not get a job Error. `postChange` `Ok` / `Error` (accept, dedup, Reject) is the Changes result to the Actor. Accepted, deduplicated, and Rejected batches do not change a job terminal. They apply (or Reject) in mailbox order while the Actor is still registered. The Command caller sees Graph and History through Poll. Query by the public number works until delete-actor applies.

Launch writes lock-present on each Node in the span immediately. Not History. Clients see it through state, Fetch, or Query. Cancel still finds the job by span. The public number stays the query key.

When the Actor Async/Task has stopped for any reason, Core enqueues a Core-only delete-actor message (not a Change, not the Actor sender id). FIFO processes earlier Actor-sent Changes first. When delete-actor applies, Core removes the registry entry, drops the number, writes lock off the Nodes, and removes the send credential. 09 "after the task ends" means after delete-actor applies.

Shutdown stays [[../issues/12-define-actor-pool-shutdown-behavior.md]].

## Round 8

❓ **Q11** - **Lock this contract, or name what stays open?**

A. Lock.

B. Lock, and name a may-change.

C. Not yet (name what stays open).

➡️ A. The ticket question is answered. Delete-actor is the finish transition.

**Answer (Alan, 2026-09-05):** A. Lock.
