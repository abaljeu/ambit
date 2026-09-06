# Grill issue 10 facts — Actor cancel and output admission

Date: 2026-09-05

Purpose: Facts only for grilling [[../issues/10-define-actor-cancellation-and-output-admission.md]]. No decisions.

Related: [[../issues/01-generalized-server-actor-produce-path.md]], [[../issues/09-define-core-command-launch-contract.md]], [[../issues/02-core-actor-pool.md]], [[asynchronous-core-task-manager-facts.md]], [[../../event-sourced-ops/details/actors-and-jobs.md]], [[../../event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]], [[../../event-sourced-ops/details/soft-lock.md]].

## 1. How `CoreChanges.postChange` works today

Contract in [[src/Server/Core/CoreChanges.fs]]:

- `CoreChangesAccepted` holds `revision`, `changes`, `externalChanges`, `message`, `isReady`.
- `CoreChanges.postChange: Change list -> Async<Result<CoreChangesAccepted, string>>`.
- Same shape for `postGraphOnlyChange`.

Entry points:

- [[src/Server/Core/CoreRuntime.fs]] `getHandle` returns a `CoreChanges` from File and/or Db agents (`FileAgent.coreChanges`, `DbAgent.coreChanges`, `ofFileWithDbMirror`, or `readOnly`).
- [[src/Server/FileAgent.fs]] / [[src/Server/DbAgent.fs]] `postChange` posts `PostChange(changes, reply)` via `MailboxProcessor.PostAndAsyncReply`.
- HTTP Adapter: [[src/Server/Api.fs]] `Api.postChange` decodes a Change batch, then calls `handle.postChange`.

Checks today: empty list → `Error "changes must not be empty"`; then `applyBatch` (amend + persist + log). No job identity, no send credential, no `CancellationToken` on the contract or in agent `postChange`.

`CoreRuntime.readOnly` replaces both write functions with a fixed `Error` string (DB unavailable). That refuse is before Graph apply.

## 2. The apply mailbox

Both [[src/Server/FileAgent.fs]] and [[src/Server/DbAgent.fs]] own a `MailboxProcessor<FileAgentMsg>`. That queue is the apply mailbox named in issue 01 / actors-and-jobs.

`FileAgentMsg` includes `PostChange` and `PostGraphOnlyChange` (plus reads / `SnapshotDone`).

Enter vs apply:

- **Enter:** caller starts `PostAndAsyncReply`; the message sits in the mailbox until `Receive`.
- **Apply:** `dispatch` runs `handlePostChange` → `applyBatch` (and persist). Reply completes only after that work (or an Error reply).

No cancel of a queued `PostChange`. No reject-after-enqueue path. Timeout is `runBounded` on persist/apply work inside the handler; it does not dequeue or drop a waiting message. Abandoned timeout Tasks may still write later ([[src/Server/FileAgent.fs]] comment on `runBounded`).

## 3. CancellationToken / cancel / job registry in Server Core or tests

[[src/Server/Core/]]: no `CancellationToken`, cancel API, or job registry.

Actors / agents: no job map or cancel token. `CancellationToken` appears elsewhere (e.g. HTTP response log), not on Core Changes.

[[tests/Server.Tests/CoreChangesTests.fs]]: Actor produce path uses `Async.StartAsTask` and full `CoreChanges`; no cancel, token, credential, or registry.

[[asynchronous-core-task-manager-facts.md]] §5 states the same inventory: no `CancellationToken`, job registry, or Command launch in Server code today.

## 4. Issue 01 vs 09 — what the test Actor receives today

| Source | Actor initial state |
| --- | --- |
| Issue 01 (done) | Subgraph (`Graph`) + full `CoreChanges` handle; produce via Normal `postChange` |
| Issue 09 (resolved grill) | Extracted subgraph + send credential only; public job number not given to Actor; Core retains number→Actor, credential, span, Revision, registered name |
| Test today | Matches issue 01 |

[[tests/Server.Tests/CoreChangesTests.fs]]: `runActor subgraph handle act` and `produceFromSubgraph` call `handle.postChange`. Comment: "Local Graph plus full CoreChanges". No send credential type or value exists in code.

## 5. Plan language on cancel, further output, not undoing merged Changes

[[../../event-sourced-ops/details/actors-and-jobs.md]]:

- Cancel cancels the job token; job must not send an apply message once cancelled.
- If the apply message is already in the queue, it runs — **no cancel-after-enqueue**.
- Assessment: none of that product exists yet (no multi-job launcher, identity, or cancel interface).
- Once a Change **arrives** at Server apply, existing merge rules apply; launch/identity/cancel before arrival are undecided.

[[../../event-sourced-ops/details/soft-lock.md]] (accepted): Cancel stops further Changes; already-merged Changes stay; Cancel is not Undo.

[[../../event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]]: Core owns launch, identity, cancellation, finish through Changes; Browser indicator can inspect/cancel via Core pool; this issue does not implement that machinery.

[[asynchronous-core-task-manager-facts.md]]: Cancel stops further Actor output, not Undo; cancel-after-enqueue is issue 10 open, with assessment recommending no cancel-after-enqueue; Changes = output admission seam.

[[../issues/02-core-actor-pool.md]]: Cancel prevents further Actor output without undoing Changes that already merged; design must specify cancellation and cancel-after-enqueue before implementation.

## 6. `CoreChangesAccepted` vs Reject / failed post

Typed seam: `Ok CoreChangesAccepted` vs `Error string`. Success fields are revision, confirmed Changes, `externalChanges`, optional `message`, `isReady`.

HTTP mapping ([[src/Server/Api.fs]]): `Ok` → Change success JSON; `Error` → `agentErrorResult` (typically HTTP 400 `{ error }`, or 500 for internal-server-error strings). Malformed body never calls `postChange`.

Graph / apply "Reject" in ESO language is apply refusal (auth, malformed Ops, hard CAS, Unchanged submission, validation). Those surface as `Error` from `applyBatch` / validation after the mailbox message is already being handled.

Admission can refuse without Graph apply today:

- Empty Change list → `Error` before `applyBatch`.
- `CoreRuntime.readOnly` → fixed `Error` with no agent apply.
- Invalid JSON at HTTP → BadRequest without `postChange`.

There is no job-credential or cancel-admission refuse yet. A future admission gate could return `Error` without calling apply; that would not be a Graph Op Reject, but it would still be an Error result (and HTTP Reject if via Adapter).
