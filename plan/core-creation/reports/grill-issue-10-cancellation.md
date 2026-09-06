# Grill issue 10 — Actor cancellation and output admission

Date: 2026-09-05

Session: closed. Q1–Q16 recorded. Alan locked the contract. [[../issues/10-define-actor-cancellation-and-output-admission.md]] is Status resolved. Named may-change: span lookup vs a cheaper Node field. Adapter 401 and Core Unauthorized are one auth refuse (2026-09-06). Facts: [[grill-issue-10-facts.md]].

## What I read

Project files: [[plan/core-creation/project.md]], [[plan/core-creation/map.md]], issues 01–03, 09–12. Glossary: [[CONTEXT.md]]. Assessment: [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/event-sourced-ops/details/soft-lock.md]]. Facts: [[grill-issue-10-facts.md]]. Issue 03 and 09 are Status resolved. Issue 01 is done. Issue 02 stays needs-info.

## Weakest assumption

The ticket assumes Core can stop later Actor output and can admit or refuse each attempt. [[plan/event-sourced-ops/details/actors-and-jobs.md]] only says cancel the token and the job must not send. Issue 01 still gives the Actor a full CoreChanges handle. Issue 09 gave a send credential. If cancel is only cooperative, admission is not Core's, and the mailbox race this ticket names has no Core rule.

## Design tree

Locked. See ## Locked contract. Q14 is not Actor-only: Browser must present an active source after page open. That source is the existing login cookie at the HTTP Adapter (already checked on every `/ambit/changes` and `/ambit/poll`). Actor sender id is the 09 send credential at Core. Q15 at lock named two refuse words; later they merge into one auth family. Q16=Lock.

Glossary **agent** vs Actor still unanswered. The contract says Actor.

Finish stays [[../issues/11-define-actor-finish-and-failure-behavior.md]]. Shutdown stays [[../issues/12-define-actor-pool-shutdown-behavior.md]].

## Round 1

❓ **Q1** - **Cancel the Actor, refuse its posts, or both?**

A. Core refuses later output. The running Actor is not signaled.

B. Core signals the Actor. The Actor must not post. Core does not refuse a post that still arrives.

C. Both: Core signals the Actor and refuses later output attempts.

➡️ C. A leaves work running. B makes admission the Actor's promise, which this ticket exists to replace. Finish after the signal is [[../issues/11-define-actor-finish-and-failure-behavior.md]].

**Answer (Alan, 2026-09-05):** C. Signal and refuse. He flagged a possible amendment to prior decisions.

❓ **Q2** - **What request cancels?**

A. Core Command cancel by the public number from launch.

B. A different Core API surface (Query, Changes, or a fifth call).

C. Only internal (shutdown, finish). No caller cancel in this ticket.

➡️ A. Same number the caller already holds. Shutdown stays in [[../issues/12-define-actor-pool-shutdown-behavior.md]].

**Answer (Alan, 2026-09-05):** A. Command cancel by the public number. Same amendment flag. Intended UI: user clicks Run Command; message goes to the Server; Server approves; selected Nodes become soft-locked to "the agent"; picking a soft-locked Node can request cancel. Cancel is not Undo ([[../../event-sourced-ops/details/soft-lock.md]]).

**Amended by Q5=A:** cancel Command takes NodeId. The public number stays the 09 query key, not the cancel argument. 2A is not locked.

## Round 2

Recorded **1C** (signal the Actor and refuse later posts) and **2A** (Command cancel by the public launch number). You flagged a possible amendment. You said "agent": glossary **Agent** is the LLM-empowered worker; **Actor** is the Core pool work unit. Which — Actor, Agent, or both (Actor job now; Agent as a later registered name)?

❓ **Q3** - **Who creates the advisory soft-lock?** After Run Command, the Server approves, then selected Nodes become soft-locked. Issue 09: Core retains number→Actor, credential, span, Revision, name. Nothing else. Map: lock policy and Browser UI belong to ESO. ESO 09: Core owns launch, identity, cancel, and finish; ESO owns lock meaning and chrome.

A. Core creates the lock inside Command launch and retains lock facts (amends 09 and the map).

B. Core launch returns the number and retains the span. ESO/Browser attaches the lock after approve, using that number/span. "Server approves" means Command launch succeeded, not that Core owns lock policy.

➡️ B. Soft-lock meaning stays ESO. No 09 retained-facts amendment.

**Answer (Alan, 2026-09-05):** A. Core creates the lock inside Command launch and retains lock facts. This amends 09 "nothing else" and the map's ESO ownership.

❓ **Q4** - **How does picking a locked Node cancel, while Q2=A stays Command-by-number?**

A. Browser maps the lock to the job number and calls Command cancel(number). Cancel input stays the number.

B. Command cancel takes a NodeId. Core finds the job from the retained span.

