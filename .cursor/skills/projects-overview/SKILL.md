---
name: projects-overview
description: Regenerate .scratch/index.md, a stage overview of every project. Use when the user wants a projects overview or project status/stage list, or after any skill changes a project's stage.
---

# Projects overview

Follow [[docs/agents/project-status.md]] for the stage vocabulary and the `project.md` format, and [[.cursor/skills/project-work/SKILL.md]] for the branch and commit protocol.

Regenerate [[.scratch/index.md]] so every **project** — a `.scratch/<slug>/` directory — appears with its **stage**.

1. List every `.scratch/*/` directory except the `done/` archive. Completion: every live directory accounted for, including any lacking a `project.md`.
2. For each, read `Stage:` and `Summary:` from its `project.md`. If the file is missing, derive a one-line summary from the directory's contents, assign stage `charting`, and create the `project.md`.
3. Write `.scratch/index.md`: a table with one row per directory, sorted by stage in vocabulary order (`charting` … `done`) then by name, each row linking the project. Completion: row count equals directory count.
