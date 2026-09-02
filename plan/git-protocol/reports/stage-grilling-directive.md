# Stage: grilling is a directive

Instruction change. `Stage: grilling` on a [[plan]] `project.md` now forces the next agent that starts or advances that project to run [[.agents/skills/grilling/SKILL.md]]. Other stages stay status-only.

## Files changed

- [[doc/agents/project-status.md]] — source of truth. `grilling` is in the stage table. Setting-the-stage says it is the only stage that invokes a skill.
- [[.cursor/rules/project-stage.mdc]] — always-apply. When you start or advance and `Stage:` is `grilling`, follow the grilling skill before implement or ticket work. After grilling starts, set `charting`.
- [[.cursor/skills/project-work/SKILL.md]] — start/advance workflow. Read `Stage:` before you change it. If `grilling`, follow the grilling skill, set `charting` as soon as grilling starts, stay in the interview.
- [[.cursor/skills/projects-overview/SKILL.md]] — overview sort order includes `grilling` first.

No bridge edits. No [[plan/index.md]] regen: no live project is at `grilling`. Did not edit vendor [[.agents/skills/grilling/SKILL.md]].

## How the force works

An always-apply rule is in context on every turn. If `project.md` is `Stage: grilling` and the agent starts or advances that project, it must follow the grilling skill. It must not implement, ticket, or skip the interview. After grilling starts, stage becomes `charting` (same as `/grilling` today). An agent already working a different issue of the same project does not stop.

`charting`, `steering`, `spec`, `tickets`, `active`, `blocked`, and `done` remain status-only. They do not auto-invoke skills.

## Verify

If `project.md` said `Stage: grilling`, project-stage and project-work require grilling before implement/ticket work. If the project is `charting` or `active`, that branch does not fire.
