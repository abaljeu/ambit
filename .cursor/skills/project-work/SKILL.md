---
name: project-work
description: .scratch project files and Stage. Use before editing a project's files, when starting a .scratch effort, or when another skill touches .scratch.
---

# Project work

Stages: [[docs/agents/project-status.md]]. Git: [[.cursor/skills/git-protocol/SKILL.md]].

Each `.scratch/<slug>/` effort is a **Project**. Keep its `project.md` Stage current. Create `project.md` if the effort lacks one. Regenerating the overview: [[.cursor/skills/projects-overview/SKILL.md]].

Do not create `git.md` to record branch names. Existing `git.md` files are history.

## 1. Start

Follow [[.cursor/skills/git-protocol/SKILL.md]] for where work sits. Then write the project files.

## 2. Stage

Set `Stage:` and `Updated:` in `project.md` when the effort starts or advances. Then regenerate [[.scratch/index.md]].

## 3. Work

Edit the project's files. Specs, issues, maps, and reports live under `.scratch/<slug>/`.

## 4. Finish

Commit only the changes the user approved, as **agent-done** per [[.cursor/skills/git-protocol/SKILL.md]].
