# Core creation

Stage: active
Summary: Establish Core and Core API as the sole Server Graph writer, persistent-state coordinator, and Actor pool.
Updated: 2026-09-06
Started: 2026-09-05
Actual: 16h30m

## Map

- [[plan/core-creation/map.md|Core creation Wayfinder]] — chart the initial Graph-agent package and later Core decisions.

## Committed Decisions

- [[doc/Decisions/0003-core-is-a-container-of-subobjects.md|Core is a container of subobjects]] — provisional framing of the Core structure.

## Agent instruction

This increment: Core owns Graph write, History, agent selection, the credential set, the Actor pool, and lock-present overlay. The Adapter owns HTTP JSON, the cookie gate, and `/ambit` routes. Persist algorithms and Parse algorithms stay outside Core. Parse calls typed Graph-only Post. Actor definitions stay outside Core. The pool Interface stays in Core.

- Callers hold the Core object ([[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]). They call typed functions on its subobjects: Credential, Revision, Change, and Command types (ActorName, PublicNumber, NodeRange). They do not unpack [[src/Server/Core/CoreRuntime.fs]] into a flattened HTTP context. Cookie token strings stay in the Adapter. This increment does not pass `dataDir` into Core.
- Files stay [[plan/core-creation/issues/07-define-core-files-contract.md|07 (Define the Core Files contract)]]. General Query stays [[plan/core-creation/issues/08-define-core-query-contract.md|08 (Define the Core Query contract)]]. Job query by public number stays on the pool ([[plan/core-creation/issues/16-track-running-job.md|16 (Track running job)]]).
- This increment does not add Browser lock UI. [[plan/core-creation/issues/21-client-shows-lock-present.md|21 (Client shows lock-present)]] belongs with [[plan/event-sourced-ops/project.md]].
- Locked code plan: [[plan/core-creation/mitigations.md]].

## Implementation plan

- [[plan/core-creation/mitigations.md|Close Core object seam and increment boundary]] — locked plan for [[plan/core-creation/issues/24-clarify-core-increment-boundary.md|24 (Clarify Core increment boundary)]] then [[plan/core-creation/issues/23-close-core-object-seam.md|23 (Close Core object seam)]].
- [[plan/core-creation/initial-core-changes-implementation.md|Initial Core Changes implementation]] — implement resolved issues 03–06 and enable later delivery issue 01.

## Issues

- [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] — establish the shared Core Changes path.
- [[plan/core-creation/issues/02-core-actor-pool.md]] — establish Core-owned Actor pool machinery.
- [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md|Delete runtime mirror and remove production Persistence:Mode]] — use Database persistence when available and reject Changes when unavailable.
- [[plan/core-creation/issues/14-server-tracks-credentials.md]] — Core credential set for the Server process and one auth-refuse family.
- [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]] — launch an Actor, hold the span, and write lock-present.
- [[plan/core-creation/issues/16-track-running-job.md]] — query a registered job by public number.
- [[plan/core-creation/issues/17-cancel-a-job.md]] — cancel by NodeId without Undo.
- [[plan/core-creation/issues/18-finish-and-drop.md]] — delete-actor after any Actor stop.
- [[plan/core-creation/issues/19-database-down-and-host-stop.md]] — Database-down probe and host StopAsync drain.
- [[plan/core-creation/issues/20-client-presents-credential.md]] — live Browser presents a credential on every message.
- [[plan/core-creation/issues/21-client-shows-lock-present.md]] — Browser shows lock-present via state, Fetch, or Query.
- [[plan/core-creation/issues/22-client-cancels-a-job.md]] — user cancels a job from the UI.
- [[plan/core-creation/issues/23-close-core-object-seam.md]] — production posts present Credential; callers use the typed Core object.
- [[plan/core-creation/issues/24-clarify-core-increment-boundary.md]] — agent instruction: Core vs Adapter vs Client; no lock UI this increment.
- [[plan/core-creation/issues/25-bind-changes-at-core-seam.md]] — leftover after 23: HTTP posts through bound Changes, not unpacked CoreAuth.

