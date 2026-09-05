# Chapter: Actors supported

**Part of:** [[plan/roadmap/epics/robust-outliner.md]]
**Blocked by:** [[initial-core.md]].

## Context

[[plan/core-creation/project.md]] establishes Core and its Actor pool. Parse File remains an ESO Actor definition, not Core pool machinery.

## Goal

Parse File is the first Actor definition that works through Core. It concludes through Changes and returns merge success. Parse stays outside Core.

## Required for done

- [ ] [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]]

## Notes

This Chapter owns the first Actor definition only. Core implementation detail stays in [[plan/core-creation/project.md]]. Advisory soft-lock behavior stays in [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]].
