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
