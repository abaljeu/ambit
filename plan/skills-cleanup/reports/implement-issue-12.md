# Implement issue 12 — Ask-Matt and gambol.mdc agree

[[.agents/skills/ask-matt/SKILL.md]] (human advisor) and [[.cursor/rules/gambol.mdc]] (primary Agent instruction file) now list the same jobs, the same skill names, and the same sequence. Both name the two ticket skills and the Gambol adapters. Default grill is `/grill-me`. Neither names setup-matt, update-matt-skills, or `/triage`. Both routers remain. Prototype capture is on `dev`. Product F# was not touched.

## What changed

- Shared job catalog in both files: idea to ship (including `/implement` and both ticket skills), on-ramps (`/qa`, Wayfinder, request-refactor-plan), vocabulary and health, Gambol adapters, standalone.
- Ask-Matt default grill is `/grill-me`. `/grill-with-docs` stays named and is not the default invoke. ADR wording is Committed Decision. `/qa` replaces `/triage`. `prototype/<name>` git-place teaching is gone.
- [[.cursor/rules/gambol.mdc]] keeps rules, runtime state, tracker docs, and tool bridges. The skill index matches Ask-Matt.

## How verified

- Extracted the 40 `[[.agents/skills/...]]` job-list lines from both files. They match in order and blurb.
- Search on both owned routers finds no setup-matt, update-matt-skills, `/triage`, or `prototype/<name>`.
- Both name [[.agents/skills/to-tickets/SKILL.md]], [[.agents/skills/to-feature-tickets/SKILL.md]], [[.agents/skills/git-protocol/SKILL.md]], [[.agents/skills/git-share/SKILL.md]], [[.agents/skills/git-master/SKILL.md]], and [[.agents/skills/project-work/SKILL.md]].
- Ticket [[plan/skills-cleanup/issues/12-ask-matt-and-gambol-mdc-agree.md]] is `**Status:** done`.

## Leftover risks

- [[.agents/skills/ask-matt/PHASE-BOUNDARIES.md]] was not edited (not in this ticket's file list).
- [[.agents/skills/triage/]] still has files on disk. Ticket 11 claimed delete; out of this ticket's exclusive files.
- [[doc/agents/domain.md]], [[.agents/skills/grill-with-docs/SKILL.md]], [[.agents/skills/diagnosing-bugs/SKILL.md]], [[.agents/skills/handoff/SKILL.md]], and [[.agents/skills/to-spec/SKILL.md]] still say ADR.
- Skills not in the live index (azure, loop-me, roll-dice, and others) stay unlisted. Intentional: the catalog is the agreed job list, not every folder under [[.agents/skills/]].
- [[.cursor/rules/core-agent-behavior.mdc]] still points at deleted testing-workflow. Ticket 10 leftover.
- [[doc/agents/project-status.md]] Who-writes-Stage still lists only `/wayfinder` → `chart`. request-refactor-plan now charts too. Ticket 07 leftover.
- Live ticket Status migrate waits on ticket 13.
