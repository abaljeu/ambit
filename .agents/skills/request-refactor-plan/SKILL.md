---
name: request-refactor-plan
description: Chart a new Feature-set Project for a refactor — interview, tiny steps, then a map. Use when the user wants to plan a refactor, a refactoring RFC, or safe incremental refactor steps.
---

This skill **charts** a new Feature-set Project under [[plan/]], like [[.agents/skills/wayfinder/SKILL.md]]. Spec work is [[.agents/skills/to-spec/SKILL.md]]. Implementation tickets are [[.agents/skills/to-tickets/SKILL.md]] and [[.agents/skills/to-feature-tickets/SKILL.md]].

Git: [[.agents/skills/git-protocol/SKILL.md]]. Start the Project and set Stage with [[.agents/skills/project-work/SKILL.md]]. This invocation is a chart — the same Who-writes-Stage act as `/wayfinder` in [[doc/agents/project-status.md]]. Map, child tickets, blocking, and frontier: [[doc/agents/issue-tracker.md]] (Wayfinding operations). Those docs own structure. This skill owns the refactor interview and the **steps** grain.

Plan grain is **steps** (same size discipline as tiny commits). Martin Fowler: make each refactoring step as small as possible, so that you can always see the program working. Each step leaves the codebase in a working state; when later implementing, a step may become a commit.

## Process

You may skip an interview step that is not necessary. Always create and chart.

1. Ask the user for a long, detailed description of the problem they want to solve and any potential ideas for solutions. Done when the destination of the refactor is named.

2. Explore the repo to verify their assertions and understand the current state of the codebase. Done when you can confirm or correct those assertions.

3. Ask whether they have considered other options, and present other options to them. Done when the user has seen the options and chosen a direction.

4. Interview the user about the implementation. Be extremely detailed and thorough. Default grill: [[.agents/skills/grill-me/SKILL.md]]. Done when the implementation shape is sharp enough to chart.

5. Hammer out the exact scope of the implementation. Work out what you plan to change and what you plan not to change. Done when in-scope and out-of-scope are named.

6. Look in the codebase to check for test coverage of this area of the codebase. If there is insufficient test coverage, ask the user what their plans for testing are. Done when testing intent is named.

7. Break the implementation into tiny **steps**. Done when the first chartable questions and the remaining fog are visible.

8. Create the Feature-set Project and chart it. Follow [[.agents/skills/project-work/SKILL.md]] to start. Follow [[.agents/skills/wayfinder/SKILL.md]] Chart the map for how to write the map and tickets (this session's interview is the destination grill). Record the **steps** grain on the map Notes. Follow [[doc/agents/issue-tracker.md]] for where those files live. Done when project.md exists at Stage `chart`, the map exists, and the first tickets you can specify now are filed.
