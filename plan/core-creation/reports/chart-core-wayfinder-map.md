# Chart Core creation Wayfinder map

Date: 2026-09-05

## Summary

Created the canonical [[plan/core-creation/map.md]] for an implementation-ready initial Core Changes increment and retained the later Core API and Actor-pool questions as decision tickets. No decision was resolved, claimed, or added to Decisions so far, and no implementation code changed.

## Files created

- [[plan/core-creation/map.md]] with exactly Destination, Notes, Decisions so far, Not yet specified, and Out of scope.
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
- [[chart-core-wayfinder-map.md]] as this transactional report.

## Files updated

- [[plan/core-creation/project.md]] now links the map, all decision tickets, the fact inventory, and this report while retaining the two implementation issues and prior reports. Stage remains charting and Updated remains 2026-09-05.
- [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md|Generalized Server Actor produce path]] now waits on [[plan/core-creation/issues/06-ready-the-initial-core-changes-increment.md|Ready the initial Core Changes increment]] and has Status needs-info. This minimal correction prevents implementation from starting while its contract, transport, placement, and acceptance decisions are open.
- [[plan/core-creation/issues/02-core-actor-pool.md|Core Actor pool]] now also waits on [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md|Define Actor-pool shutdown behavior]]. Its existing needs-info status and all implementation content remain.

## Frontier and dependency topology

The only initial frontier ticket is [[plan/core-creation/issues/03-define-typed-core-changes-contract.md|Define the typed Core Changes contract]].

The initial route continues through [[plan/core-creation/issues/04-separate-http-adapter-from-core-changes.md|Separate the HTTP Adapter from Core Changes]], then [[plan/core-creation/issues/05-place-core-changes-in-existing-projects.md|Place Core Changes in the existing projects]], then [[plan/core-creation/issues/06-ready-the-initial-core-changes-increment.md|Ready the initial Core Changes increment]]. The physical seam waits on both the typed contract and transport boundary. The readiness decision waits on all three preceding decisions and gates the existing Changes implementation issue.

After the physical seam is known, [[plan/core-creation/issues/07-define-core-files-contract.md|Define the Core Files contract]] and [[plan/core-creation/issues/08-define-core-query-contract.md|Define the Core Query contract]] can proceed independently. [[plan/core-creation/issues/09-define-core-command-launch-contract.md|Define the Core Command launch contract]] also waits on the typed Changes contract. Command then gates [[plan/core-creation/issues/10-define-actor-cancellation-and-output-admission.md|Define Actor cancellation and output admission]], which gates [[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md|Define Actor finish and failure behavior]], which gates [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md|Define Actor-pool shutdown behavior]].

## Fog and scope

The only retained fog is whether the final Core API composition needs a separate decision after the individual call and Actor-pool contracts are known. No speculative composition ticket was created.

The map points Parse Actor definition, advisory soft-lock policy and Browser UI, ACID authority migration, and incremental Upload and Load to their owning Projects or Roadmap Chapters. It states that these are exclusions from this map's initial implementation increment, not product-wide exclusions.

## Verification

- Confirmed the map has exactly the five required Wayfinder sections, has an empty Decisions so far section, and does not link or list open tickets.
- Confirmed every new ticket has a title, Type grilling, Status open, ticket-number Blocked by metadata, and only a Question section.
- Confirmed all new tickets are unclaimed and unresolved, and only the typed Core Changes contract has an empty Blocked by field.
- Confirmed [[plan/core-creation/project.md]] retains Stage charting, Updated 2026-09-05, both implementation issues, and all prior report links.
- The Project Stage and Summary did not change, so overview regeneration is content-identical. Confirmed [[plan/index.md]] contains the matching Core creation row and all 35 live Project rows; no overview edit was needed.
- No repository Markdown lint configuration or focused Markdown lint command was available. Validation used focused structure and link inspection. No tests or builds ran.
