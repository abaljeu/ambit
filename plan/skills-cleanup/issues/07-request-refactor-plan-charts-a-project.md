# 07 — request-refactor-plan charts a new Project

**Status:** done
**Blocked by:** [[02-allow-agents-skill-home-in-prepare-rule.md|02 Allow the agents skill home in the prepare rule]], [[03-write-status-and-stage-lists-into-canonical-docs.md|03 Write Status and Stage lists into canonical docs]]
**Actual:** 45m

## Context

request-refactor-plan walks a human through a refactor and publishes a plan under [[plan/]]. It can look like a third spec-or-ticket skill in a line after Wayfinder and to-spec. Alan locked it as a process that creates and charts a new Feature-set Project, like Wayfinder.

## What to build

An Agent that runs request-refactor-plan creates a new Feature-set Project under `plan/` and charts it. It is not a third spec-or-ticket skill in a line. New Project structure follows [[doc/agents/issue-tracker.md]] and project-work by reference. The skill does not copy that structure.

- [x] request-refactor-plan creates and charts a new Feature-set Project under [[plan/]].
- [x] The skill points at [[doc/agents/issue-tracker.md]] and project-work for structure. It does not repeat those docs.

## See also

[[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]], [[doc/agents/issue-tracker.md]]

## Time

- 2026-09-06 45m — rewrote request-refactor-plan to chart a Feature-set Project (from chat)
