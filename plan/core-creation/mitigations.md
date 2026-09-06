# Close Core object seam and increment boundary

See also: [[map.md]], [[project.md]], [[reports/core-api-boundary-review.md]], [[issues/23-close-core-object-seam.md|23 (Close Core object seam)]], [[issues/24-clarify-core-increment-boundary.md|24 (Clarify Core increment boundary)]], [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]], [[CONTEXT.md]]

Locked refactor plan for two tickets. Do not implement in the planning session. Grain is **steps** (each step leaves a working tree).

## Problem Statement

The Core API seam is **partial**. [[src/Server/Core/CoreChanges.fs]] is a typed Graph-agent package. Command and Actor types live in Core. Production still does not hold a Core object, and it does not present a `Credential` on Core posts.

[[src/Server/RouteRegistration.fs]] unpacks [[src/Server/Core/CoreRuntime.fs]] into `PersistenceContext`, keeps `GetHandle`, and drops `pool`. `PersistenceContext.Credentials` is stored and never read. [[src/Server/Api.fs]] `postChange`, Graph-only Parse posts, and an Actor that holds `CoreChanges` enqueue with no sender. [[issues/14-server-tracks-credentials.md|14 (Server tracks credentials)]] is Status `done`; the Post-admission seam is not closed.

Call sites still use primitives (`string` Error, `revision: int`, cookie strings) where the Core Interface wants `Credential`, `Revision`, `Change list`, `ActorName`, `PublicNumber`, and `NodeRange`.

The next worker has no one agent-facing boundary: what belongs in Core vs Adapter vs Browser, that this increment does not add lock UI, and that callers must use typed Core functions.

## Solution

Do [[issues/24-clarify-core-increment-boundary.md|24 (Clarify Core increment boundary)]] first: one canonical **Agent instruction** in [[project.md]], a map pointer, and an optional scoped rule that only points. Then do [[issues/23-close-core-object-seam.md|23 (Close Core object seam)]]: keep the Core object at HTTP, present a live `Credential` on every production post, and type the admission Error. After 23, [[issues/17-cancel-a-job.md|17 (Cancel a job)]] may use sender-at-Post.

