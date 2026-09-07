## Project stage upkeep

Whenever any skill starts or advances a **project** (a `plan/<slug>/` effort), set that project's `Stage:` on its `project.md` per [[doc/agents/project-status.md]]. Create `project.md` if the effort lacks one.

Starting or advancing a project follows [[.agents/skills/project-work/SKILL.md]]. Git: [[.agents/skills/git-protocol/SKILL.md]].

Grilling is a method, not a Stage. Do not write `Stage: grilling` on a Project. Do not write Status or Stage `grilling` on a ticket. If the user invokes a grill skill, follow it. Grilling does not write Stage; `/wayfinder` writes `chart`. An agent already working a different issue of the same Project does not stop.

Skill-to-stage transitions:

- `/wayfinder` → `chart`
- `/to-spec` → `spec`
- `/to-tickets`, `/to-feature-tickets` → `slice`
- first implement → `build`
- delivered → `done`
- abandon → `dead`
