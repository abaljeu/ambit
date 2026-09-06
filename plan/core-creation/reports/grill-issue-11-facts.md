# Grill issue 11 facts — Actor finish and failure behavior

Date: 2026-09-05

Purpose: Facts only for grilling [[../issues/11-define-actor-finish-and-failure-behavior.md]]. No policy.

Related: [[../issues/01-generalized-server-actor-produce-path.md]], [[../issues/09-define-core-command-launch-contract.md]], [[../issues/10-define-actor-cancellation-and-output-admission.md]], [[../issues/12-define-actor-pool-shutdown-behavior.md]], [[../issues/02-core-actor-pool.md]], [[implement-issue-01-actor-produce-path.md]], [[grill-issue-10-facts.md]], [[../../event-sourced-ops/details/actors-and-jobs.md]], [[../../event-sourced-ops/details/soft-lock.md]], [[../../event-sourced-ops/details/completing-ops.md]], [[../../event-sourced-ops/architecture.md]], [[../../event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]].

## Issue 11 scope (from the ticket)

Type: grilling. Status: open. Blocked by: 03, 09, 10.

Question: "How does Core transition and retain job state when an Actor finishes successfully or fails, what result or error is available to callers, and how do accepted, deduplicated, or rejected final Changes affect that terminal state?"

Map topology ([[issue-02-ready-from-chat.md]]): Command (09) gates cancel (10) gates finish (11) gates shutdown (12) gates issue 02.

## 1. Plan language — finish / success / fail / complete / terminal

### plan/event-sourced-ops/

[[../../event-sourced-ops/details/actors-and-jobs.md]] (assessment, not a lock):

- Stage **"2. Finish and apply."**: "The job builds Change objects and then sends a **message** into the apply queue… The finishing task does not return to the original request, which is long since complete. The requesting Browser **polls**; there is no completion push."
- Stage **"3. Cancel."**: Cancelling stops further apply messages; "If the apply message is already in the queue, it runs — there is no cancel-after-enqueue."
- **Produce**: "Recoverable overlap is merge success. Auth and malformed requests stay Reject."
- **Complete** (Server fill-in, not job lifecycle): "If the job's Local Subgraph cannot name a promote-to-Owned Op, the Server completes **that** Change" ([[../../event-sourced-ops/details/completing-ops.md]]).
- "None of this exists as a product. There is no multi-job launcher, no job identity, and no cancel interface."

[[../../event-sourced-ops/architecture.md]]:

- **Merge success**: stale base/field/list "is not a failure. The Server amends and answers with success."
- **Reject**: "Only request failures remain: authentication, a malformed request, and the like. Concurrency is not a Reject."
- Long-running Actors: "When it concludes, its Change is simply the newest Change… Other Browsers learn of it by polling; there is no completion push."

[[../../event-sourced-ops/details/soft-lock.md]] (accepted meaning): "When the job completes there may be Changes to those Nodes." Lifecycle direction: "Job completion clears the lock."

[[../../event-sourced-ops/details/completing-ops.md]]: **Complete** means Server adds missing Ops inside one Change; "A later fill-in, arriving as a second Change on a poll, is **rejected**."

[[../../event-sourced-ops/details/vocabulary.md]]: **Actor** = "Anything that produces a Change, synchronous or asynchronous." Not yet in [[CONTEXT.md]].

### plan/core-creation/

[[../issues/02-core-actor-pool.md]]: "finish work through Core Changes and inner apply"; checklist includes "A finishing Actor submits Change objects through Core Changes and inner apply."

[[../issues/09-define-core-command-launch-contract.md]] (resolved): Launch returns a public number; "After the task ends, the query fails because the number is gone." Does not define finish or failure semantics.

[[../issues/10-define-actor-cancellation-and-output-admission.md]] (resolved): Covers cancel and Post admission; not terminal job state.

[[../issues/12-define-actor-pool-shutdown-behavior.md]] (open): shutdown cancel/await, enqueued batches, terminal info before exit — not finish semantics.

