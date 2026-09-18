# Define the Core Command launch contract

**Type:** grilling
**Status:** done
Blocked by: 03, 05
Actual: 2h15m

## Question

What typed Command input selects and launches an Actor definition off the apply queue, what Core-owned job identity and initial state does launch return, and what job information does Core retain without defining Parse, shell, or Agent behavior?

## Answer

The controlling contract is [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Dispatch is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. Rebuild implementation is [[15-launch-actor-and-hold-span.md]]. This grill no longer owns launch membership or identity.

Grill notes: [[plan/core-creation/reports/grill-issue-09-launch-contract.md]]. The earlier span answers below are historical interrogation notes and no longer control implementation.

## Comments

- Grilling started. Round 1 is in [[plan/core-creation/reports/grill-issue-09-launch-contract.md]]. Blockers 03 and 05 are resolved, so this ticket is unblocked.
- Q1: A. Registered name plus arguments. Arguments are Revision and a range like SiteNodeRange (parent, start, endd) with parent as NodeId, not SiteEntry. See the grill report.
- Q2: Core extracts the subgraph defined by start and endd and passes it to the named Actor. DAG occurrence of parent NodeId is still unresolved (parent press). See the grill report.
- Q3: Extraction takes the parent Node (NodeId), then child occurrences. SiteNodeRange indexes children under a parent site-map occurrence; Alan indexes the Node's children (parent press). See the grill report.
- Q4: Site-map children under a parent occurrence and the Node's children never differ; start/endd always mean the same list. What enforces that invariant is still unresolved (parent press). See the grill report.
- Q5: The two child lists never differ as a universal view-edits-model law, not a Command-only restriction. Whether (Revision, parent NodeId, start, endd) is every launch argument or only subgraph Actors is still open (parent move). See the grill report.
- Q6: (Revision, parent NodeId, start, endd) is the argument of every Core Command launch. The no-selection / whole-revision case is still unresolved (parent press). See the grill report.
- Q7: Launch without parent/start/endd is forbidden. Whether a caret (start == endd) is forbidden or a valid empty span is still unresolved (parent press). See the grill report.
- Q8: Caret (start == endd) is the same as no selection: forbidden. Launch requires a non-empty span. Core-owned job identity that launch returns is still open (parent move). See the grill report.
- Q9: Launch returns a pool slot if that slot is stable. An internal id is not returned. What stable means (recycled slots aliasing jobs) is still unresolved (parent press). See the grill report.
- Q10: A pool slot identifies the task only while it runs; it is not a durable job id. Post-completion query (fail vs silent hit on a reused slot) is still unresolved (parent press). See the grill report.
- Q11: After the task ends, a query on that number must fail, not silently hit a new task. Whether an old caller's number still fails after slot reuse (generation vs fail-only-while-empty) is still unresolved (parent press). See the grill report.
- Q12: Store a map number to Actor. Do not reuse numbers. Stale queries fail because the number is gone. Why an internal unreturned id still exists is still unresolved (parent press). See the grill report.
- Q13: Internal unreturned id is a credential so the running Actor can send messages. Public number is the caller query key. Who receives that id at launch (Actor initial state, never the Command caller) is still open (parent pin). See the grill report.
- Q14: Launch puts the send credential only in the Actor's initial state, never in the Command caller's return value. What else Core retains on the running job is still unresolved (parent press). See the grill report.
- Q15: Core retains number→Actor, send credential, span, Revision, and registered name. Nothing else. Whether Actor initial state is only subgraph plus credential is still unresolved (parent press). See the grill report.
- Q16: Actor initial state is subgraph plus send credential only; Core keeps span, Revision, and registered name. See the grill report.
- Q17: What might change is sending more subgraph. Lock. Status resolved.
- 2026-09-11 — [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] superseded the span-shaped request, Browser-selected registered name, transient-only public number, and non-event launch assumptions.
- 2026-09-11 — Dispatch is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]].

## Time

- 2026-09-05 25m — started grill; first question is how Command names work Core does not own (from chat)
- 2026-09-05 10m — recorded Q1 (from chat)
- 2026-09-05 5m — recorded Q2 (from chat)
- 2026-09-05 10m — recorded Q3 and Alan's parent-vs-occurrence clarification (from chat)
- 2026-09-05 5m — recorded Q4 (from chat)
- 2026-09-05 5m — recorded Q5 (from chat)
- 2026-09-05 5m — recorded Q6 (from chat)
- 2026-09-05 5m — recorded Q7 (from chat)
- 2026-09-05 5m — recorded Q8 (from chat)
- 2026-09-05 5m — recorded Q9 (from chat)
- 2026-09-05 5m — recorded Q10 (from chat)
- 2026-09-05 5m — recorded Q11 (from chat)
- 2026-09-05 5m — recorded Q12 (from chat)
- 2026-09-05 5m — recorded Q13 (from chat)
- 2026-09-05 5m — recorded Q14 (from chat)
- 2026-09-05 5m — recorded Q15 (from chat)
- 2026-09-05 5m — recorded Q16 (from chat)
- 2026-09-05 10m — locked contract; Q17 more-subgraph may-change; resolved (from chat)
- 2026-09-06 5m — Answer: query fails after delete-actor applies, not when the Actor Task returns (from chat)
- 2026-09-11 5m — record Command-text Actor dispatch (from chat)
- 2026-09-11 5m — drop restated launch contract; point at 07 and 15 (from chat)
