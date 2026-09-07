# Actor spine facts

Date: 2026-09-06. Git place: `dev` (dirty tree). No product code edit. Terms: [[CONTEXT.md]] Core, Core API, Change, Agent (LLM, not Actor). Actor here is the Server-side job in Core tickets.

## 1. Implemented in code now

[[src/Server/Core/CoreRuntime.fs]] `CoreRuntime` holds `changes`, `bindChanges`, `browserChanges`, `credentials`, and `command` (`CoreActorPool`). HTTP `/ambit/changes` posts through `browserChanges` ([[src/Server/RouteRegistration.fs]]). [[src/Server/Api.fs]] `postChange` decodes JSON and maps HTTP status. Admission is [[src/Server/Core/CoreCredentials.fs]] `CoreAuth.bindHandle`.

[[src/Server/Core/CoreChanges.fs]] `CoreChanges`: `getState`, `getRevision`, `getChangesSince`, `isReady`, `postChange`, `postGraphOnlyChange`. Return type `CoreChangesAccepted`: `revision`, confirmed Changes, `externalChanges`, `message`, `isReady`.

[[src/Server/Core/CoreActorPool.fs]]:

- Types: `ActorName`, `PublicNumber`, `LaunchRequest` (`name`, `revision`, `span` as `NodeRange`), `ActorFn` = `Graph -> Credential -> CoreChanges -> Async<unit>`.
- Pool fields: `register`, `launch`, `query`, `lockedIds`, `withLocks`.
- Pool `MailboxProcessor` messages: `Register`, `TryLaunch`, `Query`, `GetLocked`. Apply stays on FileAgent / DbAgent mailboxes.
- `launch`: `getState`, `GraphSpan.extract`, refuse unknown name or span overlap, `credentials.add` a job `Credential`, bind Posts, `Async.Start` the Actor. Returns a never-reused `PublicNumber`. Launch reply does not wait for Actor work.
- `withLocks` overlays lock-present on `getState` Graph.

Tests: [[tests/Server.Tests/CoreChangesTests.fs]] issue-01 test Actor (`Async.StartAsTask`, Normal `postChange`, Poll). [[tests/Server.Tests/CoreActorPoolTests.fs]] register, launch, query, overlap, job-credential `postChange` of Owner children. [[tests/Server.Tests/CoreRuntimeTests.fs]] bound Browser Changes and `command.query`.

`register` runs only in tests. No production `ActorFn`. No `/ambit` Command launch, query, or cancel route. `CoreCredentials.remove` exists; no Server caller. `ActorFn` has no `CancellationToken`. Core has no `cancel` field.

Nearby delivery: [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]] Status `done`. [[plan/core-creation/issues/16-track-running-job.md]] Status `done`.

## 2. Tickets 01 / 02 / 25

- [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] Status `done`. Gap: none on this ticket. Protocol is a test Actor plus Normal `postChange`. Pool, cancel, and Command stay on later tickets.
- [[plan/core-creation/issues/02-core-actor-pool.md]] Status `needs-info`. Unchecked boxes still name launch identity, cancel, finish through Changes, and cancel-after-enqueue design. Launch identity and query exist in code (15/16). Remaining vs those boxes: cancel, finish/drop, HTTP Command. Cancel-after-enqueue is locked on [[plan/core-creation/issues/10-define-actor-cancellation-and-output-admission.md]] (Status `resolved`) and not implemented. Blocked by 01 (`done`) and [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]] (`resolved`).
- [[plan/core-creation/issues/25-bind-changes-at-core-seam.md]] Status `done`. Gap: none. HTTP bound Changes only. The ticket does not start [[plan/core-creation/issues/17-cancel-a-job.md]].

## 3. What [[plan/event-sourced-ops/details/actors-and-jobs.md]] requires

Status: assessment, not a lock. Three stages:

1. Launch: own task off the apply queue; the launch request returns after spawn; the Client keeps a job identity; the Server maps that identity to a cancellation source and the task.
2. Finish and apply: the job builds Change objects and sends a message into the apply queue; the finishing task does not return to the original request; Browsers Poll; there is no completion push.
3. Cancel: cancel the token; the job must not send an apply message after cancel; an apply message already in the queue still runs (no cancel-after-enqueue).

The file states none of this exists as a product (no multi-job launcher, no job identity, no cancel interface). That sentence is older than [[src/Server/Core/CoreActorPool.fs]]. Parse in that file is still a request-scoped task; job identity, return-before-apply, and cancel are not required of Parse.

## 4. Minimum spine: start, HTTP call, postChange Owned children, cancel

Facts from types and tickets:

- Start: `register` plus `launch` (`LaunchRequest`). The Actor receives subgraph, job Credential, and bound `CoreChanges`. That path exists in Core. No production registration. No HTTP Command launch ([[plan/core-creation/issues/09-define-core-command-launch-contract.md]] is `resolved`; HTTP Command stays later per 15).
- External HTTP API: `ActorFn` is `Async<unit>` with no HTTP type. [[plan/core-creation/project.md]] keeps Actor definitions outside Core. Server Core has no HTTP client for an Actor.
- postChange Owned children: Normal `postChange` on the bound handle. Tests post `Op.NewNode` plus `ChildNode.owner`. Writes on the handle are only `postChange` and `postGraphOnlyChange` ([[plan/core-creation/reports/actor-core-and-mailbox-check.md]]).
- Cancel: [[plan/core-creation/issues/10-define-actor-cancellation-and-output-admission.md]] and [[plan/core-creation/issues/17-cancel-a-job.md]] require cancel by NodeId, a `CancellationToken` to the Actor, removal of the job Credential, and apply of already-enqueued mailbox items. 17 Status `ready-for-agent`, boxes unchecked. Pool has no cancel. `ActorFn` has no token.

## 5. Explicitly not built

- Production Actor definition (LLM, shell, or other). Parse stays Graph-only on `/ambit/file/parse`, not a pool Actor.
- HTTP Command launch, query, cancel.
- Cancel, delete-actor / finish-drop ([[plan/core-creation/issues/18-finish-and-drop.md]] `ready-for-agent`), host-stop drain ([[plan/core-creation/issues/19-database-down-and-host-stop.md]]).
- Browser lock UI ([[plan/core-creation/issues/21-client-shows-lock-present.md]]), Browser cancel ([[plan/core-creation/issues/22-client-cancels-a-job.md]]).
- Completion push. A Core-level `postChange` facade (25 / [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]).