[[issue-02-ready-from-chat.md]]: Ticket 11 asks terminal job state and effect of accepted, deduplicated, or rejected final Changes.

[[implement-issue-01-actor-produce-path.md]]: finish and shutdown not added in issue 01.

### doc/Decisions/

No matches for finish, fail, complete, terminal, Actor, or job in [[doc/Decisions/]] (grep 2026-09-05).

### CONTEXT.md

**Core**: "manages the Actor pool." **Core API**: "Advanced logic and Actor definitions work to this Interface." **Agent**: "An LLM-empowered worker. Ambit will have one." _Avoid_: "Actor (for this counterpart)." No **Actor** glossary entry. **Poll**: "used in Sync… not a synonym for completion push" (implied by ESO; CONTEXT does not say "finish").

## 2. Issue 01 produce path — completion and exception today

Locked protocol ([[../issues/01-generalized-server-actor-produce-path.md]]): Actor runs off apply mailbox; posts via Normal `CoreChanges.postChange`; "Core answers `postChange` with `CoreChangesAccepted` or an Error string… There is no completion push." "Awaiting the acknowledgement is optional for one-shot Actors."

Implementation ([[implement-issue-01-actor-produce-path.md]], [[tests/Server.Tests/CoreChangesTests.fs]]): `runActor` uses `Async.StartAsTask`; `produceFromSubgraph` `return! handle.postChange`. Test awaits and `requireOk` on `Result` — no finish hook or exception-to-job mapping. On success: asserts acknowledgement and Poll parity. Issue 01: exit before pool runs can drop the post; fire-and-forget hides Error.

## 3. Job registry, result store, query-by-number in code

No matches in [[src/Server/Core/]] for job registry, result store, Command launch, pool, or query-by-number (grep 2026-09-05).

[[grill-issue-10-facts.md]] §3: "[[src/Server/Core/]]: no `CancellationToken`, cancel API, or job registry."

[[asynchronous-core-task-manager-facts.md]] §5 (same inventory): no job map in Server code today.

[[../issues/09-define-core-command-launch-contract.md]] describes planned "map from that number to the Actor" and post-end query fail — not implemented.

Client `commandRegistry` in [[src/Client/Commands.fs]] is Browser UI commands, not Core Actor jobs.

## 4. Core Changes — Reject vs accept vs changeId dedup (Actor awaits Post)

Contract ([[src/Server/Core/CoreChanges.fs]]): `postChange: Change list -> Async<Result<CoreChangesAccepted, string>>`. Success fields: `revision`, `changes`, `externalChanges`, `message`, `isReady`.

Typed contract ([[../issues/03-define-typed-core-changes-contract.md]]): "A repeated `changeId` returns the stored accepted Change through the normal success path and does not advance Revision. Deduplication has no separate result case." Empty list and no-effect Change are Rejects.

Apply path ([[src/Server/FileAgent.fs]] `applyBatch`):

- **Dedup**: `ChangeLog.tryFindByChangeId` hit → return stored Change in confirmations; state and `changed` flag unchanged; no new log entry when `fresh` is empty.
- **Reject**: `ApplyResult.Invalid` → `Error errMsg`; `ApplyResult.Unchanged` → `Error "Unchanged submission is rejected."`
- **Accept / amend**: `ApplyResult.Changed` → new state, revision +1, `externalChanges || amended`.

`handlePostChange`: empty batch → `Error "changes must not be empty"` before apply; validation/persist/log failures → `Error`; success → `Ok(accepted ackChanges externalChanges persistMessage)`.

Tests ([[tests/Server.Tests/StateEndpointTests.fs]]):

- ``POST same changeId twice is idempotent``: second POST HTTP 200, revision stays 1, same `changes` as first.
- ``POST duplicate changeId with stale revision stays idempotent``: same pattern after intervening Change.

