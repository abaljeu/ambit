# 03 — Write Status and Stage lists into canonical docs

**Status:** done
**Blocked by:** [[02-allow-agents-skill-home-in-prepare-rule.md|02 Allow the agents skill home in the prepare rule]]
**Actual:** 1h

## Context

[[doc/agents/project-status.md]] already lists Stage `chart` | `spec` | `slice` | `build` | `done` | `dead`. Ticket Status still copies the old set in [[doc/agents/triage-labels.md]] and [[doc/agents/issue-tracker.md]]. [[.cursor/rules/project-stage.mdc]] still writes `charting` and `tickets`. An Agent that writes a ticket or a Project Stage can pick the wrong token.

## What to build

Canonical tracker docs list one Status set and one Stage set. Ticket Status is `ready-for-agent` | `ready-for-human` | `needs-info` | `blocked` | `done` | `cancelled`. Takeable is the first two. Closed is `done`. `cancelled` is reject or abandon of a ticket, not Stage `dead`. Tracker templates use bold field labels. project-stage and projects-overview use the locked Stage list and the Who-writes-Stage table. [[CONTEXT.md]] matches those lists. Live tickets and other Projects' Stage lines are not rewritten yet. `blocked` stays in the Status list even when no live ticket uses it.

- [x] [[doc/agents/triage-labels.md]] and [[doc/agents/issue-tracker.md]] list only the locked Status set. They drop `needs-triage`, `wontfix`, `open`, `resolved`, `claimed`, `closed`, `agent-done`, and `in-progress`.
- [x] Tracker templates in those docs use bold field labels (`**Status:**`, `**Type:**`).
- [x] [[.cursor/rules/project-stage.mdc]] and projects-overview use Stage `chart` | `spec` | `slice` | `build` | `done` | `dead` and the Who-writes-Stage table.
- [x] [[CONTEXT.md]] Status and Stage avoid-lists match the lock.
- [x] Live `plan/**/issues/` headers and other Projects' `Stage:` lines are unchanged.

## See also

[[plan/skills-cleanup/reports/lock-status-set-v2.md]], [[plan/skills-cleanup/reports/lock-bold-properties-spec-md.md]]

## Time

- 2026-09-06 1h — wrote locked Status and Stage lists into canonical tracker docs, project-stage, overview and project-work skills, and CONTEXT.md names (from chat)
