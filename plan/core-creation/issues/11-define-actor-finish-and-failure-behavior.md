# Define Actor finish and failure behavior

**Type:** grilling
**Status:** done
Blocked by: 03, 09, 10
Actual: 1h

## Question

How does Core transition and retain job state when an Actor finishes successfully or fails, what result or error is available to callers, and how do accepted, deduplicated, or rejected final Changes affect that terminal state?

## Answer

The controlling contract is [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Succeeded, Failed, and Cancelled are Core mailbox terminal messages. The first terminal message durably appends ActorFinished to the one global Event sequence. Failed stores a safe domain error; raw provider details stay in logs. Cancelled has no Error or Change. A duplicate or late terminal message is ignored.

Core accepts Changes while the Actor remains registered. Terminal processing runs after earlier queued Changes, derives truthful success from their outcomes, appends ActorFinished, synchronously removes the live registry and secret, then requests asynchronous termination only if the task is still running. Core never waits. Earlier accepted output remains even if later output rejects.

The Browser observes Graph and lifecycle Events through universal Core responses and Poll. Public Actor identity remains durable after drop. ActorStarted and ActorFinished replace the non-History lock-present overlay; no Graph lock field is written or cleared.

On restart, every unmatched ActorStarted receives ActorFinished Interrupted. The live secret is not recovered and work does not resume.

Shutdown stays [[12-define-actor-pool-shutdown-behavior.md]].

Grill notes: [[plan/core-creation/reports/grill-issue-11-finish.md]]. The earlier no-terminal and Graph-lock answers below are historical interrogation notes and no longer control implementation.

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
- 2026-09-11 — [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] superseded no-result completion, lock-present outside History, and delete-only finish. Durable ActorFinished is the terminal result.

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
