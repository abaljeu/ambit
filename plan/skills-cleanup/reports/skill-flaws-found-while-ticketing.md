# Skill flaws found while ticketing

Noted during `/to-tickets` publish for [[plan/skills-cleanup/]]. Do not repair in this pass. Later review.

## [[.agents/skills/to-tickets/SKILL.md]]

- Precondition still says run [[.agents/skills/setup-matt-pocock-skills/SKILL.md]] if tracker vocab is missing. This repo already has [[doc/agents/issue-tracker.md]]. Ticket [[plan/skills-cleanup/issues/04-remove-vendor-merge-and-setup-matt.md]] deletes that bootstrap.
- Step 2 says respect ADRs. [[CONTEXT.md]] says Committed Decision.
- Vertical-slice rules assume schema, API, UI, and tests. Instruction-file work has no product layers. The rule does not describe a verifiable instruction slice.
- Step 5 says work the frontier right after publish. That reads as start implement in the same session.
- The template always includes Comments. The skill text says add Comments when there is a comment.

## [[.agents/skills/wait-what/SKILL.md]]

- One sentence. It does not say how Context differs from What to build. Ticket prose has to invent that split.

## [[.agents/skills/project-work/SKILL.md]] and [[.cursor/rules/project-stage.mdc]]

- After grilling starts, project-work still writes Stage `charting`. Locked Stage is `chart` in [[doc/agents/project-status.md]].
- Both still map `/to-tickets` to Stage `tickets`. Locked Who-writes-Stage is `slice`. This publish used `slice` from [[doc/agents/project-status.md]].

## [[doc/agents/issue-tracker.md]] and [[doc/agents/triage-labels.md]]

- Conventions still say an unbolded `Status:` line. The 2026-09-02 template and the bold-field lock require `**Status:**`.
- triage-labels still lists `needs-triage` and `wontfix`. Locked Status set is in [[plan/skills-cleanup/reports/lock-status-set-v2.md]]. Ticket [[plan/skills-cleanup/issues/03-write-status-and-stage-lists-into-canonical-docs.md]] is the obedience.
- triage-labels has Status `blocked`. The to-tickets template always writes `ready-for-agent` and puts the wait in Blocked by. Frontier text is takeable plus unblocked. Two stories for a named wait. This publish followed the template.
