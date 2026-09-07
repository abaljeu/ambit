# Skill flaws found while ticketing

Noted during `/to-tickets` publish for [[plan/skills-cleanup/]]. Leftover repair: [[plan/skills-cleanup/reports/address-skill-flaws-while-ticketing.md]].

## [[.agents/skills/to-tickets/SKILL.md]]

- **Done** (ticket 04). No setup-matt precondition. This repo already has [[doc/agents/issue-tracker.md]].
- **Done** (leftover repair). Step 2 says Committed Decision, not ADR.
- **Done** (leftover repair). Instruction-file slices are verifiable by repo search and published instruction behavior. Schema, API, and UI are not required for that work.
- **Done** (leftover repair). Step 5 publishes tickets and records the frontier for a later implement.
- **Done** (leftover repair). The template has no Comments section. The skill adds `## Comments` only when there is a comment.

Same ADR and Comments leftovers were in [[.agents/skills/to-feature-tickets/SKILL.md]]. Those two are **Done**. That skill had no frontier-after-publish leftover.

## [[.agents/skills/wait-what/SKILL.md]]

- **Done** (leftover repair). The skill names how Context differs from What to build.

## [[.agents/skills/project-work/SKILL.md]] and [[.cursor/rules/project-stage.mdc]]

- **Done** (ticket 03). Stage is `chart`, not `charting`.
- **Done** (ticket 03). `/to-tickets` writes `slice`, not `tickets`. Grilling is a method, not a Stage.

## [[doc/agents/issue-tracker.md]] and [[doc/agents/triage-labels.md]]

- **Done** (ticket 03). Conventions use `**Status:**`.
- **Done** (ticket 03). Locked Status set. `needs-triage` and `wontfix` are not live values.
- **Leftover.** triage-labels has Status `blocked`. The to-tickets template writes `ready-for-agent` and puts the wait in Blocked by. Frontier is takeable plus unblocked. Two stories for a named wait. The publish followed the template. No new policy. Do not merge the stories.
