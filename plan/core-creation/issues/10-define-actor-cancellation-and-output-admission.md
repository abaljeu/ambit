# Define Actor cancellation and output admission

**Type:** grilling
**Status:** done
Blocked by: 03, 09
Actual: 1h25m

## Question

At what exact point does cancellation stop later Actor output, how does Core admit or refuse each output attempt through Core Changes, and what happens when cancellation arrives after a Change or batch has entered the apply mailbox but before it has applied?

## Answer

The controlling contract is [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Cancel implementation is [[17-cancel-a-job.md]]. Admission is [[14-server-tracks-credentials.md]].

Grill notes: [[plan/core-creation/reports/grill-issue-10-cancellation.md]]. The earlier span-lock answers below are historical interrogation notes and no longer control implementation.

**Amend (2026-09-07):** Core mailbox messages clear fast; slow work is an Actor. Cancel is a fast mailbox message. Posts ahead of cancel still apply (FIFO); posts behind fail the normal active-source check after cancel has run — no special cancel reject. See [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]].

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
- 2026-09-11 — [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] superseded span membership, Graph lock-present, and non-event cancellation. Focus-keyed cancellation, credential admission, FIFO ordering, and no Undo remain standing.

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
- 2026-09-11 5m — drop restated cancel and admission; point at 07, 14, and 17 (from chat)
