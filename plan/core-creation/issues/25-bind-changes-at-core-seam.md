# 25 — Bind Changes at the Core seam

**Status:** needs-triage
**Blocked by:** none
**Estimate:** 2h
**Actual:** 15m

## Context

[[plan/core-creation/reports/improve-codebase-architecture.md]] top recommendation. [[23-close-core-object-seam.md|23 (Close Core object seam)]] is Status `done`: production posts present a live Credential; HTTP holds [[src/Server/Core/CoreRuntime.fs]] as Core. Leftover: `/ambit/changes` still unpacks Core into `changes()`, credentials, and `browserCredential`, and [[src/Server/Api.fs]] `postChange` runs `CoreAuth.post`. Parse already binds: `parseBound` uses `CoreAuth.bindHandle`. The HTTP Adapter interface is nearly the admission implementation. Leakage across the seam.

Domain: Core Changes admission. This ticket does not re-open [[07-define-core-files-contract.md|07 (Define the Core Files contract)]] or [[08-define-core-query-contract.md|08 (Define the Core Query contract)]]. It does not collapse Graph-only chunking. It does not start [[17-cancel-a-job.md|17 (Cancel a job)]] or [[21-client-shows-lock-present.md|21 (Client shows lock-present)]].

## What to build

Bind Changes inside Core. The Adapter posts through one nested interface, same pattern as `parseBound`. Do not add a Core-level `postChange` facade ([[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]).

- [ ] [[src/Server/RouteRegistration.fs]] `/ambit/changes` does not pass `core.changes ()`, `core.credentials`, and `core.browserCredential` as three arguments into `Api.postChange`. It posts through a bound Changes handle (Browser credential), nested on the Core object.
- [ ] [[src/Server/Api.fs]] `postChange` does not run `CoreAuth.post` or take a credentials set. Decode JSON and map HTTP status only. Admission stays behind the bound Changes interface.
- [ ] Tests hit that bound interface: a live Browser credential is admitted; an inactive sender is auth-refused. Callers do not unpack CoreRuntime into admission primitives.

## Related, not this ticket

- [[07-define-core-files-contract.md|07 (Define the Core Files contract)]] — `flushFileSnapshot` / `getFileRevision` on the container stay until Files is a subobject.
- [[08-define-core-query-contract.md|08 (Define the Core Query contract)]] — Poll and Load composition in `Api.getPoll` / `postLoad` stay until Query.
- Three Change enqueue paths ([[src/Server/GraphOnlyChangePost.fs]], Parse, LazyLoad) — worth exploring later; do not collapse chunking here.
- Browser dual Change POST (`applyAndPostSync`) — not this Project.

## See also

[[23-close-core-object-seam.md|23 (Close Core object seam)]], [[src/Server/Core/CoreCredentials.fs]], [[src/Server/Core/CoreRuntime.fs]], [[src/Server/Api.fs]], [[src/Server/RouteRegistration.fs]], [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]], [[plan/core-creation/reports/improve-codebase-architecture.md]], [[CONTEXT.md]]

## Time

- 2026-09-06 15m — filed from the architecture review (from chat)

## Comments

- 2026-09-06 — Filed from [[plan/core-creation/reports/improve-codebase-architecture.md]] (Strong, in-process). Does not swallow 07, 08, Graph-only chunking, or Browser SyncPlanner.
