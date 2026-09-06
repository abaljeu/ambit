# Grill issue 12 facts — Actor-pool shutdown behavior

Date: 2026-09-05

Purpose: Facts only for grilling [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]]. No policy.

Related: [[plan/core-creation/issues/09-define-core-command-launch-contract.md]], [[plan/core-creation/issues/10-define-actor-cancellation-and-output-admission.md]], [[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]], [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]], [[plan/core-creation/project.md]], [[plan/core-creation/map.md]], [[plan/core-creation/reports/grill-issue-09-launch-contract.md]], [[plan/core-creation/reports/grill-issue-10-facts.md]], [[plan/core-creation/reports/grill-issue-10-cancellation.md]], [[plan/core-creation/reports/grill-issue-11-facts.md]], [[plan/core-creation/reports/grill-issue-11-finish.md]], [[plan/core-creation/reports/asynchronous-core-task-manager-facts.md]], [[plan/core-creation/reports/kernel-fsproj.md]], [[plan/core-creation/reports/mirror-mode-removal-facts.md]], [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]], [[CONTEXT.md]].

## 1. Issue 12 question and settled inputs from 09, 10, 11

### Issue 12 (open)

[[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]] — Type: grilling. Status: open. Blocked by: 09, 10, 11.

Question: "When the Server or Core shuts down, how are running and queued Actors cancelled or awaited, how are already-enqueued Change batches treated, and what terminal job information must remain observable before process exit without adding process crash isolation?"

Map topology ([[plan/core-creation/reports/issue-02-ready-from-chat.md]], [[plan/core-creation/reports/chart-core-wayfinder-map.md]]): Command (09) gates cancel (10) gates finish (11) gates shutdown (12) gates issue 02.

### From issue 09 (resolved) — shutdown reuses job identity, not a job result

Launch returns a never-reused public number; Core retains number→Actor, send credential, span, Revision, and registered name ([[plan/core-creation/issues/09-define-core-command-launch-contract.md]] Answer). Actor initial state is extracted subgraph plus send credential only. Issue 11 amends "after the task ends" to mean after delete-actor applies, not when the Async/Task returns ([[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]] Answer).

### From issue 10 (resolved) — cancel token, sender admission, mailbox apply-already-enqueued

Core signals the Actor with a `CancellationToken` and refuses later output ([[plan/core-creation/issues/10-define-actor-cancellation-and-output-admission.md]] Answer). Match at Post: sender id (09 send credential for Actors) must match an active source; not active → auth refuse, do not enqueue. Adapter cookie fail (HTTP 401) and Core inactive-sender (Unauthorized) are one auth family. Already in the mailbox → apply. Cancel removes the source from the active set. Launch forbids overlapping spans (any shared NodeId). Core creates the advisory lock at launch. Lock-present is on the Node, not a job flag ([[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]] Answer).

[[plan/core-creation/reports/grill-issue-10-cancellation.md]] Round 1 Q2 answer: shutdown stays in issue 12; cancel Command is not the only internal cancel path ("Only internal (shutdown, finish)" was option C in the grill tree).

### From issue 11 (resolved) — no job result, delete-actor on stop, lock-present, registry until mailbox drain

No completed or aborted job result; callers do not get a job Error ([[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]] Answer). `postChange` `Ok` / `Error` (accept, dedup, Reject) is the Changes result to the Actor only. Accepted, deduplicated, and Rejected batches do not change a job terminal; they apply or Reject in mailbox order while the Actor is still registered.

When the Actor Async/Task has stopped for any reason, Core enqueues a Core-only delete-actor message (not a Change, not the Actor sender id). FIFO processes earlier Actor-sent Changes first. When delete-actor applies, Core removes the registry entry, drops the number, writes lock off the Nodes, and removes the send credential ([[plan/core-creation/reports/grill-issue-11-finish.md]] Locked contract).

Launch writes lock-present on each Node in the span immediately; lock status is not History; clients see it through state, Fetch, or Query ([[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]] Answer). Query by the public number works until delete-actor applies.