HTTP Adapter ([[src/Server/Api.fs]]): `Ok` → success JSON; `Error` → `agentErrorResult` (400 `{ error }` unless message starts with "Internal server error" → 500). Malformed JSON never calls `postChange`.

An Actor that awaits `postChange` today sees the same `Result` as a typed caller — not a separate Reject enum. ESO "Reject" for Graph apply = `Error string` after the message entered the mailbox. Amendment = `Ok` with `externalChanges = true` when amended ([[plan/core-creation/reports/current-change-contract-facts.md]]).

## 5. Soft-lock release language

[[../../event-sourced-ops/details/soft-lock.md]]:

- Accepted: reservation is advisory; concurrent Browser edits are legal and merge; concluding Change is amended as newest and "returns success."
- Lifecycle direction (accepted): "The reservation **belongs to a job**. **Job completion clears the lock**."
- Still proposed: "Who issues a soft lock, and how it expires."

[[../../event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]] checklist: "The reservation belongs to a Core job identity, and job completion clears it." Does not implement launch or finish machinery.

[[../issues/10-define-actor-cancellation-and-output-admission.md]] (resolved): Core creates lock at launch; cancel is not Undo; does not define finish-driven lock release beyond ESO text above.

No soft-lock implementation in Server Core code ([[implement-issue-01-actor-produce-path.md]]: "soft-lock UI" out of scope).

## 6. Issue 12 shutdown — what NOT to treat as finish (issue 11)

[[../issues/12-define-actor-pool-shutdown-behavior.md]] Question (open, blocked by 09, 10, 11):

"When the Server or Core shuts down, how are running and queued Actors cancelled or awaited, how are already-enqueued Change batches treated, and what terminal job information must remain observable before process exit **without adding process crash isolation**?"

[[asynchronous-core-task-manager-facts.md]] lifecycle table separates **Completion / failure** (issue 11) from **Restart / shutdown** (issue 12): cancel or await running jobs; treat enqueued batches; terminal info before exit.

[[../issues/09-define-core-command-launch-contract.md]] grill notes: "Cancel, finish, and shutdown stay in 10–12."

Finish in plan docs ([[../../event-sourced-ops/details/actors-and-jobs.md]]) is job posting Changes and Poll consumption — not process shutdown, not pool drain, not crash isolation ([[plan/core-creation/reports/kernel-fsproj.md]]: "abort a hung Actor; Graph stays consistent").

## 7. CONTEXT.md — Actor vs Agent

| Term | CONTEXT.md |
| --- | --- |
| **Agent** | Defined: "An LLM-empowered worker. Ambit will have one." _Avoid_: "Actor (for this counterpart)." |
| **Actor** | Not defined as a glossary headword. Mentioned only under **Core** ("Actor pool") and **Core API** ("Actor definitions"). |
| **Agentic** | _Avoid_: "using Agentic for Sync, Upload, or a long-running job." |

ESO [[../../event-sourced-ops/details/vocabulary.md]] defines **Actor** separately and says "Parse and later agents are the same kind as a user-edit Actor." Explicitly: "Do not add these terms to [[CONTEXT.md]] yet."

[[grill-issue-10-cancellation.md]] records Alan's distinction: "glossary **Agent** is the LLM-empowered worker; **Actor** is the Core pool work unit."

## 8. Cross-reference — words that overlap "finish"

| Word | Sense in repo | Owner |
| --- | --- | --- |
| **Finish** (job) | Post concluding Change(s); Poll for others | Issue 11 / [[actors-and-jobs.md]] |
| **Complete** (Change) | Server fill-in missing Ops in same Change | [[completing-ops.md]] |
| **Merge success** | Amend stale Change; answer Ok | [[architecture.md]] |
| **Reject** | Auth, malformed, invalid Op, unchanged, validation | Issue 03, ESO architecture |
| **Shutdown** | Process exit behavior for pool and mailbox | Issue 12 |
| **Agent-done** | Git workflow on dev branch | [[CONTEXT.md]] — unrelated to Actor jobs |
