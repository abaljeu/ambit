# Implement issue 02 — prepare-rule home flip

Flipped [[.agents/skills/prepare-agent-instruction-change/SKILL.md]] so repo-shared skills live in [[.agents/skills/]]. The old forbid that sent Agents to [[.cursor/skills/]] is gone. Inventory, edit-the-most-specific-place, drop copies, and update gambol.mdc when files move stayed. Product F# was not touched. [[.cursor/rules/project-stage.mdc]] was not edited.

## What changed

- Prepare skill: **Skills** principle names [[.agents/skills/]] as the repo-shared skill home. The Do-not bullet that forbade that home is removed. Frontmatter `description` now triggers on `.agents/skills`, not `.cursor/skills`.
- Edit workflow and review checklist are unchanged.
- [[.cursor/rules/gambol.mdc]] was not edited: no skill files were added or removed.
- Ticket [[plan/skills-cleanup/issues/02-allow-agents-skill-home-in-prepare-rule.md]] is `Status: done`.
- Project Stage stays `build`. Summary frontier is canonical Status and Stage lists (ticket 03). [[plan/index.md]] skills-cleanup row matches.

## How verified

- Repo search finds no live line that stores repo-shared skills in [[.cursor/skills/]] or forbids [[.agents/skills/]] as that home.
- The prepare skill contains `Repo-shared skills live in [[.agents/skills/]]`.
- No `SKILL.md` under [[.cursor/skills/]] (`test -e` on the old prepare path is ABSENT).
- `git diff -- .cursor/rules/project-stage.mdc` is empty.

## Leftover risks

- Chart-time reports and [[plan/skills-cleanup/spec.md]] still say the prepare skill forbids the home. Those are history of the build order, not live instruction.
- Ticket 03 still owns canonical Status/Stage lists and the grilling-as-Stage copy in [[.cursor/rules/project-stage.mdc]].
- Empty [[.cursor/skills/]] remains. Prepare no longer names that directory. New skills go to the named home.