Issue 11 explicitly defers shutdown: "Shutdown stays [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]]." [[plan/core-creation/reports/grill-issue-11-finish.md]] Q2 notes shutdown drain stays in 12 if number/lock were dropped at task return before enqueued Changes applied.

### Not yet in Server code (facts from prior grill reports)

[[plan/core-creation/reports/grill-issue-10-facts.md]] §3: [[src/Server/Core/]] has no `CancellationToken`, cancel API, job registry, send credential, or admission gate. [[plan/core-creation/reports/grill-issue-11-facts.md]] §3: no job map, delete-actor, or query-by-number in Server code. Issue 12 grilling is contract-only until pool implementation.

## 2. Issue 13 and Persistence:Mode — Database unavailable and Change rejection

[[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]] — Type: task. Status: open. Independent of issue 01.

What to build: delete runtime mirror; remove `Persistence:Mode` from production Server decisions. When the database is available, writable path uses DbAgent; correlated files remain secondary. When the database is unavailable, retain file-backed Graph-data and file queries and **Reject Changes**. Legacy `Persistence:Mode` configuration is ignored.

Acceptance criteria (facts): with database available, production startup selects DbAgent for writable Changes regardless of `Persistence:Mode`; with no database available, Server Rejects Changes while Graph-data and file queries still work; runtime mirror deleted; production does not read `Persistence:Mode`.

[[plan/core-creation/project.md]] Issues list describes issue 13 as "use Database persistence when available and reject Changes when unavailable."