## Decision tickets

- [[plan/core-creation/issues/03-define-typed-core-changes-contract.md|Define the typed Core Changes contract]]
- [[plan/core-creation/issues/04-separate-http-adapter-from-core-changes.md|Separate the HTTP Adapter from Core Changes]]
- [[plan/core-creation/issues/05-place-core-changes-in-existing-projects.md|Place Core Changes in the existing projects]]
- [[plan/core-creation/issues/06-ready-the-initial-core-changes-increment.md|Ready the initial Core Changes increment]]
- [[plan/core-creation/issues/07-define-core-files-contract.md|Define the Core Files contract]]
- [[plan/core-creation/issues/08-define-core-query-contract.md|Define the Core Query contract]]
- [[plan/core-creation/issues/09-define-core-command-launch-contract.md|Define the Core Command launch contract]]
- [[plan/core-creation/issues/10-define-actor-cancellation-and-output-admission.md|Define Actor cancellation and output admission]]
- [[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md|Define Actor finish and failure behavior]]
- [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md|Define Actor-pool shutdown behavior]]

## Reports

- [[plan/core-creation/reports/kernel-fsproj.md]] — Core boundary and module shape.
- [[plan/core-creation/reports/solid-core-module-fit.md]] — fit with the existing modules.
- [[plan/core-creation/reports/current-edit-core-reconciliation.md]] — current edit path and planned authority sequence.
- [[plan/core-creation/reports/create-project-reorganization.md]] — Project creation and ownership reorganization.
- [[plan/core-creation/reports/core-wayfinder-fact-inventory.md]] — evidence and open-choice inventory used to chart the map.
- [[plan/core-creation/reports/chart-core-wayfinder-map.md]] — map, ticket topology, and verification report.
- [[plan/core-creation/reports/plan-initial-core-changes-implementation.md]] — implementation-plan changes and verification.
- [[plan/core-creation/reports/implement-issue-01-actor-produce-path.md]] — test Actor produce path on Normal Core Changes.
- [[plan/core-creation/reports/implement-issue-14-credentials.md]] — Core credential set and one auth-refuse family.
- [[plan/core-creation/reports/commit-14-implement-15.md]] — commit issue 14; implement launch, span hold, and lock-present.
- [[plan/core-creation/reports/commit-15-what-is-16.md]] — commit issue 15; what issue 16 asks.
- [[plan/core-creation/reports/implement-issue-16.md]] — query a registered job by public number.
- [[plan/core-creation/reports/implement-issue-20.md]] — Browser presents the session cookie; missing cookie is auth-refuse.
- [[plan/core-creation/reports/core-api-boundary-review.md]] — Core API seam vs the map destination.
- [[plan/core-creation/reports/commit-16-mitigation-tickets.md]] — commit issue 16/20; file 23 and 24 from the boundary review.
- [[plan/core-creation/reports/plan-23-24-mitigations.md]] — pointer at the locked 24-then-23 plan.
- [[plan/core-creation/reports/implement-24-then-23.md]] — 24 instruction then 23 Core object seam.
- [[plan/core-creation/reports/actor-core-and-mailbox-check.md]] — Actor `CoreChanges` handle vs mailbox; writes are the two Posts.
- [[plan/core-creation/reports/commit-24-and-23.md]] — commit 24 then 23 on `dev`.
- [[plan/core-creation/reports/improve-codebase-architecture.md]] — Core hot-spot deepening candidates; top recommendation is 25.
- [[plan/core-creation/reports/grill-issue-09-launch-contract.md]] — start grill of the Core Command launch contract.
- [[plan/core-creation/reports/grill-issue-10-cancellation.md]] — start grill of Actor cancellation and output admission.
- [[plan/core-creation/reports/grill-issue-11-finish.md]] — grill of Actor finish and failure behavior.
- [[plan/core-creation/reports/grill-issue-12-shutdown.md]] — start grill of Actor-pool shutdown behavior.