➡️ A. Chrome (ESO) does the lookup. No 09 amendment for cancel input.

**Answer (Alan, 2026-09-05):** B. Command cancel takes a NodeId. Core finds the job. He said "either nodeid suffices, or node has an active actor field" — not locked. This conflicts with Q2=A.

## Round 3

Recorded **3A** (Core creates the lock at launch; amends 09 "nothing else" and the map's ESO ownership) and **4B** (Command cancel takes a NodeId). You said "either nodeid suffices, or node has an active actor field"; you still have not said whether "agent" means Actor, Agent, or both (Actor job now; Agent as a later registered name).

❓ **Q5** - **Does 4B replace locked 2A?** 2A made cancel Command take the public number.

A. Replace 2A. Cancel takes NodeId. The public number stays the 09 query key, not the cancel argument.

B. Both work. Cancel accepts a number or a NodeId.

➡️ A. Amend 2A. Do not keep both silently.

**Answer (Alan, 2026-09-05):** A. Replace 2A. Cancel takes NodeId. The public number stays the 09 query key.

❓ **Q6** - **How does Core find the job from that NodeId?** "Either" is not a lock.

A. NodeId is in the retained span. No field on the Node.

B. A field on the Node, written as a Graph Change at launch.

C. A field on the Node that is Core-only runtime, not a Change.

➡️ A. Core already keeps the span. A Graph field is ESO chrome unless you want Core to write Graph at launch.

**Answer (Alan, 2026-09-05):** A. NodeId is in the retained span. No field on the Node. He said "only because/if it's easiest". Locked as span membership now. Named may-change: if implementation shows a field is cheaper than a span scan, a Node field may replace this lookup. Not a silent field fallback.

❓ **Q7** - **What lock facts does Core retain beyond 09?** 09 keeps number→Actor, credential, span, Revision, name. Q3=A adds lock facts. Name them.

➡️ A lock-present flag on that job. The span already names the reserved Nodes.

**Answer (Alan, 2026-09-05):** OK. Core retains 09's list plus a lock-present flag on that job.

## Round 4

Recorded **5A** (cancel takes NodeId; 2A amended — number stays the 09 query key), **6A** (span membership now; named may-change if a field is cheaper later), and **7** (lock-present flag plus 09's list). Still unanswered: "agent" — Actor, Agent, or both?

❓ **Q8** - **How does Core signal the Actor?**

A. A CancellationToken on the running Async/Task.

B. A message to the Actor. No token.

C. Both.

➡️ A. Matches [[../../event-sourced-ops/details/actors-and-jobs.md]].

**Answer (Alan, 2026-09-05):** A. CancellationToken on the running Async/Task.

❓ **Q9** - **How does Core refuse a later output attempt?**

A. Core checks the job is not cancelled and the send credential matches, then Posts to the apply mailbox. A cancelled job's postChange returns Error without enqueue. Production send is credential-gated (09). The issue 01 full handle is not the production admission object.

B. Core wraps or disables the handle. No credential check.

C. The Actor keeps the issue 01 full CoreChanges handle. Admission is the Actor's promise.

➡️ A. Q1=C requires Core refuse. 09 already gave a send credential.

**Answer (Alan, 2026-09-05):** Not A/B/C. A mailbox message has a sender id that must match an active source. That is the admission rule. Not relabeled as 9A.

❓ **Q10** - **If two jobs' spans contain the same NodeId, which job does cancel(NodeId) hit?**

A. Cancel every job whose retained span contains that NodeId.

B. Cancel one (last lock / last launch).

C. Refuse cancel when more than one matches.

D. Launch forbids overlapping locks, so at most one job matches.

➡️ A. 4B/6A has no single job without uniqueness. Pick D if launch must forbid overlap.

**Answer (Alan, 2026-09-05):** D. Launch forbids overlapping locks, so at most one job matches cancel(NodeId). This amends issue 09, which did not forbid overlap.

## Round 5

Recorded **8A** (CancellationToken on the running Async/Task), **9** in your words (a mailbox message has a sender id that must match an active source), and **10D** (launch forbids overlapping locks — amends 09). Still unanswered: "agent" — Actor, Agent, or both?

❓ **Q11** - **Is sender id the issue 09 send credential?**

A. Yes. One id. The 09 send credential is the sender id.

B. No. Sender id is a new id, separate from the send credential.

➡️ A. Do not keep two ids.

**Answer (Alan, 2026-09-05):** A. Sender id is the issue 09 send credential. One id.

❓ **Q12** - **When must sender id match an active source?** Cancel removes that source from the active set.

A. At Post. If the source is not active, Error and do not enqueue. A message already in the mailbox applies.

B. At apply (Receive). A message that entered may still be refused.

➡️ A. No cancel-after-enqueue. Matches actors-and-jobs. A batch that entered already passed admission.

**Answer (Alan, 2026-09-05):** A. Match at Post. Not active → Error, do not enqueue. Already in the mailbox → apply. No cancel-after-enqueue.

❓ **Q13** - **What counts as overlap for 10D?**

A. Any shared NodeId in two jobs' retained spans.

B. Only some other overlap (name it).

➡️ A. Two locks that share a NodeId are forbidden at launch.

**Answer (Alan, 2026-09-05):** A. Overlap is any shared NodeId in two jobs' retained spans. Forbidden at launch.

## Locked contract

Cancel Command takes a NodeId. The public number stays the 09 query key. Core finds the job by NodeId in the retained span. Named may-change: a Node field if a span scan is not cheapest. Launch forbids overlapping locks: any shared NodeId. This amends 09. Core creates the advisory lock at launch. Core retains 09's list. Lock-present is on the Node, not a job flag ([[../issues/11-define-actor-finish-and-failure-behavior.md]]). Cancel is not Undo.

Core signals the Actor with a CancellationToken and refuses later output. A mailbox message has a sender id that must match an active source. For an Actor, sender id is the 09 send credential. Match at Post: not active → Unauthorized, do not enqueue. Already in the mailbox → apply. Cancel removes the source from the active set.

Browser PostChange must be admitted after page open. The Browser active source is the existing `gambol_auth` cookie. The HTTP Adapter already checks it on every `/ambit/changes` and `/ambit/poll` (and state, load); fail is HTTP 401 before Core. Two kinds of source: session cookie (Adapter) and job credential (Core). This ticket does not rewrite login.

Auth refuse is one family: Adapter cookie fail (HTTP 401) and Core inactive-sender (Unauthorized) are the same refuse — one word and one path. Named may-change about mapping Core Unauthorized to HTTP 401 is now: they merge. Actor admission fail is that refuse without enqueue. TCP / Database-unavailable / readOnly Reject is a system error, not this refuse ([[../issues/12-define-actor-pool-shutdown-behavior.md]]).

## Round 6

Recorded **11A** (sender id is the 09 send credential), **12A** (match at Post; already in the mailbox applies — no cancel-after-enqueue), and **13A** (overlap is any shared NodeId). Still unanswered: "agent" — Actor, Agent, or both?

❓ **Q14** - **Who must present a sender id?**

A. Actor produce messages only. Browser HTTP PostChange stays unchanged in this ticket.

B. Every PostChange, including Browser HTTP.

➡️ A. Today Browser posts have no sender id. This ticket is Actor admission.

**Answer (Alan, 2026-09-05):** Not A. Browser PostChange must be admitted after page open. He asked whether a login cookie exists or is only for opening the page. He said that should be fixed. Do not lock Actor-only sender id.

❓ **Q15** - **What is the refuse shape?**

A. Existing Error string. Same as empty list and readOnly. No new result case.

B. A distinct admission case.

➡️ A. Admission already returns Error without apply.

**Answer (Alan, 2026-09-05):** Unauthorized. Locked with cookie facts: Browser cookie fail is HTTP 401 at the Adapter; Actor admission fail is Core Error "Unauthorized" without enqueue.

❓ **Q16** - **Lock this contract, or name a may-change?** The draft is in the grill report. It includes the Q14/Q15 recs (Actor-only sender id; Error string).

A. Lock.

B. Lock, and name a may-change.

C. Not yet (name what stays open).

➡️ A. The ticket question is answered.

**Answer (Alan, 2026-09-05):** Lock. Locked with cookie facts; Q17–Q19 recs taken as the lock (not asked as a new round).

## Cookie facts

Login sets cookie `gambol_auth` ([[src/Server/AuthToken.fs]], [[src/Server/RouteRegistration.fs]]). Page open (`GET /ambit`): cookie match serves the App; else redirect to `/ambit/login`. Empty Auth user and password disables the check.

Every `/ambit/changes`, `/ambit/poll`, `/ambit/state`, and `/ambit/load` already calls `auth.IsAuthenticated`. Fail is `Results.Unauthorized()` (HTTP 401) before [[src/Server/Api.fs]] `postChange`. The cookie does not enter Core. `Api.postChange` and the apply mailbox carry no sender id. Core `Error string` maps through `agentErrorResult` to HTTP 400 `{ error }` (or 500). That path never becomes 401.

## Round 7 (not asked)

Q16=Lock arrived before a new round. Cookie facts closed the Browser-source and Unauthorized forks without Q17–Q19. Recs taken: Browser source is the existing cookie at the Adapter; Actor source is the 09 credential; Unauthorized is HTTP 401 for cookie fail and Core Error "Unauthorized" for Actor admission fail.

Later (2026-09-06): those two refuse words merge into one auth family. System error stays on 12. Lock-present is on the Node (11), not a job flag.
