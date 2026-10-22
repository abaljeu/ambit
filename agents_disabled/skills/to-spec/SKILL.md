---
name: to-spec
description: Turn the current conversation into a front-half spec and write it as spec.md on the Project — no interview, just synthesis of what you've already discussed.
disable-model-invocation: true
---

This skill takes the current conversation context and codebase understanding and produces a front-half spec (Problem Statement, Solution, User Stories, Out of Scope). Do NOT interview the user — just synthesize what you already know. Whole-feature layout, seams, and Implementation/Testing Decisions belong to [[.agents/skills/to-arch/SKILL.md]] afterward.

## Process

1. Explore the repo to understand the current state of the codebase, if you haven't already. Use the project's domain glossary vocabulary throughout the spec, and respect any Committed Decisions in the area you're touching. Done: you can name the problem and solution in project vocabulary.

2. Write the spec using [[SPEC.md]], then publish it as `spec.md` on the Project. Number every section and every list item; give each a name per [[.agents/rules/refer-by-name.md]]. The spec path is in [[doc/agents/issue-tracker.md]]. A spec is not a ticket and has no `**Status:**`. Set `Stage: spec` and `Updated:` on `project.md` per [[doc/agents/project-status.md]]. Done: `plan/<slug>/spec.md` exists and project Stage is `spec`.

3. Stop. Hand off to [[.agents/skills/to-arch/SKILL.md]] for architecture (HITL). Do not run [[.agents/skills/to-tickets/SKILL.md]] from here. Done: no arch or tickets were written in this run.
