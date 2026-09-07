# Retire plan/index.md updates

Agents no longer regenerate or rewrite [[plan/index.md]] after Stage changes. The file may stay on disk as a leftover snapshot. This pass does not invent a replacement overview.

## What changed

- [[.agents/skills/project-work/SKILL.md]] keeps Stage on `project.md` and no longer points at a regenerate job.
- [[.agents/skills/projects-overview/SKILL.md]] is retired (`disable-model-invocation: true`). It is not a live regenerate-overview job.
- [[.agents/skills/to-archive/SKILL.md]] moves a `done` project into [[plan/done/]] and does not rewrite the snapshot.
- [[.agents/rules/project-stage.md]] sets Stage on `project.md` and leaves the snapshot on disk. The Cursor stub [[.cursor/rules/project-stage.mdc]] is still an Obey pointer.
- [[.agents/rules/gambol.md]] no longer lists projects-overview as a required after-Stage adapter. Ask-Matt [[.agents/skills/ask-matt/SKILL.md]] matches that file in name and sequence. The Cursor stub [[.cursor/rules/gambol.mdc]] is still an Obey pointer.
- [[doc/agents/project-status.md]] records Stage on `project.md`. It names [[plan/index.md]] as leftover, not a table to regenerate.

[[plan/index.md]] was not rewritten.

## How verified

- Search in [[.agents/]] and [[.cursor/rules/]] for `regenerate` plus `plan/index.md`: remaining hits are leftover-snapshot wording, not a write job.
- [[doc/agents/project-status.md]] no longer names [[.agents/skills/projects-overview/SKILL.md]] as a Stage follow-up.
- Job lists in [[.agents/rules/gambol.md]] and [[.agents/skills/ask-matt/SKILL.md]] still match in name and sequence.

## Leftover flaws

- [[plan/index.md]] stays on disk and can disagree with live `project.md` Stage lines. Intentional this pass.
- [[plan/roadmap/map.md]] still treats [[plan/index.md]] as a stage table and as discovery after the work-board retirement. Not a skill or rule; not edited.
- Historical `plan/**/reports/` still describe regenerating the index. Not rewritten.
- [[.agents/rules/core-agent-behavior.md]] still tells subagents to run `./status.sh` at startup. That script is gone. Git protocol uses [[scripts/gitstatus.sh]]. Unrelated; not repaired.
- [[plan/skills-cleanup/project.md]] Later implementation still names older catch-up (including the old project-stage grilling copy). Unrelated; not repaired.
