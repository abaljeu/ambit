# Implement issue 03 — canonical Status and Stage lists

Wrote the locked ticket Status set and Project Stage set into the canonical tracker docs. [[doc/agents/project-status.md]] already matched Stage and Who-writes-Stage; it was not edited. Live ticket headers and other Projects' `Stage:` lines were not rewritten. Product F# was not touched. Forced Stage gates were not installed.

## What changed

- [[doc/agents/triage-labels.md]] Status list is `ready-for-agent` | `ready-for-human` | `needs-info` | `blocked` | `done` | `cancelled`. Takeable is the first two. Closed is `done`. `cancelled` is ticket reject or abandon, not Stage `dead`. `blocked` stays. Dropped tokens are named only as do-not-use.
- [[doc/agents/issue-tracker.md]] templates use `**Status:**` and `**Type:**`. Operations use the locked takeable pair and close with `done`. Status values stay in triage-labels (no second list).
- [[.cursor/rules/project-stage.mdc]] uses Alan's grilling copy (method, not Stage) and Who-writes-Stage: `/wayfinder` → `chart`; `/to-spec` → `spec`; ticket skills → `slice`; first implement → `build`; delivered → `done`; abandon → `dead`.
- [[.agents/skills/projects-overview/SKILL.md]] sorts and assigns `chart` | `spec` | `slice` | `build` | `done` | `dead`. Missing `project.md` gets Stage `chart`.
- [[.agents/skills/project-work/SKILL.md]] no longer writes `Stage: grilling` or `charting`. It points at [[doc/agents/project-status.md]] for vocabulary and Who-writes-Stage.
- [[CONTEXT.md]] Stage and Status value lists and avoid-lists match the lock. Glossary-owner skill text was not edited.
- Ticket [[plan/skills-cleanup/issues/03-write-status-and-stage-lists-into-canonical-docs.md]] is `**Status:** done`.

## How verified

- Canonical Status table in triage-labels has the six locked values and no live `needs-triage` / `wontfix` row.
- issue-tracker ticket field templates are `**Status:**` / `**Type:**`.
- project-stage.mdc has no `charting`, `tickets`, `steering`, or `active` as Stage, and no `/grilling` → Stage.
- projects-overview sort order is the locked Stage list.
- CONTEXT.md Stage and Status entries list the locked pipes and avoid the dropped tokens.

## Leftover risks

- Live `plan/**/issues/` headers still use old Status words and unbolded labels (ticket 13). Other Projects' `Stage:` lines still use old tokens.
- Skills and rules outside this ticket (Wayfinder, to-spec, Ask-Matt, gambol.mdc, implement) may still teach old Status or Stage words.
- [[.agents/skills/project-work/SKILL.md]] Finish still says **agent-done** as the git-protocol finish name. That is not a ticket Status.
- issue-tracker Time examples still use unbolded `Estimate:` / `Actual:` / `Started:`.
- [[doc/agents/project-status.md]] `project.md` template still uses unbolded `Stage:`.
