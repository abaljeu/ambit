---
name: project-work
description: plan project files and Stage. Use before editing a project's files, when starting a plan effort, or when another skill touches plan.
---

# Project work

Stages: [[doc/agents/project-status.md]]. Git: [[.agents/skills/git-protocol/SKILL.md]].

Each `plan/<slug>/` effort is a **Project**. Keep its `project.md` Stage current. Create `project.md` if the effort lacks one.

Do not create `git.md` to record branch names. Existing `git.md` files are history.

## 1. Start

Follow [[.agents/skills/git-protocol/SKILL.md]] for where work sits. Then write the project files.

## 2. Stage

Read `Stage:` before you change it. Vocabulary and Who-writes-Stage: [[doc/agents/project-status.md]]. Grilling is a method, not a Stage. Do not write `Stage: grilling`. If the user invokes a grill skill, follow it; grilling does not write Stage.

Set `Stage:` and `Updated:` in `project.md` when the effort starts or advances.

## 3. Work

Edit the project's files. Specs, issues, maps, and reports live under `plan/<slug>/`. On issues you touch, log spent time under `## Time` and keep `Actual:` in sync; set optional `Estimate:` when sizing. On `project.md`, set `Started:` on the discuss→build handoff (or first build commit), set `Finished:` when Stage becomes `done`, and keep project `Actual:` as the sum of issue times — backfill from chat and commits when gaps remain (see [[doc/agents/issue-tracker.md]] Time tracking).

## 4. Finish

Commit only the changes the user approved, as **agent-done** per [[.agents/skills/git-protocol/SKILL.md]].
