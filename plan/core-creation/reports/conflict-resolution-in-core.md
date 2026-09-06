# Conflict resolution in Core

When an Actor posts a Change, amendment runs **inside the Graph-agent package**, behind the typed Core Changes Interface. The Actor plans Ops. It then calls Core Changes. The selected FileAgent or DbAgent mailbox runs `applyBatch`, which calls Shared [[src/Shared/ChangeAmendment.fs]]. The HTTP Adapter and Actor code do not amend. This is not a new Core seam. The map already requires it.

## Design

The live Interface is [[src/Server/Core/CoreChanges.fs]] (`postChange` and `postGraphOnlyChange`). The map names that Interface GraphAgentHandle. Production callers receive it from [[src/Server/Core/CoreRuntime.fs]].

Placement:

- **Actor code** plans Changes only. Parse in [[src/Server/Api.fs]] (`postParseFile`) and chunked Graph-only posts in [[src/Server/GraphOnlyChangePost.fs]] wrap Ops into a Change list and call Core. They do not call `ChangeAmendment`.
- **HTTP Adapter** ([[src/Server/Api.fs]] `postChange`) decodes JSON and calls Normal Core Changes. In-process Actors do not go through this Adapter ([[../issues/04-separate-http-adapter-from-core-changes.md|HTTP Adapter boundary]]).
- **Core Changes Interface** is the Seam. Normal `postChange` is for Browser POST and future Server Actors. Graph-only `postGraphOnlyChange` is reserved for Parse ([[../issues/03-define-typed-core-changes-contract.md|Typed Core Changes contract]], [[../issues/06-ready-the-initial-core-changes-increment.md|Ready the initial Core Changes increment]]).
- **Graph-agent package implementation** (FileAgent / DbAgent mailboxes) owns sequencing. Both mailbox messages share `applyBatch` → `ChangeAmendment.applyChange`. Graph-only only skips document validation and document persist. Amendment is the same.
- **Shared apply** stays in [[src/Shared/History.fs]] and [[src/Shared/ChangeAmendment.fs]]. `History.applyChange` is the CAS apply. `ChangeAmendment` rewrites recoverable text, name, classes, and child-list mismatches, then reapplies. The Browser can use the same Module. Do not move these files ([[../issues/05-place-core-changes-in-existing-projects.md|Graph-agent package placement]]).

A future Server Actor on [[../issues/01-generalized-server-actor-produce-path.md|Generalized Server Actor produce path]] must call Normal `postChange`. Arrival at that operation is enough for conflict resolution. That issue does not re-place amendment.

## Today on produce

Conflict detection is CAS fail from `Op.apply` (`"old … does not match"`). Amendment is [[src/Shared/ChangeAmendment.fs]] `applyChange`. Callers: [[src/Server/FileAgent.fs]] and [[src/Server/DbAgent.fs]] `applyBatch`. Evidence: [[plan/event-sourced-ops/reports/conflict-detection-location.md]]. Ticket [[plan/event-sourced-ops/issues/03-server-amends-recoverable-field-collisions.md|Server amends recoverable field collisions]] is Status done. Amendment order is the accepted sequence in [[plan/event-sourced-ops/details/merge-invariant.md]]: common prior, then other accepted Changes in full, then the newest Change amended.

## Map decisions (gists only)

From [[../map.md]]:

- [[../issues/03-define-typed-core-changes-contract.md|Typed Core Changes contract]] — both operations preserve current amendment.
- [[../issues/04-separate-http-adapter-from-core-changes.md|HTTP Adapter boundary]] — Parse calls typed Graph-only Post Change directly after it produces Changes.
- [[../issues/05-place-core-changes-in-existing-projects.md|Graph-agent package placement]] — extract the current Graph-agent package; keep Shared `ChangeAmendment`; agents keep mailbox apply.
- [[../issues/06-ready-the-initial-core-changes-increment.md|Ready the initial Core Changes increment]] — Normal serves Browser POST and future Server Actors; Graph-only is reserved for Parse.

Map Notes: Shared keeps Browser-compatible apply. Server owns the typed produce path. Preserve current produce behavior during extraction. Every runtime Change must reach the authoritative Server Graph and History through Core Changes.

Roadmap framing ([[plan/roadmap/reports/hub-epic-framing.md]]): an Actor produces Changes; the Server sequences and amends. That is effort scope, not a new Committed Decision.

## Gap?

**No.** The map already implies the answer: keep the current produce path behind GraphAgentHandle / CoreChanges, including Shared amendment. Do not add a ticket. Do not put this in Not yet specified.

[[../issues/01-generalized-server-actor-produce-path.md|Generalized Server Actor produce path]] is implementation of a production Normal caller. It is not a placement decision for conflict resolution.
