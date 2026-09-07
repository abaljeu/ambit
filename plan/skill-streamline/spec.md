# Spec — Skill streamlining

## What it gives you

An Agent that follows live skills and rules no longer hits retired jobs, dead paths, or a second copy of writing/design theory. Stage and Status value lists live only in their canonical docs; other files point.

## What it avoids for now

- Rewriting every `SKILL.md` against writing-for-agents
- Extracting the shared to-tickets / to-feature-tickets publish core
- Deleting foreign-stack skills unless they auto-invoke
- Rewriting [[plan/skills-cleanup/project.md]] Locked / historical reports
- Product F#

## Locked winners

- Writing theory: [[.agents/skills/writing-for-agents/SKILL.md]] stays. Fold unique glossary terms from writing-great-skills, then delete that skill.
- Design-it-twice: [[.agents/skills/codebase-design/DESIGN-IT-TWICE.md]] stays under [[.agents/skills/codebase-design/SKILL.md]]. Fold unique steps from design-an-interface, add the trigger to the codebase-design description, then delete the extra skill.
- Canonical lists: Stage in [[doc/agents/project-status.md]], Status in [[doc/agents/triage-labels.md]]. Other live files point; they do not restate the pipe lists.

## Build order

1. Leftover junk — retired projects-overview, HTML-REPORT, `./status.sh`, `doc/plan`, code-review standards sources, ADR-FORMAT → Committed Decision.
2. Writing pair collapse.
3. Design-it-twice pair collapse.
4. List pointers in [[CONTEXT.md]] and [[.agents/rules/project-stage.md]].

## Testing Decisions

Repo search: no live instruction names `projects-overview` as a job, `./status.sh`, `doc/plan`, `writing-great-skills`, or `design-an-interface`. [[CONTEXT.md]] and [[.agents/rules/project-stage.md]] do not paste Stage/Status enums. Cursor stubs in `.cursor/rules/` stay Obey pointers; edit canonical `.agents/` text.
