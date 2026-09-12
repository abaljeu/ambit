# Core creation

Stage: build
Summary: Establish Core and Core API as the sole Server Graph writer, persistent-state coordinator, and Actor pool.
Updated: 2026-09-12
Started: 2026-09-05
Actual: 18h25m

## Map

- [[plan/core-creation/map.md]] — chart the initial Graph-agent package and later Core decisions.

## Committed Decisions

- [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]] — provisional framing of the Core structure.
- [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]] — cancel/finish are fast queue messages; slow work is an Actor.

## Agent instruction

This increment: Core owns the authoritative Graph, Authority validation, and the Actor pool. The Adapter owns HTTP JSON and Browser transport. Persist algorithms and Parse algorithms stay outside Core. Parse retains its typed Graph-only operation; Actor output uses normal Core Change. Actor definitions stay outside Core.

- Callers hold the Core object ([[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]). Browser HTTP is an adapter. Callers do not unpack [[src/Server/Core/CoreRuntime.fs]] into a flattened HTTP context.
- Dispatch is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. Boundaries, launch membership, Event sequence, universal response, and mailbox lifecycle are [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]].
- Files stay [[plan/core-creation/issues/07-define-core-files-contract.md]]. General Query stays [[plan/core-creation/issues/08-define-core-query-contract.md]]. Live Actor query stays [[plan/core-creation/issues/16-track-running-job.md]].
- This increment does not add Browser lifecycle UI. [[plan/core-creation/issues/21-client-shows-lock-present.md]] belongs with [[plan/event-sourced-ops/project.md]].
- Locked code plan: [[plan/core-creation/mitigations.md]].

## Implementation plan

- [[plan/core-creation/issues/Implementation Planning and Record.md]]

## Issues

- [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] — establish the shared Core Changes path.
- [[plan/core-creation/issues/02-core-actor-pool.md]] — establish Core-owned Actor pool machinery. Status `ready-for-agent` after rewind named the mailbox/TaskPool shape.
- [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]] — use Database persistence when available and reject Changes when unavailable.
- [[plan/core-creation/issues/14-server-tracks-credentials.md]] — public/secret Authority admission and durable readable Authority; Status `blocked` by the Actor pool baseline.
- [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]] — historical span delivery superseded by typed launch and ActorStarted; Status `blocked`.
- [[plan/core-creation/issues/16-track-running-job.md]] — live public-identity query plus durable lifecycle Events; Status `blocked`.
- [[plan/core-creation/issues/17-cancel-a-job.md]] — terminal Cancelled by Focus NodeId without Undo; Status `blocked`.
- [[plan/core-creation/issues/18-finish-and-drop.md]] — durable ActorFinished, synchronous registry removal, non-blocking termination, and Interrupted restart reconciliation; Status `blocked`.
- [[plan/core-creation/issues/19-database-down-and-host-stop.md]] — Database-down state and mutating Post/launch probe; blocked by [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]].
- [[plan/core-creation/issues/20-client-presents-credential.md]] — live Browser presents a credential on every message.
- [[plan/core-creation/issues/21-client-shows-lock-present.md]] — historical name; any Browser running indicator projects lifecycle Events.
- [[plan/core-creation/issues/22-client-cancels-a-job.md]] — user cancels a job from the UI.
- [[plan/core-creation/issues/23-close-core-object-seam.md]] — production posts present Credential; callers use the typed Core object.
- [[plan/core-creation/issues/24-clarify-core-increment-boundary.md]] — agent instruction: Core vs Adapter vs Client; no lock UI this increment.
- [[plan/core-creation/issues/25-bind-changes-at-core-seam.md]] — leftover after 23: HTTP posts through bound Changes, not unpacked CoreAuth. Status `done`.
- [[plan/core-creation/issues/26-failed-actor-stop-still-drops.md]] — failed Actor stop must still enqueue delete-actor. Status `cancelled` (rewind/redo, not a wrap patch).
- [[plan/core-creation/issues/27-prove-core-actor-lifecycle-with-testactor.md]] — prove the public Core lifecycle and universal response without an Agent transport.
- [[plan/core-creation/issues/28-drain-actor-lifecycle-on-host-stop.md]] — later host-stop terminal drain and restart reconciliation, separate from Database availability.
- [[plan/core-creation/issues/29-prove-testactor-hello.md]] — current implement cut: prove TestActor hello through the public Core path.

