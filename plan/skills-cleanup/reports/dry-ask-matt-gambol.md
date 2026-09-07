# DRY Ask-Matt and gambol

Ticket 12 made [[.agents/skills/ask-matt/SKILL.md]] and [[.agents/rules/gambol.md]] agree by pasting one job catalog twice. This pass keeps both routers and one catalog.

## What changed

- The job catalog (same jobs, skill names, sequence, including Gambol adapters) lives only in [[.agents/rules/gambol.md]].
- Ask-Matt stays the human advisor. Default grill is `/grill-me`. It points at gambol for the job index. It does not paste the catalog.
- [[.cursor/rules/gambol.mdc]] stays a thin Obey pointer.
- [[.agents/skills/ask-matt/PHASE-BOUNDARIES.md]] is not a third catalog. It is the phase-boundary tree. Ask-Matt already points at it.
- Both routers dropped every leftover overview-file reference. None were added. The retired [[.agents/skills/projects-overview/SKILL.md]] catalog line no longer names that file.
- Neither router names setup-matt, update-matt-skills, or `/triage`.
- Locked destination on [[plan/skills-cleanup/project.md]] now says one catalog in gambol, Ask-Matt points at it.

## How verified

- Ask-Matt has no `[[.agents/skills/.../SKILL.md]]` catalog lines. gambol still has the full index, including Gambol adapters.
- Search of the two routers finds no leftover overview path, setup-matt, or update-matt-skills. `triage-labels.md` remains the Status list pointer on gambol only.

## Leftover flaws (not repaired)

- [[plan/skills-cleanup/reports/lock-ask-matt-gambol-agree.md]], [[plan/skills-cleanup/spec.md]], ticket [[plan/skills-cleanup/issues/12-ask-matt-and-gambol-mdc-agree.md]], and [[plan/skills-cleanup/reports/implement-issue-12.md]] still describe agreement as two pasted lists and still name gambol.mdc as the primary file.
- [[.agents/skills/projects-overview/SKILL.md]], [[.agents/rules/project-stage.md]], [[.agents/skills/project-work/SKILL.md]], and [[doc/agents/project-status.md]] still named a leftover overview file. Out of this edit set.
- [[.agents/skills/triage/]] still has files on disk. Ticket 11 leftover.
- ADR wording leftovers in other skills. Ticket 09 leftover.
- [[.cursor/rules/core-agent-behavior.mdc]] still points at deleted testing-workflow. Ticket 10 leftover.
- Skills not in the live index stay unlisted on purpose.
