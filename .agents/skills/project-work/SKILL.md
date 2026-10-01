---
name: project-work
description: plan project files and Stage. Use before editing a project's files, when starting a plan effort, or when another skill touches plan.
---

# Project work

Stages: [[doc/agents/project-status.md]]. Git: [[.agents/skills/git-protocol/SKILL.md]].

Each `plan/<slug>/` effort is a **Project**. Keep its `project.md` Stage current. Create `project.md` if the effort lacks one.

Do not create `git.md` to record branch names. Existing `git.md` files are history. Plan and arch text do not discuss git branches; record implementation status by ticket, section, or Point per [[.agents/rules/planning-docs.md]].

## 1. Start

Follow [[.agents/skills/git-protocol/SKILL.md]] for where work sits. Then write the project files. Done: `plan/<slug>/` exists with a `project.md`.

Anywhere CONTEXT.md is found, it asserts rules for how that subdirectory is structured.  Look for CONTEXT.md.

## 2. Stage

Read `Stage:` before you change it. Vocabulary and Who-writes-Stage: [[doc/agents/project-status.md]]. Grilling is a method, not a Stage. Do not write `Stage: grilling`. If the user invokes a grill skill, follow it; grilling does not write Stage.

Set `Stage:` and `Updated:` in `project.md` when the effort starts or advances. Done: `Stage:` and `Updated:` match the effort's current place.

## 3. Work

Edit the project's files. Placement of files under `plan/` follows [[doc/agents/issue-tracker.md]] Conventions. On issues you touch, keep `**Type:**` set ([[doc/agents/issue-tracker.md]] Ticket Type: Wayfinder `research`/`prototype`/`grilling`/`task`, or `coding` / `bug-fixing` for implement). Time, Status, Started, Finished, and project Actual: [[doc/agents/issue-tracker.md]] Time tracking and Claim / Resolve; Status values: [[doc/agents/triage-labels.md]]. Done: every file you edited sits under the Project, and every touched issue carries Type plus tracker time/Status fields per those docs.

## 4. Finish

Finish as **done**. Done: this session's Project edits are written and `Stage:` / `Updated:` are current.
