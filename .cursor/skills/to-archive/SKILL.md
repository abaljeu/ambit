---
name: to-archive
description: Archive a completed project by moving a done .scratch project into .scratch/done/ and dropping it from the overview. Use when a project's stage is done, or the user asks to /to-archive a project.
---

# Archive a project

Follow [[docs/agents/project-status.md]].

Archive a completed **project** — a `.scratch/<slug>/` effort at stage `done` — into `.scratch/done/<slug>/`, then drop it from the overview.

0. Follow [[.cursor/skills/project-work/SKILL.md]] — be on a clean project branch before moving files.
1. Confirm the target's `project.md` reads `Stage: done`. Any other stage → stop and report; only `done` projects archive.
2. Move the whole `.scratch/<slug>/` directory to `.scratch/done/<slug>/` with `git mv` (preserving history), creating `.scratch/done/` if needed. Completion: the slug no longer exists at `.scratch/<slug>/` and its files are intact under `.scratch/done/<slug>/`.
3. Regenerate [[.scratch/index.md]] with [[.cursor/skills/projects-overview/SKILL.md]]. Completion: the archived project no longer appears.
4. Commit and offer to merge back per [[.cursor/skills/project-work/SKILL.md]].
