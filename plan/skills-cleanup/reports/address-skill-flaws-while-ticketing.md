# Address skill flaws found while ticketing

Authorized leftover repair of [[plan/skills-cleanup/reports/skill-flaws-found-while-ticketing.md]]. Not a numbered skills-cleanup ticket. Did not migrate live `plan/**/issues/` Status. Did not edit Ask-Matt, gambol.mdc, git-protocol, product F#, or [[plan/skills-cleanup/project.md]].

## Already obedient (no edit)

- Both ticket skills had no setup-matt precondition (ticket 04).
- [[.agents/skills/project-work/SKILL.md]] and [[.cursor/rules/project-stage.mdc]] already use Stage `chart` and `/to-tickets` → `slice`. Grilling is a method, not a Stage (ticket 03).
- [[doc/agents/issue-tracker.md]] already uses `**Status:**`. [[doc/agents/triage-labels.md]] already lists the locked Status set (ticket 03).

## Fixed

- [[.agents/skills/to-tickets/SKILL.md]]: Committed Decision; instruction-file slices verifiable by repo search and published instruction behavior; step 5 records the frontier for a later implement; Comments section only when there is a comment.
- [[.agents/skills/to-feature-tickets/SKILL.md]]: Committed Decision; Comments section only when there is a comment. No frontier-after-publish leftover.
- [[.agents/skills/wait-what/SKILL.md]]: Context is the situation before the change. What to build is the new behaviour from that person's view.

## Left

- Status `blocked` vs template `ready-for-agent` plus Blocked by. The lock keeps `blocked`. Did not invent a merge policy.
