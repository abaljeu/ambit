# Core API boundary review

Date: 2026-09-06

Independent review. No product edits. Range: committed Core on `dev` (issues 03–15) plus the dirty tree. [[../issues/16-track-running-job.md|16 (Track running job)]] is Status `done` but [[src/Server/Core/CoreActorPool.fs]] and [[tests/Server.Tests/CoreActorPoolTests.fs]] are still uncommitted. [[../issues/20-client-presents-credential.md|20 (Client presents credential)]] is Status `done` with dirty [[src/Client/JsInterop.fs]] and untracked [[tests/Server.Tests/BrowserCredentialTests.fs]]. Treat those files as mid-edit.

Spec: [[../map.md]], [[../project.md]], issues 03–16, [[CONTEXT.md]], [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]].

## Verdict

**Partial.** The Graph-agent package is a typed Changes Interface. Command and Actor types live in Core. Production still does not hold a Core object, and it does not present a `Credential` on Core posts. Files and general Query are later work, as the map retains them.

## Already a boundary

- [[src/Server/Core/CoreChanges.fs]] is the live GraphAgentHandle: typed `Change list`, `State`, `Revision`, `CoreChangesAccepted`. No `HttpRequest`, JSON, or HTTP status on that Interface. The map still names GraphAgentHandle; the code name is `CoreChanges`.
- Production HTTP and Parse call that handle. [[src/Server/Api.fs]] `postChange` decodes JSON then `handle.postChange`. [[src/Server/Api.fs]] `postParseFile` and [[src/Server/LazyLoadReconciliationServer.fs]] call `postGraphOnlyChange`. [[src/Server/RouteRegistration.fs]] does not take a raw `FileAgent` or `DbAgent`.
- [[src/Server/Core/CoreRuntime.fs]] selects the agent and overlays lock-present. [[src/Server/FileAgent.fs]] and [[src/Server/DbAgent.fs]] `postChange` are private. Tests may still construct agents ([[../issues/05-place-core-changes-in-existing-projects.md|05 (Place Core Changes in the existing projects)]]).
- [[src/Server/Core/CoreActorPool.fs]] uses `ActorName`, `PublicNumber`, `LaunchRequest`, `NodeRange`, `Credential`, `Graph`. Launch keeps the send credential in the Actor and the Core set. Query (dirty tree) returns retained launch identity, not a job result.
- Lock-present is a live Node overlay in Core ([[src/Shared/GraphSpan.fs]]). History and SQL omit lock ([[../issues/15-launch-actor-and-hold-span.md|15 (Launch an Actor and hold the span)]]).
- [[src/Server/Core/CoreCredentials.fs]] `CoreAuth.post` and Adapter mapping of `Unauthorized` to HTTP 401 exist. Cookie fail at the Adapter is the Browser source locked in [[../issues/10-define-actor-cancellation-and-output-admission.md|10 (Define Actor cancellation and output admission)]].

## Gaps

1. **No Core object at call sites.** [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]] wants `core.changeAgent.postChange`. [[CONTEXT.md]] Core API is Files, Changes, Query, Command. [[src/Server/RouteRegistration.fs]] unpacks `CoreRuntime` into `PersistenceContext`, keeps `GetHandle: unit -> CoreChanges`, and drops `pool`. There is no `core.command.launch` or `core.command.query`. `PersistenceContext.Credentials` is stored and never read.
2. **Production Changes has no sender `Credential`.** [[../issues/14-server-tracks-credentials.md|14 (Server tracks credentials)]]: every message to Core must present a live credential. `CoreAuth.post` is test-only. [[src/Server/Api.fs]] and agent `coreChanges.postChange` enqueue with no sender. An Actor that holds `CoreChanges` can post without `CoreAuth.post`.
3. **Files still take primitive `dataDir`.** [[src/Server/Api.fs]] file-status, import, and git-save call [[src/Server/DocumentPersistence.fs]] and [[src/Server/GitSave.fs]]. Persist algorithms still open files. [[../issues/07-define-core-files-contract.md|07 (Define the Core Files contract)]] is open grilling. This is deferred map work, not a closed-ticket miss.
4. **Query is not a Query subobject.** Reads go through `CoreChanges.getState` / `getChangesSince`. [[../issues/08-define-core-query-contract.md|08 (Define the Core Query contract)]] is open grilling. Job query is on `CoreActorPool`, not HTTP Command.
5. **Primitive leaks.** `Result<_, string>` on Core posts and launch/query. [[src/Server/GraphOnlyChangePost.fs]] `revision: int`. FileAgent/DbAgent `getChangesSince` take `int`. Parse body `fileId: string`. Cookie token strings in [[src/Server/RouteRegistration.fs]]. `getRevision` / `getChangesSince` still `failwith` via agent `unwrap` ([[.cursor/rules/fsharp-source.mdc]]).