## Decision tickets

- [[plan/core-creation/issues/03-define-typed-core-changes-contract.md]]
- [[plan/core-creation/issues/04-separate-http-adapter-from-core-changes.md]]
- [[plan/core-creation/issues/05-place-core-changes-in-existing-projects.md]]
- [[plan/core-creation/issues/06-ready-the-initial-core-changes-increment.md]]
- [[plan/core-creation/issues/07-define-core-files-contract.md]]
- [[plan/core-creation/issues/08-define-core-query-contract.md]]
- [[plan/core-creation/issues/09-define-core-command-launch-contract.md]]
- [[plan/core-creation/issues/10-define-actor-cancellation-and-output-admission.md]]
- [[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]]
- [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]]

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
- [[plan/core-creation/reports/commit-14-implement-15.md]] — commit [[plan/core-creation/issues/14-server-tracks-credentials.md]]; implement historical [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]].
- [[plan/core-creation/reports/commit-15-what-is-16.md]] — commit historical [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]]; describe [[plan/core-creation/issues/16-track-running-job.md]].
- [[plan/core-creation/reports/implement-issue-16.md]] — query a registered job by public number.
- [[plan/core-creation/reports/implement-issue-20.md]] — Browser presents the session cookie; missing cookie is auth-refuse.
- [[plan/core-creation/reports/core-api-boundary-review.md]] — Core API seam vs the map destination.
- [[plan/core-creation/reports/commit-16-mitigation-tickets.md]] — commit [[plan/core-creation/issues/16-track-running-job.md]] and [[plan/core-creation/issues/20-client-presents-credential.md]]; file [[plan/core-creation/issues/23-close-core-object-seam.md]] and [[plan/core-creation/issues/24-clarify-core-increment-boundary.md]] from the boundary review.
- [[plan/core-creation/reports/plan-23-24-mitigations.md]] — pointer at the locked 24-then-23 plan.
- [[plan/core-creation/reports/implement-24-then-23.md]] — 24 instruction then 23 Core object seam.
- [[plan/core-creation/reports/actor-core-and-mailbox-check.md]] — Actor `CoreChanges` handle vs mailbox; writes are the two Posts.
- [[plan/core-creation/reports/actor-pool-rewind-review.md]] — sets 1–3 review: one mailbox, pool is TaskPool; product rewound; rebuild.
- [[plan/core-creation/reports/set3-review-handoff.md]] — set 3 review closed.
- [[plan/core-creation/reports/commit-24-and-23.md]] — commit 24 then 23 on `dev`.
- [[plan/core-creation/reports/improve-codebase-architecture.md]] — Core hot-spot deepening candidates; top recommendation is 25.
- [[plan/core-creation/reports/implement-issue-25.md]] — bind Browser Changes on Core; Adapter decode and status only.
- [[plan/core-creation/reports/grill-issue-09-launch-contract.md]] — start grill of the Core Command launch contract.
- [[plan/core-creation/reports/grill-issue-10-cancellation.md]] — start grill of Actor cancellation and output admission.
- [[plan/core-creation/reports/grill-issue-11-finish.md]] — grill of Actor finish and failure behavior.
- [[plan/core-creation/reports/grill-issue-12-shutdown.md]] — start grill of Actor-pool shutdown behavior.
- [[plan/core-creation/reports/implement-issue-29-testactor-hello.md]] — public Core TestActor hello on the FileAgent apply mailbox.
- [[plan/core-creation/reports/align-29-referenced-completed-details.md]] — completed-detail `[x]` pass on files referenced by 29.
- [[plan/core-creation/reports/file-db-agent-mailbox-twins.md]] — FileAgent and DbAgent persist twins plus CoreActorMailbox module.

## Comments

- 2026-09-11 — Added [[plan/core-creation/issues/29-prove-testactor-hello.md]].
- 2026-09-11 — One-home DRY of Phase 2 issues and locked 06/07. Command text `?test hello` is owned by [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]].
- 2026-09-12 — Reviews of the TestActor hello increment failed. Code was stashed. Yesterday's issue checkboxes and implementation logs for that increment were cleared.
