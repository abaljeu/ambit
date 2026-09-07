---
name: to-archive
description: Archive a completed project by moving a done plan project into plan/done/. Use when a project's stage is done, or the user asks to /to-archive a project.
---

# Archive a project

Follow [[doc/agents/project-status.md]].

Archive a completed **project** — a `plan/<slug>/` effort at stage `done` — into `plan/done/<slug>/`.

0. Follow [[.agents/skills/project-work/SKILL.md]] for `plan` files. Git: [[.agents/skills/git-protocol/SKILL.md]].
1. Confirm the target's `project.md` reads `Stage: done`. Any other stage → stop and report; only `done` projects archive.
2. Move the whole `plan/<slug>/` directory to `plan/done/<slug>/` with `git mv` (preserving history), creating `plan/done/` if needed. Completion: the slug no longer exists at `plan/<slug>/` and its files are intact under `plan/done/<slug>/`.
3. Finish as **agent-done** per [[.agents/skills/git-protocol/SKILL.md]].
