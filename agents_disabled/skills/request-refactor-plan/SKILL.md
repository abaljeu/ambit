---
name: request-refactor-plan
description: Chart a new Feature-set Project for a refactor — interview, tiny steps, then a map. Use when the user wants to plan a refactor, a refactoring RFC, or safe incremental refactor steps.
---

This skill **charts** a new Feature-set Project under [[plan/]], like [[.agents/skills/wayfinder/SKILL.md]]. Spec work is [[.agents/skills/to-spec/SKILL.md]]. Implementation tickets are [[.agents/skills/to-tickets/SKILL.md]].

Git: [[.agents/skills/git-protocol/SKILL.md]]. Start the Project and set Stage with [[.agents/skills/project-work/SKILL.md]]. This invocation is a chart — the same Who-writes-Stage act as `/wayfinder` in [[doc/agents/project-status.md]]. Map, child tickets, blocking, and frontier: [[doc/agents/issue-tracker.md]] (Wayfinding operations). Those docs own structure. Charting is [[.agents/skills/wayfinder/SKILL.md]] Chart the map. This skill owns the refactor **steps** grain and coverage interview.

Plan grain is **steps** (same size discipline as tiny commits). Martin Fowler: make each refactoring step as small as possible, so that you can always see the program working. Each step leaves the codebase in a working state; when later implementing, a step may become a commit.

## Process

You may skip an interview step that is not necessary. Always create and chart.

1. Look in the codebase to check for test coverage of this area of the codebase. If there is insufficient test coverage, ask the user what their plans for testing are. Done: testing intent is named.
2. Break the implementation into tiny **steps**. Done: the steps are named at commit grain.
3. Create the Feature-set Project and chart it. Follow [[.agents/skills/project-work/SKILL.md]] to start. Follow [[.agents/skills/wayfinder/SKILL.md]] Chart the map. Record the **steps** grain on the map Notes. Follow [[doc/agents/issue-tracker.md]] for where those files live. Done: `project.md` exists at Stage `chart`, the map exists, and the first tickets you can specify now are filed.
