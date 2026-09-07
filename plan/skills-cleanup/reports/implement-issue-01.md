# Implement issue 01 — skill home move

Moved sixteen repo-shared workflow skill directories from [[.cursor/skills/]] to [[.agents/skills/]]. [[.cursor/skills/]] holds no SKILL.md. Live indexes name the new home. The junk copy [[.agents/skills/to-tickets - Copy/]] is gone (it was already absent on disk when the move ran; confirmed after). Product F# was not touched.

## What changed

- git mv of: add-shared-test, co-edit-format-plan, git-master, git-protocol, git-share, implement-fsharp-feature, investigate-fable-client, maintain-doc-currency, plan-roadmap-change, prepare-agent-instruction-change, project-work, projects-overview, scratch-script, to-archive, update-matt-skills (including scripts), write-simple-parser.
- Named wikilinks `[[.cursor/skills/<skill>/` retargeted to `[[.agents/skills/<skill>/` in live skills, [[.cursor/rules/]], [[CONTEXT.md]], [[doc/agents/]], [[doc/Decisions/0001-agent-git-hygiene.md]], [[doc/Decisions/0002-git-protocol.md]], [[scripts/commit.sh]], [[plan/skills-cleanup/project.md]], [[plan/skills-cleanup/spec.md]] (named skill files only), and [[plan/skills-cleanup/reports/skill-flaws-found-while-ticketing.md]].
- [[.cursor/rules/gambol.mdc]] workflow index heading now names [[.agents/skills/]]. Ticket-skill catalog was not merged (later ticket).
- update-matt-skills self-paths now name [[.agents/skills/update-matt-skills/]]. The skill remains; ticket 04 deletes it.
- [[.agents/skills/prepare-agent-instruction-change/SKILL.md]] still forbids storing repo-shared skills in [[.agents/skills/]] (ticket 02).
- Chart-time lock reports under [[plan/skills-cleanup/reports/]] were not rewritten.
- Project Stage is `build`.

## How verified

- No `SKILL.md` under [[.cursor/skills/]].
- No named live wikilink `[[.cursor/skills/<skill>/` in rules, [[CONTEXT.md]], [[doc/agents/]], [[doc/Decisions/]], live skills (except the prepare forbid, which is backticks not a skill-file wikilink), [[scripts/commit.sh]], project.md, spec.md named files, skill-flaws.
- Directory `[[.cursor/skills/]]` still appears in issue 01, spec "move from", and project In scope — those name the old directory as a place, not a live skill file.
- Sixteen moved directories exist under [[.agents/skills/]].

## Leftover risks

- Ticket 02 must flip the prepare forbid. Until then an agent that obeys that skill may try to move files back.
- Historical `plan/` issues and reports (git-protocol, lock reports, and others) still name [[.cursor/skills/]] as a fact from that day.
- [[.cursor/rules/project-stage.mdc]] and project-work still use old Stage tokens (`charting`, `tickets`, `active`). Ticket 03.
- update-matt-skills is still indexed in gambol.mdc. Ticket 04.
- During this window, overlapping edits briefly truncated skill-flaws and dropped a project-stage paragraph. Those bodies were restored. Paths were retargeted again after a revert on project-stage.
