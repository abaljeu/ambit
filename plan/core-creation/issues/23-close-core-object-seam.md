# 23 — Close Core object seam

**Status:** ready-for-agent
**Blocked by:** None — can start immediately.
**Estimate:** 3h

## Context

[[plan/core-creation/reports/core-api-boundary-review.md]] verdict is **partial**. [[14-server-tracks-credentials.md|14 (Server tracks credentials)]] is Status `done`, but production still posts Changes with no sender `Credential`. HTTP unpacks [[src/Server/Core/CoreRuntime.fs]] and drops the Actor pool. Callers use primitives (`string` Error, `revision: int`, cookie strings) instead of the typed Core API. This ticket corrects that code. It does not re-open [[07-define-core-files-contract.md|07 (Define the Core Files contract)]] or [[08-define-core-query-contract.md|08 (Define the Core Query contract)]]. It does not start [[21-client-shows-lock-present.md|21 (Client shows lock-present)]]. Read [[24-clarify-core-increment-boundary.md|24 (Clarify Core increment boundary)]] before coding.

Domain: outliner Graph, files, and Actors. Files stay behind 07 except that this ticket must not add more `dataDir` primitives on Core. Actors stay behind the Command / pool Interface already in Core.

## What to build

Close the Core object / typed DDD function seam. Production `postChange` presents a live `Credential`. Call sites hold a Core object and call typed functions on its subobjects. They do not unpack `CoreRuntime` into a flattened HTTP context and drop `pool`.

- [ ] Every production post to Core Changes presents a live `Credential` (`CoreAuth.post` or the typed Changes function that admits). HTTP [[src/Server/Api.fs]] `postChange`, Graph-only [[src/Server/GraphOnlyChangePost.fs]] / [[src/Server/LazyLoadReconciliationServer.fs]], and an Actor that holds `CoreChanges` do not enqueue with no sender.
- [ ] The HTTP host keeps a Core object ([[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]). [[src/Server/RouteRegistration.fs]] does not unpack `CoreRuntime` into `PersistenceContext`, drop `pool`, and keep an unused `Credentials` field. Callers use `core.changes.*` and `core.command.launch` / `core.command.query` (names may match the live modules) instead of a bare `GetHandle`.
- [ ] Typed functions, not primitives, at the Core seam: `Credential`, `Revision`, `Change list`, `ActorName`, `PublicNumber`, `NodeRange`. Cookie token strings stay in the Adapter. `GraphOnlyChangePost` uses `Revision`, not `revision: int`. Admission Error is typed (`Unauthorized`, unknown job, overlap refuse), not a free `string`. Existing Change Reject text from [[03-define-typed-core-changes-contract.md|03]] may remain.
- [ ] Tests prove the two success facts: production-shaped posts present `Credential` and are auth-refused when the sender is not live; callers reach Changes and Command through the Core object, not a dismantled runtime.

## Related, not this ticket

- [[07-define-core-files-contract.md|07 (Define the Core Files contract)]] — file-status, import, and git-save still take primitive `dataDir` via [[src/Server/DocumentPersistence.fs]] and [[src/Server/GitSave.fs]]. Leave that to 07.
- [[08-define-core-query-contract.md|08 (Define the Core Query contract)]] — reads still go through `CoreChanges.getState` / `getChangesSince`. Do not invent a Query subobject here. Job query by public number stays on the pool ([[16-track-running-job.md|16 (Track running job)]]).
- [[17-cancel-a-job.md|17 (Cancel a job)]] — needs sender-at-Post. Do not implement cancel here. After this ticket, 17 can rely on the admission seam.
- [[21-client-shows-lock-present.md|21 (Client shows lock-present)]] — Browser lock UI. Map Out of scope: [[plan/event-sourced-ops/project.md]].
- Tests may still construct agents ([[05-place-core-changes-in-existing-projects.md|05]]). Do not make `FileAgent.create` / `DbAgent.create*` the production HTTP path.

## See also

[[14-server-tracks-credentials.md|14 (Server tracks credentials)]], [[src/Server/Core/CoreChanges.fs]], [[src/Server/Core/CoreCredentials.fs]], [[src/Server/Core/CoreActorPool.fs]], [[src/Server/Core/CoreRuntime.fs]], [[CONTEXT.md]], [[plan/core-creation/reports/core-api-boundary-review.md]], [[24-clarify-core-increment-boundary.md|24 (Clarify Core increment boundary)]]

## Comments

- 2026-09-06 — Filed from the Core API boundary review (partial). Closes the remaining 14 Post-admission seam and the unpacked `CoreRuntime` seam. Does not swallow 07, 08, 17, or 21.
