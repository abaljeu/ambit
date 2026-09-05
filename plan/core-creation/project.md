# Core creation

Stage: active
Summary: Establish Core and Core API as the sole Server Graph writer, persistent-state coordinator, and Actor pool.
Updated: 2026-09-05
Started: 2026-09-05
Actual: 4h30m

## Map

- [[plan/core-creation/map.md|Core creation Wayfinder]] — chart the initial Graph-agent package and later Core decisions.

## Committed Decisions

- [[doc/Decisions/0003-core-is-a-container-of-subobjects.md|Core is a container of subobjects]] — provisional framing of the Core structure.

## Implementation plan

- [[plan/core-creation/initial-core-changes-implementation.md|Initial Core Changes implementation]] — implement resolved issues 03–06 and enable later delivery issue 01.

## Issues

- [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] — establish the shared Core Changes path.
- [[plan/core-creation/issues/02-core-actor-pool.md]] — establish Core-owned Actor pool machinery.
- [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md|Delete runtime mirror and remove production Persistence:Mode]] — use Database persistence when available and reject Changes when unavailable.

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
