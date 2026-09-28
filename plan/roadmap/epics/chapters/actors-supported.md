# Chapter: Actors supported

**Part of:** [[plan/roadmap/epics/robust-outliner.md]]
**Blocked by:** [[initial-core.md]].

## Context

[[plan/core-creation/project.md]] remains the Core baseline and the existing Actor pool and mailbox. Parse File remains the first Actor definition. It stays outside Core. The pool and mailbox remake that hosts Actors as Core clients stays on [[plan/actor-as-client/project.md]] (deferred 2026-09-27).

## Goal

Parse File is the first Actor definition that works through Core. It concludes through Changes and returns merge success. Parse stays outside Core. Actor hosting is remade so an Actor talks Core the same way the Browser does.

## Required for done

- [ ] [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]] — Parse File tracer; Parse stays outside Core
- [ ] [[plan/actor-as-client/project.md]] — Core pool and mailbox remake; Actor as Core client (deferred 2026-09-27: pending the reframing of Actors as functions that can take all kinds of parameters; may land around step five or six; gaps in between unknown)

## Notes

- Core pool and mailbox remake stays on [[plan/actor-as-client/project.md]] (deferred 2026-09-27; same rationale as Required). Core baseline remains [[plan/core-creation/project.md]].
- Parse Actor definition remains [[plan/parse-actor/project.md]]. The Parse File tracer remains [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]]. This Chapter does not move those homes.
- Advisory soft-lock behavior stays in [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]].

## Actor as client chart

The remake stays deferred (2026-09-27). Chart and slice one live on [draft PR #145](https://github.com/abaljeu/ambit/pull/145). Map: [Actor as client](plan/actor-as-client/map.md). Project: [Actor as client](plan/actor-as-client/project.md). Lookup: [function-shaped start](plan/actor-as-client/function-shaped-start.md). [03 — Mailbox orchestration shape](plan/actor-as-client/issues/03-mailbox-orchestration-shape.md) and [04 — Start surface](plan/actor-as-client/issues/04-start-surface.md) are locked (`done`) on the map Decisions so far.

1. [01 — Actor-as-client duplex](plan/actor-as-client/issues/01-actor-as-client-duplex.md) — Status `defined`
2. [02 — Graph handoff](plan/actor-as-client/issues/02-graph-handoff.md) — Status `defined`
3. [03 — Mailbox orchestration shape](plan/actor-as-client/issues/03-mailbox-orchestration-shape.md) — Status `done`
4. [04 — Start surface](plan/actor-as-client/issues/04-start-surface.md) — Status `done`
5. [05 — Expand-contract first aspect](plan/actor-as-client/issues/05-expand-contract-first-aspect.md) — Status `coded`; report: [Slice one: function-passing pool start](plan/actor-as-client/reports/slice-one-function-start.md)
