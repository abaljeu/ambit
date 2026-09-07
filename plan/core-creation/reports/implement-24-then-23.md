# Implement 24 then 23

Date: 2026-09-06

Locked plan: [[plan/core-creation/mitigations.md]]. Did not commit. Did not start [[plan/core-creation/issues/17-cancel-a-job.md|17 (Cancel a job)]]. Did not start [[plan/core-creation/issues/21-client-shows-lock-present.md|21 (Client shows lock-present)]]. Did not swallow [[plan/core-creation/issues/07-define-core-files-contract.md|07 (Define the Core Files contract)]] or [[plan/core-creation/issues/08-define-core-query-contract.md|08 (Define the Core Query contract)]].

## 24 (Clarify Core increment boundary)

Status `done`. Instruction only.

- Canonical **Agent instruction** in [[plan/core-creation/project.md]] after Committed Decisions.
- One Notes sentence on [[plan/core-creation/map.md]] pointing at that section. Decisions so far unchanged.
- Scoped pointers: [[.cursor/rules/core-api.mdc]], indexed from [[.cursor/rules/gambol.mdc]].
- No bridges, no [[CONTEXT.md]] rewrite, no application `.fs`.

## 23 (Close Core object seam)

Status `done`. Project Stage `active`.

- [[src/Server/Core/CoreRuntime.fs]] is the Core object: `changes`, `command`, process-lifetime `browserCredential` and `parseCredential`.
- [[src/Server/RouteRegistration.fs]] `PersistenceContext` holds `Core`. Callers use `core.changes ()` and pass Browser / Parse credentials into Adapter posts. `GetHandle` and unused `Credentials` are gone. `DataDir` / `Mode` / `DbStatus` stay for Files HTTP.
- [[src/Server/Api.fs]] `postChange` and `postParseFile` call `CoreAuth.post` after decode. Cookie fail stays 401 at the Adapter.
- Production Graph-only posts bind `parseCredential` (`postParseFile`, git reconcile, directory/added routes).
- [[src/Server/Core/CoreActorPool.fs]] `runLaunch` binds the Actor handle with the job credential. `ActorFn` shape is unchanged.
- [[src/Server/GraphOnlyChangePost.fs]] takes `Revision`.
- [[src/Server/Core/CoreCredentials.fs]] `CoreAdmissionError` DU. `CoreAuth.admit` returns it. `CoreAuth.post` / `bind` / `bindHandle` wrap production posts. Change Reject and `getState` errors stay `string` on those Result paths; admission display text is `CoreAdmissionError.text`. Overlap and unknown job are not HTTP 401.
- FileAgent / DbAgent mailbox `postChange` stays unwrapped for tests ([[plan/core-creation/issues/05-place-core-changes-in-existing-projects.md|05 (Place Core Changes in the existing projects)]]).

## Tests

Focused Server.Tests, all green:

- CoreRuntimeTests, CoreCredentialsTests, CoreChangesTests, CoreActorPoolTests, GraphOnlyChangePostTests, BrowserCredentialTests, LazyLoadReconciliationServerTests (50).
- Extra HTTP path: StateEndpointTests, ChangeEndpointResilienceTests (68).

No Shared or Client edit. Client compile gate not run.

## Leftover tree

- [[plan/core-creation/issues/17-cancel-a-job.md|17 (Cancel a job)]] is unblocked on 23. Not started (plan default: stop after 23). Not a tiny leftover of this seam.
- [[plan/core-creation/issues/07-define-core-files-contract.md|07 (Define the Core Files contract)]] — file-status, import, git-save, `dataDir`.
- [[plan/core-creation/issues/08-define-core-query-contract.md|08 (Define the Core Query contract)]] — reads still `getState` / `getChangesSince`. Job query stays on `command.query`.
- [[plan/core-creation/issues/21-client-shows-lock-present.md|21 (Client shows lock-present)]] — Browser lock UI, [[plan/event-sourced-ops/project.md]].
- Map **Not yet specified** (final Core API composition after Files and Query) stays open.
- `command.launch` / `command.query` Error remains `string` so persistence errors are not forced into the admission DU.