[[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]: Core owns "persistence-mode selection, the read-only rejection when the Database is unavailable, and the Database mirror" as Core policy, not agent forwarding.

[[plan/core-creation/initial-core-changes-implementation.md]] Settled constraints: "The supported production direction uses Database persistence when it is available and rejects Changes on the read-only Graph/file fallback when it is unavailable."

[[plan/core-creation/reports/mirror-mode-removal-facts.md]]: DB unavailable → retain read-only fallback via FileAgent wrapped with `readOnly`; writes rejected with read-only message; reads via FileAgent. Acceptance checklist includes "Db conn absent → `postChange` rejected with read-only message; Poll/state reads work via FileAgent."

Issue 13 is not implemented ([[plan/core-creation/reports/implement-initial-core-changes.md]]): no persistence-selection change or mirror deletion yet.

## 3. Current Server process shutdown

### Host entry and lifecycle

[[src/Server/Server.fs]] `Main.main`: builds `WebApplication`, calls `RouteRegistration.registerPersistenceAndRoutes`, then `app.Run()`. No custom `IHostApplicationLifetime.ApplicationStopping` handler. No registered `IHostedService` for FileAgent, DbAgent, Actor pool, or mailbox drain.

Grep of `src/**/*.fs` (2026-09-05): `IHostedService` appears only in [[doc/history/database-migration-notes.md]] as commentary, not in Server production code. `IHostApplicationLifetime` is used in [[src/Server/DailyGitSave.fs]] `register` for `ApplicationStarted` only — background git save via `Task.Run`; comment states it "Does not wait on or call DbAgent / FileAgent."

[[src/Server/RouteRegistration.fs]] `registerDailyGitSave` calls `DailyGitSave.register app.Lifetime persistence.DataDir` during route registration. No shutdown hook registered for agents or persistence.

### Agent and mailbox lifetime today

[[src/Server/Core/CoreRuntime.fs]] `create`: lazy `FileAgent.create`; `getHandle` selects DbAgent or FileAgent (or mirror) by `PersistenceMode` and `DbStatus`. Agents are created on first `GetHandle()` use; no explicit dispose or mailbox stop on process exit.

[[src/Server/FileAgent.fs]] and [[src/Server/DbAgent.fs]] each start a `MailboxProcessor<FileAgentMsg>` at agent creation. Mailboxes run until process termination; no `MailboxProcessor.Dispose` or drain API in production code.

No Actor pool, running Actor registry, or shutdown-specific cancel/await exists ([[plan/core-creation/reports/implement-issue-01-actor-produce-path.md]]: pool, cancel, finish, shutdown not added).

### ASP.NET default shutdown behavior (implicit)

Process exit follows generic host shutdown when `app.Run()` stops (SIGTERM, Ctrl+C, etc.). The codebase does not document or implement ordered drain of apply mailboxes, in-flight `PostAndAsyncReply`, DbAgent startup sweep, or background snapshot tasks before exit. [[plan/core-creation/reports/asynchronous-core-task-manager-facts.md]] Issue 12 open items name "await vs fire-and-forget; interaction with `runBounded` abandoned tasks; DbAgent startup `isReady` gate vs in-flight jobs" — not answered in code.

## 4. Mailbox disable and Change rejection when Database is down

### Handle-level read-only rejection (exists today)

[[src/Server/Core/CoreRuntime.fs]] `readOnly`: replaces `postChange` and `postGraphOnlyChange` with a function that returns `Error "Database persistence is unavailable; file fallback is read-only."` without calling the agent mailbox.

Selection in `create` `getHandle`:

- `PersistenceMode.Db`, `DbStatus.Ok` → DbAgent ([[src/Server/DatabaseSetup.fs]] `getOrCreateDbAgent`)
- `PersistenceMode.Db`, not `Ok` → `getFile () |> readOnly`
- `PersistenceMode.File`, `DbStatus.Ok` → mirror path (`ofFileWithDbMirror`)
- `PersistenceMode.File`, not `Ok` → writable FileAgent

[[src/Server/RouteRegistration.fs]] `createPersistenceContext` calls `CoreRuntime.create` with mode from `config.["Persistence:Mode"]` via `DatabaseSetup.resolvePersistenceMode`.

This is not a mailbox disable flag on `MailboxProcessor`; it is a CoreChanges wrapper that refuses writes before `PostChange` reaches FileAgent or DbAgent ([[plan/core-creation/reports/grill-issue-10-facts.md]] §1, §6; [[plan/core-creation/reports/contain-core-change-authority.md]]).

### Reads when DB unavailable

Read paths (`getState`, `getChangesSince`, etc.) still use FileAgent mailbox when fallback is active. [[plan/core-creation/reports/current-edit-core-reconciliation.md]]: "If db mode cannot get a healthy database, RouteRegistration exposes a read-only FileAgent handle. It rejects postChange."

Test: [[tests/Server.Tests/StateEndpointTests.fs]] ``DB mode without connection serves read-only file fallback`` — GET `/ambit/state` returns 200; POST Change returns HTTP 400 with "read-only" in error body; `.amb` file unchanged.

### DbAgent `isReady` (startup gate, not DB-stop disable)

[[src/Server/DbAgent.fs]]: `isReady` is `ready.Task.IsCompletedSuccessfully` after startup projection sweep completes. `CoreChanges.accepted` includes `isReady` in the acknowledgement ([[src/Server/Core/CoreChanges.fs]], [[src/Server/Api.fs]]). PostChange is not blocked at the mailbox when not ready; mutations can proceed during startup while `isReady` is false in responses. This is a startup/readiness fact, not a Database-stop or shutdown disable.

### Planned sender admission (not implemented)

Issue 10 locked Post-time Unauthorized for inactive Actor send credentials. That admission gate does not exist in [[src/Server/Core/]] or agent `postChange` today ([[plan/core-creation/reports/grill-issue-10-facts.md]] §3).

## 5. Process-crash isolation explicitly out of scope (issue 12 wording)

Issue 12 question ends with "without adding process crash isolation" ([[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]] Question).

[[plan/core-creation/reports/asynchronous-core-task-manager-facts.md]] Issue 12: "Question locked: … terminal observability; **no crash isolation**." Open items are shutdown drain semantics, not crash rings.

[[plan/core-creation/reports/kernel-fsproj.md]] OS analogy table: "Process crash isolation; separate rings | **Not required.** If an Actor hangs, abort it; the Graph stays consistent." Same report: "Process crash isolation is not required" for Core placement.

[[plan/core-creation/reports/grill-issue-11-facts.md]] §6: finish in plan docs is job posting and Poll — "not process shutdown, not pool drain, not crash isolation."

No Committed Decision under [[doc/Decisions/]] defines crash isolation for Actor pool shutdown (grep 2026-09-05; same as issue 11 facts report).

## 6. "DB stop" vs "process shutdown" in docs and code

### Documented distinction (plan language)

| Concept | Where described | Facts |
| --- | --- | --- |
| **Database unavailable / DB stop** | Issue 13, mirror-removal facts, initial-core-changes, decision 0003, current-edit-core-reconciliation | Runtime condition: PostgreSQL absent or unhealthy (`DbStatus` not `Ok`). Writable Changes rejected via `CoreRuntime.readOnly`; reads continue via FileAgent. Not tied to process exit. |
| **Server or Core shutdown** | Issue 12, issue 11 deferral, asynchronous-core-task-manager-facts, grill-issue-11-facts | Process or Core container stopping: cancel/await running and queued Actors; treat enqueued Change batches; terminal job observability before exit. Separate from finish (delete-actor) and separate from DB-unavailable read-only mode. |
| **Finish (Actor stop)** | Issues 10–11, grill-issue-11-finish | Actor Async/Task ended → enqueue delete-actor; registry until mailbox processes prior Actor-sent Changes. Not the same as process shutdown ([[plan/core-creation/reports/grill-issue-11-facts.md]] §6). |

Issue 10 grill ([[plan/core-creation/reports/grill-issue-10-cancellation.md]]): shutdown is an internal cancel path alongside Command cancel (Q2 option C was "Only internal (shutdown, finish)" — Alan chose Command cancel by NodeId; shutdown remains issue 12).

### Code: no explicit "DB stop" lifecycle hook

There is no runtime transition from DbAgent to read-only when a healthy database becomes unavailable after startup. `CoreRuntime.create` fixes handle selection at startup from `resolveDbConnection` ([[src/Server/DatabaseSetup.fs]], [[src/Server/RouteRegistration.fs]]). No code path re-wraps the handle in `readOnly` on connection loss mid-process.

Process shutdown does not trigger read-only mode or Change rejection; agents are not explicitly stopped ([[src/Server/Server.fs]], §3 above).

### Glossary ([[CONTEXT.md]])

**Core**: manages the Actor pool; owns persistent state. **Change**: Graph modification unit applied by Browser and Server. **Core API**: inner apply is the Changes path. **Agent** (LLM worker) is defined; _Avoid_: "Actor (for this counterpart)." **Actor** is not a glossary headword; used under Core and Core API only. No **mailbox** headword in [[CONTEXT.md]]; apply mailbox is plan/code term ([[plan/core-creation/reports/grill-issue-10-facts.md]] §2 names FileAgent/DbAgent `MailboxProcessor` as apply mailbox).

## 7. Cross-reference — open grilling dependencies for issue 12

Facts only; not decisions.

- Issue 12 blocked by resolved 09, 10, 11 ([[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]]); issue 02 blocked by 12 ([[plan/core-creation/issues/02-core-actor-pool.md]]).
- Settled reuse from 09–11: CancellationToken signal; Post admission and Unauthorized; already-enqueued Changes apply; delete-actor after Actor stop; no job result; lock-present on Nodes; public number until delete-actor applies.
- Issue 13 parallel fact: DB-unavailable → Reject Changes, keep reads — implemented today via startup `readOnly` wrapper, not mailbox disable.
- Server shutdown today: ASP.NET `app.Run()` default; no agent/mailbox/Actor cleanup; DailyGitSave on ApplicationStarted only.
- Crash isolation: explicitly excluded by issue 12 wording and kernel-fsproj facts.