## Core vs Adapter vs Client vs Files

| Belong in Core now | Adapter / Parse | Later Files / Query | Not this map increment |
| --- | --- | --- | --- |
| Graph write, History, agent selection, credential set, Actor pool, lock-present overlay | HTTP JSON, cookie gate, `/ambit` routes | Files send/get/git; typed Query | Browser lock UI, advisory soft-lock policy ([[plan/event-sourced-ops/project.md]]), ACID authority ([[plan/roadmap/epics/chapters/acid-apply.md]]) |

Parse algorithms stay out and must call typed Graph-only Post. That path is in place. Actor *definitions* stay out; the pool Interface is in Core.

## UI vs map

[[../map.md]] Out of scope: advisory soft-lock policy and Browser UI belong to [[plan/event-sourced-ops/project.md]]. [[../issues/20-client-presents-credential.md|20 (Client presents credential)]] is Adapter cookie on fetch, not lock UI. The implement-20 report calls it the first UI ticket; it does not add a Core credential for the Browser. [[../issues/21-client-shows-lock-present.md|21 (Client shows lock-present)]] is the first lock UI. It does not belong in this Core API increment.

## Recommended next

Do not start [[../issues/21-client-shows-lock-present.md|21 (Client shows lock-present)]]. Do not start new work on [[../issues/16-track-running-job.md|16 (Track running job)]] while its Core files are mid-edit; let that land.

Next for a clean Core object: close the 14 Post-admission seam so every `postChange` presents a `Credential`, and stop unpacking `CoreRuntime` at HTTP. That is Core API work, not Files ([[../issues/07-define-core-files-contract.md|07]]) and not Query grilling ([[../issues/08-define-core-query-contract.md|08]]). After that, [[../issues/17-cancel-a-job.md|17 (Cancel a job)]] needs sender-at-Post and CancellationToken.

## Standards

Hard: [[.cursor/rules/fsharp-source.mdc]] forbids Exceptions; FileAgent/DbAgent `unwrap` is `failwith` on `getRevision` / `getChangesSince`; [[src/Server/Api.fs]] `getState` uses `try/with`. Mutable outside the mailbox exception: [[src/Server/DatabaseSetup.fs]] `dbAgentCache` `ref` plus `lock`, used by [[src/Server/Core/CoreRuntime.fs]]. Containment: [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]] wants no public agent constructor; `FileAgent.create` / `DbAgent.create*` stay public (allowed for tests by 05). Size: uncommitted bindings under 40/100.

Judgement: Primitive Obsession on `string` Error, `int` revision, cookie strings, unused `Credential`. Feature Envy: HTTP inspects Core error strings instead of `CoreCredentials`. Middle Man: `PersistenceContext` flattens `CoreRuntime`; 0003 overrides Middle Man against Core holding agent handles. Duplicated Code: FileAgent and DbAgent twin `unwrap` and `coreChanges`.

## Spec

(a) Missing or partial: 14 every Core message presents a credential — production does not. 0003 / CONTEXT Core object — `RouteRegistration` unpacks `CoreRuntime` and drops `pool`. CONTEXT Files — `dataDir` primitives; 07 still open. 05 typed `Revision` — `GraphOnlyChangePost` uses `int`. 20 Browser credential — cookie at Adapter only; cookie is not in the Core set.

(b) Scope creep: none in the 16/20 diff. Query returns `LaunchRequest`. Cookie on POST fetch is the 20 slice.

(c) Looks implemented, wrong: 14 checkboxes vs seam — Actors can `postChange` without `CoreAuth.post`. `CoreRuntime` looks like the container, then is dismantled.

## Axis totals

Standards: 3 hard, several judgement smells; worst is Exceptions on the Core Changes read path. Spec: 5 missing/partial, 0 creep, 2 wrong-at-seam; worst is production Changes with no sender `Credential`.