Core is a container of subobjects ([[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]). The HTTP host holds that container. Callers use `core.changes` and `core.command`. They do not flatten `CoreRuntime` and drop the pool.

## Order

1. [[issues/24-clarify-core-increment-boundary.md|24 (Clarify Core increment boundary)]] — instruction only. Estimate 45m.
2. [[issues/23-close-core-object-seam.md|23 (Close Core object seam)]] — corrected code. Estimate 3h. Blocked by 24 until 24 is Status `done`.
3. Then [[issues/17-cancel-a-job.md|17 (Cancel a job)]] (already Blocked by 23). Do not start 17 in this plan.

24 first because 23's ticket says to read 24 before coding. The instruction is the guardrail against lock UI, primitive Core calls, and unpacking `CoreRuntime`. 23 first would correct code with no standing instruction for the next worker.

## Success criteria

24 is done when a worker who opens this Project can tell Core vs Adapter vs Browser, will not add Browser lock UI in this increment, and will not call Core with cookie strings or new `dataDir` primitives. Canonical text is in [[project.md]]. Bridges and [[CONTEXT.md]] are unchanged.

23 is done when production-shaped posts present a live `Credential` and are auth-refused when the sender is not live, and when HTTP composition reaches Changes and Command through the Core object (not a dismantled runtime). Focused Server.Tests prove those two facts.

## Steps

### 24 (Clarify Core increment boundary)

Instruction only. Do not implement 23. Do not rewrite application source.

#### 24.1 Canonical Agent instruction in project.md

Edit [[project.md]]. Place a short **Agent instruction** section immediately after **Committed Decisions** (before **Implementation plan**).

Write these claims. Keep them short. Use [[CONTEXT.md]] words (Core, Core API, Adapter, Browser, Graph, Change, Credential, Revision). Do not copy the review table.

- Core owns Graph write, History, agent selection, the credential set, the Actor pool, and lock-present overlay. HTTP JSON, cookie gate, and `/ambit` routes stay in the Adapter. Persist algorithms and Parse algorithms stay outside Core. Parse calls typed Graph-only Post. Actor definitions stay outside Core; the pool Interface stays in Core.
- Callers hold the Core object ([[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]). They call typed functions on its subobjects: `Credential`, `Revision`, `Change`, Command types (`ActorName`, `PublicNumber`, `NodeRange`). They do not unpack [[src/Server/Core/CoreRuntime.fs]] into a flattened HTTP context. Cookie token strings stay in the Adapter. This increment does not pass `dataDir` into Core.
- Files stay [[issues/07-define-core-files-contract.md|07 (Define the Core Files contract)]]. General Query stays [[issues/08-define-core-query-contract.md|08 (Define the Core Query contract)]]. Job query by public number stays on the pool ([[issues/16-track-running-job.md|16 (Track running job)]]).
- This increment does not add Browser lock UI. [[issues/21-client-shows-lock-present.md|21 (Client shows lock-present)]] belongs with [[plan/event-sourced-ops/project.md]].
- Point at this plan: [[mitigations.md]].

Do not paste the Core vs Adapter vs Client table from [[reports/core-api-boundary-review.md]]. One paragraph plus bullets is enough.

Verify: [[project.md]] has the section; no application `.fs` changed.

#### 24.2 One Notes sentence on the map

Edit [[map.md]] **Notes**. Add one sentence that points at the new [[project.md]] section. Do not copy the review table into **Decisions so far**. Do not change **Out of scope** (Browser UI is already named).

Example sentence: `Agent instruction for Core vs Adapter vs Browser (typed Core object; no lock UI this increment) lives in [[project.md]].`

Verify: Notes has the pointer; Decisions so far is unchanged.

#### 24.3 Scoped rule pointers only

Add [[.cursor/rules/core-api.mdc]] so Server Core / Adapter editors who miss [[project.md]] still get a pointer. Globs limited to Core and Adapter sources, for example:

- `src/Server/Core/**/*.fs`
- `src/Server/Api.fs`
- `src/Server/RouteRegistration.fs`
- `src/Server/GraphOnlyChangePost.fs`
- `src/Server/LazyLoadReconciliationServer.fs`

Rule body is pointers only:

- [[plan/core-creation/project.md]] Agent instruction
- [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]
- [[CONTEXT.md]] Core API

No duplicated table. No Core vs Adapter essay. No ticket checklists.

Then add one index line in [[.cursor/rules/gambol.mdc]] next to the other scoped rules. Follow [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]].

Do not edit `AGENTS.md`, `.cursor/copilot-instructions.md`, or `.cursor/codex-context.md`. Do not rewrite [[CONTEXT.md]] (Core API is already defined). Do not start 21. Do not treat 07 or 08 as closed.

Verify: the new rule file exists; gambol.mdc lists it; bridges and CONTEXT are untouched.

#### 24.4 Close the ticket

Set [[issues/24-clarify-core-increment-boundary.md|24 (Clarify Core increment boundary)]] Status `done`. Append **Answer** with a one-line gist and a link to [[project.md]]. Log time. [[issues/23-close-core-object-seam.md|23 (Close Core object seam)]] is then unblocked.

### 23 (Close Core object seam)

Corrected code. Read the Agent instruction in [[project.md]] before coding. Test-first per [[.cursor/skills/implement-fsharp-feature/SKILL.md]] and [[.cursor/rules/testing-workflow.mdc]]. Each step must compile and leave focused tests green.

Do not change [[src/Server/FileAgent.fs]] / [[src/Server/DbAgent.fs]] mailbox `postChange` to require `Credential`. Tests may still construct agents ([[issues/05-place-core-changes-in-existing-projects.md|05 (Place Core Changes in the existing projects)]]). Production HTTP must not call `FileAgent.create` / `DbAgent.create*`.

Prior art: [[tests/Server.Tests/CoreCredentialsTests.fs]], [[tests/Server.Tests/CoreChangesTests.fs]], [[tests/Server.Tests/CoreActorPoolTests.fs]], [[tests/Server.Tests/GraphOnlyChangePostTests.fs]].

Focused filter as you go:

```sh
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~CoreCredentialsTests|FullyQualifiedName~CoreChangesTests|FullyQualifiedName~CoreActorPoolTests|FullyQualifiedName~GraphOnlyChangePostTests"
```

No Shared or Browser edit is in this ticket. Do not run the Client compile gate unless a Shared or Client file actually changes.

#### 23.1 Red: production-shaped post without a live Credential

Add tests (extend [[tests/Server.Tests/CoreCredentialsTests.fs]] or a new [[tests/Server.Tests/CoreRuntimeTests.fs]]) that state the two success facts:

- A production-shaped post (HTTP Adapter or `CoreAuth.post` on a handle from `CoreRuntime`) presents a `Credential` and is not enqueued when that sender is not live. Same refuse family as Adapter cookie fail (`Unauthorized` → HTTP 401).
- A live sender is enqueued.

These tests fail or are incomplete until later steps wire production. Keep the existing mailbox-level `CoreAuth.post` tests.

Verify: new tests exist and fail for the missing HTTP/Actor wiring, or they pass only the already-implemented `CoreAuth.post` cases.

#### 23.2 Process-lifetime Browser and Parse credentials

Edit [[src/Server/Core/CoreRuntime.fs]] `create`. After `CoreCredentials.create`, add two process-lifetime credentials to the set: one Browser credential (Adapter presents it after the cookie gate) and one Parse credential (Graph-only production posts). Expose them on `CoreRuntime` (names such as `browserCredential` and `parseCredential`).

Do not put cookie token strings on Core. Do not invent per-session credentials (that is beyond 20).

Test: after `CoreRuntime.create`, both credentials `contains` true.

Verify: focused CoreRuntime/CoreCredentials tests green. HTTP still posts with no sender (next steps).

#### 23.3 HTTP Adapter presents Credential on postChange

Edit [[src/Server/Api.fs]] `postChange`. After JSON decode, call `CoreAuth.post` (or `CoreAuth.bind`) with the given `Credential`, then the handle's `postChange`. Keep decode failures and Change Reject text from [[issues/03-define-typed-core-changes-contract.md|03 (Define the typed Core Changes contract)]]. Keep `agentErrorResult` mapping `CoreAuth.refuse` to `Results.Unauthorized()`.

Edit [[src/Server/RouteRegistration.fs]] `/ambit/changes`: after `auth.IsAuthenticated`, pass `runtime.browserCredential`. Cookie fail stays 401 at the Adapter and does not reach Core.

Update call sites in [[tests/Server.Tests/CoreChangesTests.fs]] and [[tests/Server.Tests/CoreCredentialsTests.fs]] that invoke `Api.postChange`.

Verify: inactive Core sender is 401 and not enqueued; live Browser credential enqueues; cookie fail remains 401; Change Reject is still not 401.

#### 23.4 Graph-only production posts present the Parse credential

Production Graph-only callers must not call `postGraphOnlyChange` with no sender:

- [[src/Server/Api.fs]] `postParseFile`
- [[src/Server/GraphOnlyChangePost.fs]] (callers pass a bound post)
- [[src/Server/LazyLoadReconciliationServer.fs]]

Bind with `parseCredential` via `CoreAuth.post` / `CoreAuth.bind`. Parse algorithms stay outside Core. Do not move plan/reconcile into Core.

Verify: Graph-only tests still post chunks; a test that uses a bound production-shaped post refuses an inactive sender.

#### 23.5 Actor posts present the job Credential

Edit [[src/Server/Core/CoreActorPool.fs]] `runLaunch`. The handle passed into `ActorFn` must have `postChange` and `postGraphOnlyChange` bound with `CoreAuth` and `plan.credential` (already added to the set). Do not change `ActorFn` shape (`Graph -> Credential -> CoreChanges -> Async<unit>`). Do not implement cancel.

Test in [[tests/Server.Tests/CoreActorPoolTests.fs]]: an Actor that posts through the given handle enqueues with the live job credential; a post with a different inactive credential is refused. Tests that construct `FileAgent.coreChanges` and post without a credential remain valid for 05.

Verify: launch tests green; new Actor-post admission test green.

#### 23.6 GraphOnlyChangePost uses Revision

Edit [[src/Server/GraphOnlyChangePost.fs]]: the chunk walker takes `Revision`, not `revision: int`. Use `accepted.revision` for the next chunk. `Change.id` may still be the integer value; do not change Shared `Change` in this ticket.

Verify: [[tests/Server.Tests/GraphOnlyChangePostTests.fs]] and reconcile tests green.

#### 23.7 HTTP holds the Core object

Edit [[src/Server/RouteRegistration.fs]] `PersistenceContext`:

- Hold `CoreRuntime` (the Core object). Call it `Core` at the field if that reads clearly.
- Remove `GetHandle` and the unused `Credentials` field.
- Keep `DataDir`, `Mode`, and `DbStatus` for current Files HTTP ([[issues/07-define-core-files-contract.md|07 (Define the Core Files contract)]]). Do not add more `dataDir` primitives onto Core.
- Flush and file Revision already live on `CoreRuntime`; callers use the Core object.

Rename `CoreRuntime.getHandle` to `changes` (still `unit -> CoreChanges` because lock overlay is applied per call). Add `command` as the Actor-pool subobject (the live [[src/Server/Core/CoreActorPool.fs]] value). Call sites use `core.changes ()` and `core.command.launch` / `core.command.query`. Do not add a Core-level `core.postChange` forwarder ([[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]).

Do not invent a Query subobject. Reads may still go through `core.changes().getState` / `getChangesSince` until [[issues/08-define-core-query-contract.md|08 (Define the Core Query contract)]].

Edit [[src/Server/Api.fs]] only as needed to accept the handle and credential from RouteRegistration. Do not pass the whole Core object into every Api function if the Adapter already received the subobject plus `Credential`.

Test: composition uses `Core` / `command`; `pool` is not dropped on the floor. A focused test can construct `CoreRuntime.create` and call `command.query` / `changes` without unpacking a parallel `PersistenceContext` handle.

Verify: Server compiles; state/poll/load/changes/parse routes still resolve a handle from `persistence.Core`.

#### 23.8 Typed admission Error

Add a small DU for admission only, in [[src/Server/Core/CoreCredentials.fs]] (next to `CoreAuth`), for example `Unauthorized`, `UnknownJob`, `UnknownActor`, `Overlap`. Use it for `CoreAuth.admit` / `CoreAuth.post` and for `command.launch` / `command.query`.

Keep Change Reject as `string` on the Changes path ([[issues/03-define-typed-core-changes-contract.md|03]]). Do not replace persistence or TCP errors with this DU (issue 12 / 13 family).

Adapter: `Unauthorized` maps to HTTP 401. Unknown job / overlap stay a non-auth Error at Core; do not add HTTP Command routes here.

Update [[tests/Server.Tests/CoreActorPoolTests.fs]] and [[tests/Server.Tests/CoreCredentialsTests.fs]] to match the DU. String literals `unknown job` / `span overlaps a live job` may remain as display text behind the DU if a caller still needs text; the Core Interface type is the DU.

Verify: auth refuse is still one family; launch overlap and unknown job are not 401.

#### 23.9 Close the ticket

Confirm both success facts with the focused filter. Set [[issues/23-close-core-object-seam.md|23 (Close Core object seam)]] Status `done`. Append **Answer**. Log time. [[issues/17-cancel-a-job.md|17 (Cancel a job)]] is then unblocked on the 23 dependency. Set Project Stage back to `active` (implementation underway) when 23 coding starts if this plan left Stage at `spec`.

## Decision Document

- **24 then 23.** Instruction is the guardrail. 23 is blocked on 24 in the tracker.
- **Core object is `CoreRuntime`.** Do not add a second container type. Expose `changes` and `command` on it. `command` is the live `CoreActorPool` (launch, query, lock overlay). Do not add [[src/Server/Core/CoreCommand.fs]] unless the file size rule forces a split.
- **Admission at production posts, not in agent mailboxes.** `CoreAuth.post` / `CoreAuth.bind` wrap `CoreChanges.postChange` and `postGraphOnlyChange`. FileAgent and DbAgent keep `Change list -> ...` for tests (05).
- **Two process-lifetime credentials** on Core create: Browser (after Adapter cookie) and Parse (Graph-only). Actors keep the launch credential already added in 15. Cookie strings stay in the Adapter.
- **No Core-level forwarder.** RouteRegistration holds Core and calls subobjects. Api is decode/encode around typed Changes plus `Credential`.
- **Typed Error is admission only.** Change Reject stays text. Files and Query stay 07 and 08.
- **PersistenceContext keeps DataDir/Mode/DbStatus** for current file-status, import, and git-save. That is 07 leftover, not a new Core primitive.
- **This plan is effort-local scope**, not a product commitment ([[doc/agents/scope-vs-commitment.md]]).

## Testing Decisions

Good tests prove external behavior at the Core Interface: a sender is live or not; a post is enqueued or refused; HTTP 401 is the auth family; callers hold Core and reach `changes` / `command`. Do not assert `PersistenceContext` field names as the spec. Do not test FileAgent mailbox internals for 23.

Modules under test: CoreRuntime, CoreAuth, Api.postChange, Graph-only bind, CoreActorPool launch wrap, GraphOnlyChangePost `Revision`. Prior art listed in 23.1.

Do not convert all agent tests to `CoreAuth.post`. Do not add Browser tests. Do not start 21.

## Out of Scope

Out of scope for this plan (this Project increment), not product-wide exclusions:

- [[issues/21-client-shows-lock-present.md|21 (Client shows lock-present)]] and advisory soft-lock Browser UI — [[plan/event-sourced-ops/project.md]] / [[map.md]] Out of scope.
- [[issues/07-define-core-files-contract.md|07 (Define the Core Files contract)]] — file-status, import, git-save, `dataDir`. Relate only: 23 must not add more `dataDir` on Core.
- [[issues/08-define-core-query-contract.md|08 (Define the Core Query contract)]] — do not invent a Query subobject. Job query stays on the pool (16).
- [[issues/17-cancel-a-job.md|17 (Cancel a job)]] — sequence after 23. Do not implement cancel, CancellationToken, or credential removal on stop.
- Making `FileAgent.create` / `DbAgent.create*` private (05 allows tests).
- Fixing `failwith` / `try/with` on getState / getRevision (standards debt, not 23).
- Per-session Core credentials, Client source, or new HTTP Command routes.
- Rewriting [[CONTEXT.md]], bridges, or global always-apply policy.

## Defaults Alan can override

Not blocking. The plan is locked on these defaults:

1. One process-lifetime Browser credential after the cookie gate, not a new credential per Session.
2. Wrap production posts with `CoreAuth`; do not add `Credential` to FileAgent/DbAgent mailbox signatures.
3. Project Stage is `spec` while this plan is the next work. Return to `active` when 23 implementation starts.

## Further Notes

[[issues/14-server-tracks-credentials.md|14 (Server tracks credentials)]] stays `done`. 23 closes the remaining Post-admission seam. Map **Not yet specified** (final Core API composition after Files and Query) stays open.
