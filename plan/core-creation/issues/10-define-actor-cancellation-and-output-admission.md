# Define Actor cancellation and output admission

Type: grilling
Status: resolved
Blocked by: 03, 09
Actual: 1h20m

## Question

At what exact point does cancellation stop later Actor output, how does Core admit or refuse each output attempt through Core Changes, and what happens when cancellation arrives after a Change or batch has entered the apply mailbox but before it has applied?

## Answer

Command cancel takes a NodeId. The public number stays the 09 query key. Core finds the job by NodeId membership in the retained span. Named may-change: a Node field if a span scan is not cheapest. Launch forbids overlapping locks: any shared NodeId. This amends [[09-define-core-command-launch-contract.md]]. Core creates the advisory lock at launch. Core retains 09's list. Lock-present is on the Node, not a job flag ([[11-define-actor-finish-and-failure-behavior.md]]). Cancel is not Undo.

Core signals the Actor with a CancellationToken and refuses later output. A mailbox message has a sender id that must match an active source. For an Actor, sender id is the 09 send credential. Match at Post: not active → Unauthorized, do not enqueue. Already in the mailbox → apply. Cancel removes the source from the active set.

Browser PostChange must be admitted after page open. The Browser active source is the existing `gambol_auth` cookie. The HTTP Adapter already checks it on every `/ambit/changes` and `/ambit/poll` (and state, load). Two kinds of source: session cookie (Adapter) and job credential (Core). This ticket does not rewrite login.

Auth refuse is one family: Adapter cookie fail (HTTP 401) and Core inactive-sender (Unauthorized) are the same refuse — one word and one path. Named may-change about mapping Core Unauthorized to HTTP 401 is now: they merge. Actor admission fail is that refuse without enqueue. TCP / Database-unavailable / readOnly Reject is a system error, not this refuse ([[12-define-actor-pool-shutdown-behavior.md]]).

Grill notes: [[plan/core-creation/reports/grill-issue-10-cancellation.md]].

## Comments

- Grilling started. Round 1 is in [[plan/core-creation/reports/grill-issue-10-cancellation.md]]. Blockers 03 and 09 are resolved, so this ticket is unblocked. Facts: [[plan/core-creation/reports/grill-issue-10-facts.md]].
- Q1: C. Core signals the Actor and refuses later output. See the grill report.
- Q2: A amended by Q5=A. Cancel Command takes NodeId. The public number stays the 09 query key, not the cancel argument. See the grill report.
- Q3: A. Core creates the advisory soft-lock at Command launch and retains lock facts. Amends 09 "nothing else" and the map's ESO ownership. See the grill report.
- Q4: B. Command cancel takes a NodeId. See the grill report.
- Q5: A. Replaces 2A. See the grill report.
- Q6: A. Lookup is NodeId membership in the retained span. No Node field. Named may-change: a field may replace span scan if implementation shows it is cheaper. See the grill report.
- Q7: OK. Core retains 09's list plus a lock-present flag on that job. See the grill report.
- Q8: A. CancellationToken on the running Async/Task. See the grill report.
- Q9: A mailbox message has a sender id that must match an active source. Not recorded as 9A. See the grill report.
- Q10: D. Launch forbids overlapping locks. Amends 09. See the grill report.
- Q11: A. Sender id is the 09 send credential. See the grill report.
- Q12: A. Match at Post. Already in the mailbox applies. No cancel-after-enqueue. See the grill report.
- Q13: A. Overlap is any shared NodeId. See the grill report.
- Q14: Not A. Browser must present an active source after page open. That source is the existing login cookie at the Adapter. See the grill report.
- Q15: Unauthorized. Browser cookie fail is HTTP 401; Actor fail is Core Error "Unauthorized". See the grill report.
- Q16: Lock. Status resolved. See the grill report.
- Amend (2026-09-06): merge Adapter 401 and Core Unauthorized into one auth refuse; system error stays on 12. Lock-present is on the Node (11), not a job flag. Status stays resolved.

## Time

- 2026-09-05 15m — started grill; first questions are cancel effect vs admission, and the cancel request (from chat)
- 2026-09-05 10m — recorded Q1=C and Q2=A; Alan flagged a possible amendment and described lock UI (from chat)
- 2026-09-05 10m — recorded Q3=A and Q4=B; pressed 2A vs 4B, lookup, and lock facts (from chat)
- 2026-09-05 10m — recorded Q5=A, Q6=A with may-change, Q7=OK; asked signal, refuse, overlap (from chat)
- 2026-09-05 10m — recorded Q8=A, Q9 sender-id rule, Q10=D; asked credential, match time, overlap (from chat)
- 2026-09-05 10m — recorded Q11=A, Q12=A, Q13=A; asked who presents sender id, refuse shape, lock (from chat)
- 2026-09-05 10m — recorded Q14 cookie source, Q15 Unauthorized, Q16 lock; resolved (from chat)
- 2026-09-06 5m — merge 401 and Unauthorized into one auth refuse; keep system error off this ticket (from chat)
- 2026-09-06 5m — Answer: lock-present is on the Node (11), not a job flag (from chat)
