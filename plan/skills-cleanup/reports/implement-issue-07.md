# Implement issue 07 — request-refactor-plan charts a Project

Rewrote [[.agents/skills/request-refactor-plan/SKILL.md]] so an Agent that runs it creates and charts a new Feature-set Project under [[plan/]], like Wayfinder. It is not a third spec-or-ticket skill in a line. Structure stays in [[doc/agents/issue-tracker.md]] and [[.agents/skills/project-work/SKILL.md]]. The spec-like publish template is gone. Product F# was not touched. Sibling-owned files were not edited.

## What changed

- Skill framing: **charts** a Feature-set Project. Spec work points at [[.agents/skills/to-spec/SKILL.md]]. Implementation tickets point at [[.agents/skills/to-tickets/SKILL.md]] and [[.agents/skills/to-feature-tickets/SKILL.md]].
- Start and Stage: [[.agents/skills/project-work/SKILL.md]]. This invocation is a chart — same Who-writes-Stage act as `/wayfinder` in [[doc/agents/project-status.md]].
- Map, child tickets, blocking, frontier: [[doc/agents/issue-tracker.md]] (Wayfinding operations). No copied project.md, map body, or ticket template.
- Charting process: [[.agents/skills/wayfinder/SKILL.md]] Chart the map. The interview is the destination grill. Default grill is [[.agents/skills/grill-me/SKILL.md]].
- Kept the refactor interview and the **steps** grain (Fowler). Steps go on map Notes. Step 8 always creates and charts.
- Ticket [[plan/skills-cleanup/issues/07-request-refactor-plan-charts-a-project.md]] is `**Status:** done`.

## How verified

- Skill contains `charts` a Feature-set Project and points at [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/project-work/SKILL.md]], and [[doc/agents/issue-tracker.md]].
- Skill has no `refactor-plan-template`, no Problem Statement / Decision Document publish block, and no spec.md / `ready-for-agent` publish path.
- `git diff --name-only` for this commit is only the skill, this report, and issue 07.

## Leftover risks

- [[doc/agents/project-status.md]] and [[.cursor/rules/project-stage.mdc]] Who-writes-Stage still list only `/wayfinder` → `chart`. This skill now charts too. Out of scope for this ticket.
- [[.cursor/rules/gambol.mdc]] does not index request-refactor-plan. Ask-Matt still sequences wayfinder→to-spec→tickets. Ticket 12 owns those files.
- [[.agents/skills/wayfinder/SKILL.md]] still copies GitHub-shaped ops and `research/<name>` places. Ticket 05. Until that lands, Chart the map may pull those; this skill already says the tracker owns file layout.
- [[.agents/skills/request-refactor-plan/agents/openai.yaml]] still says "Plan a safe incremental refactor" and does not say chart a Project. Not in this ticket's file list.
- [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]] still names [[.cursor/skills/project-work/SKILL.md]]. History lock; live path is [[.agents/skills/project-work/SKILL.md]].
- [[plan/skills-cleanup/project.md]] Actual / Time were not refreshed. This worker does not own that file.
