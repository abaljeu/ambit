# Define Actor finish and failure behavior

**Type:** grilling
**Status:** done
Blocked by: 03, 09, 10
Actual: 1h

## Question

How does Core transition and retain job state when an Actor finishes successfully or fails, what result or error is available to callers, and how do accepted, deduplicated, or rejected final Changes affect that terminal state?

## Answer

There is no completed or aborted job result. Callers do not get a job Error. `postChange` `Ok` / `Error` (accept, dedup, Reject) is the Changes result to the Actor. Accepted, deduplicated, and Rejected batches do not change a job terminal. They apply or Reject in mailbox order while the Actor is still registered. The Command caller sees Graph and History through Poll. Query by the public number works until delete-actor applies.

Launch writes lock-present on each Node in the span immediately. Lock status is not History. Clients see it through state, Fetch, or Query. Cancel still finds the job by span. The public number stays the query key. This amends [[10-define-actor-cancellation-and-output-admission.md]] (lock-present flag on the job) and [[09-define-core-command-launch-contract.md]] ("after the task ends").

When the Actor Async/Task has stopped for any reason, Core enqueues a Core-only delete-actor message. It is not a Change and does not use the Actor sender id. FIFO processes earlier Actor-sent Changes first. When delete-actor applies, Core removes the registry entry, drops the number, writes lock off the Nodes, and removes the send credential.

Amend (2026-09-11): Exactly one Core mailbox (the Changes apply queue). The runner is a TaskPool, not a mailbox. Registry is mailbox state. There is no credential MailboxProcessor; admit and enqueue are one message. Drop is registry remove plus async terminate if the task is still running. Failed stop enqueues delete-actor the same as a normal stop. See [[../reports/actor-pool-rewind-review.md]] and [[18-finish-and-drop.md]].

Shutdown stays [[12-define-actor-pool-shutdown-behavior.md]].

Grill notes: [[plan/core-creation/reports/grill-issue-11-finish.md]].

## Comments

- Grilling started. Round 1 is in [[plan/core-creation/reports/grill-issue-11-finish.md]]. Blockers 03, 09, and 10 are resolved, so this ticket is unblocked. Facts: [[plan/core-creation/reports/grill-issue-11-facts.md]].
- Q1: Discarded. No completed/aborted job result. See the grill report.
- Q2: When the Actor has stopped, queue a lock-clear request on the mailbox. See the grill report.
- Q3: A. Core-only lock-clear. Lock status is on the Node, communicated to clients. Amends 10 toward a Node field. See the grill report.
- Q4: A. History never contains lock status. See the grill report.
- Q5: B. Number stays until lock-clear applies. Amends 09 "after the task ends". See the grill report.
- Q6: A. Launch writes lock on the Node immediately. See the grill report.
- Q7: A. Node carries lock-present only. Cancel still uses span. See the grill report.
- Q8: B. Source stays active until delete-actor applies. See the grill report.
- Q9/Q10: Withdrawn as too picky. On any stop, enqueue delete-actor. Registry lasts until that message is processed. See the grill report.
- Q11: Lock. Status resolved. See the grill report.

## Time

- 2026-09-05 15m — started grill; first question is whether finish keeps a job result or only drops the 09 number (from chat)
- 2026-09-05 5m — recorded Q1 discarded: no completed/aborted job result (from chat)
- 2026-09-05 5m — recorded Q2: queue lock-clear on the mailbox after Actor stop (from chat)
- 2026-09-05 5m — recorded Q3=A plus Node lock status / History fork (from chat)
- 2026-09-05 5m — recorded Q4=A History never holds lock status (from chat)
- 2026-09-05 5m — recorded Q5=B and Q6=A (from chat)
- 2026-09-05 5m — recorded Q7=A and Q8=B (from chat)
- 2026-09-05 5m — recorded delete-actor rule; withdrew Q9/Q10 (from chat)
- 2026-09-05 5m — locked contract; Q11=A; resolved (from chat)
- 2026-09-11 5m — amend Answer with mailbox/TaskPool/drop shape (from chat)
