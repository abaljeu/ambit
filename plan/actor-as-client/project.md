# Actor as client

Stage: chart
Summary: Remake Server Actor hosting and mailbox orchestration so an Actor is a Core client like the Browser: it owns internal behavior, talks Core for Graph, Events, and Changes, and starts from a full Graph plus start ids under thin mailbox routing.
Updated: 2026-09-27

**Part of:** [[plan/roadmap/epics/chapters/actors-supported.md]]

## Notes

- 2026-09-27 — Charted from Alan chat. Existing Core pool and mailbox stay [[plan/core-creation/project.md]]. First Actor definition / Parse stays outside Core on [[plan/parse-actor/project.md]]. Parse File tracer stays [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]].
- Map: [[map.md]].
- First grillset: [01 — Actor-as-client duplex](issues/01-actor-as-client-duplex.md), [02 — Graph handoff](issues/02-graph-handoff.md), [03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md), [04 — Start surface](issues/04-start-surface.md).
