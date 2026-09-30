# Actor as client

Stage: build
Parked: deferred 2026-09-27
Summary: Remake Server Actor hosting and mailbox orchestration so an Actor is a Core client like the Browser: it owns internal behavior, talks Core for Graph, Events, and Changes, and starts from a full Graph plus start ids under thin mailbox routing.
Updated: 2026-09-28
Started: 2026-09-27
Actual: 2h

**Part of:** [[plan/roadmap/epics/chapters/actors-supported.md]]

## Notes

- 2026-09-28 — Alan Server description on [[plan/architecture/server-core.md]] confirms Actors as privilege-less external functions that only post to the mailbox. Core and the mailbox own create and destroy, and Graph, file, and git mutations. This Feature-set stays concept-only. No new implement tickets from this lock.
- 2026-09-27 — Deferred pending the reframing of Actors as functions that can take all kinds of parameters. It may land around step five or six; the gaps in between are unknown. Keep the project, map, tickets, and chapter link. Slice one on [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md) stays `coded` as historical progress; further slices wait.
- 2026-09-27 — [03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md) locked from Alan voice: mailbox is a dispatcher (stamp EventId and route, do not wait); EventId is the watermark; queues are open-access; each entity answers completion on that Event. Feature-set remains deferred.
- 2026-09-27 — [04 — Start surface](issues/04-start-surface.md) locked from Alan voice: curried function posted to the mailbox; CoreActorPool keeps cancel via focus ID and name; named registry stays fog; ActorStart drops `graphIds` and records focus ID and name. Feature-set remains deferred.
- 2026-09-27 — Charted from Alan chat. Existing Core pool and mailbox stay [[plan/core-creation/project.md]]. First Actor definition / Parse stays outside Core on [[plan/parse-thread/project.md]]. Parse File tracer stays [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]].
- Map: [[map.md]]. Function-shaped start lookup: [function-shaped start](function-shaped-start.md).
- First grillset: [01 — Actor-as-client duplex](issues/01-actor-as-client-duplex.md), [02 — Graph handoff](issues/02-graph-handoff.md), [03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md), [04 — Start surface](issues/04-start-surface.md).
- 2026-09-27 — Added [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md): sequencing of remake aspects under expand-and-contract. Aims stay provisional.
- 2026-09-27 — [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md) revised: first slice is function-passing pool setup, not Graph resolver.
- 2026-09-27 — [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md) step-two working hypothesis only: flexibility across Actor types after function-passing ships. Not a Decision.
- 2026-09-27 — Slice one coded: function-passing pool start expand-alongside. Report: [Slice one: function-passing pool start](reports/slice-one-function-start.md).
