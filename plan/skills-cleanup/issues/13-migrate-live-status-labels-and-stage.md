# 13 — Migrate live Status, bold labels, and Stage tokens

**Status:** done
**Blocked by:** [[03-write-status-and-stage-lists-into-canonical-docs.md|03 Write Status and Stage lists into canonical docs]], [[05-wayfinder-process-git-on-dev.md|05 Wayfinder is process; all git stays on `dev`]], [[06-to-spec-publishes-spec-md-only.md|06 to-spec publishes spec.md only]], [[07-request-refactor-plan-charts-a-project.md|07 request-refactor-plan charts a new Project]], [[08-reports-land-under-plan-reports.md|08 Reports land under plan reports]], [[09-one-glossary-committed-decision.md|09 One glossary; say Committed Decision]], [[10-implement-entry-ditch-testing-workflow.md|10 `/implement` is the entry; ditch testing-workflow]], [[11-delete-triage-qa-files-tracker-issues.md|11 Delete triage; QA files valid tracker issues]], [[12-ask-matt-and-gambol-mdc-agree.md|12 Ask-Matt and gambol.mdc agree]]
**Actual:** 1h 30m

## Context

Instruction files now agree. Live tickets still say `open`, `resolved`, `closed`, `agent-done`, or `in-progress`. Many headers use unbolded `Status:`. Other Feature-set Projects still say `charting`, `tickets`, or `active`. The Roadmap Project file still carries Stage `steering`. The issue tracker frontier is a Status scan. Old tokens hide takeable work.

## What to build

Live tickets under `plan/**/issues/` use only the locked Status set and bold field labels. `open` becomes takeable (`ready-for-agent` unless the ticket is clearly human). `resolved` / `closed` / `agent-done` become `done`. Any `wontfix` becomes `cancelled`. The one live `in-progress` ticket becomes takeable (`ready-for-agent` unless it is clearly human). Unbolded `Status:` / `Type:` become `**Status:**` / `**Type:**`. Other Feature-set Projects' Stage tokens become `chart` | `spec` | `slice` | `build` | `done` | `dead`. The Roadmap Project file does not carry Stage. The Status inventory search class is re-run and shows no forbidden first tokens on live tickets. Product F# is untouched. The Roadmap map body is not rewritten.

- [x] Live tickets use only `ready-for-agent` | `ready-for-human` | `needs-info` | `blocked` | `done` | `cancelled`, with bold field labels.
- [x] `open` and the one `in-progress` ticket are takeable (`ready-for-agent` unless clearly human). `resolved` / `closed` / `agent-done` are `done`. Any `wontfix` is `cancelled`.
- [x] Other Feature-set Projects use locked Stage tokens (`charting` → `chart`, `tickets` → `slice`, `active` → `build`). The Roadmap Project file has no Stage.
- [x] The inventory search class in [[plan/skills-cleanup/reports/status-reconsider-inventory.md]] is re-run. Live tickets show no forbidden first tokens.

## Comments

Alan (2026-09-06): do not split this ticket. Map the one live `in-progress` ticket to takeable (`ready-for-agent` unless clearly human).

## See also

[[plan/skills-cleanup/reports/lock-status-set-v2.md]], [[plan/skills-cleanup/reports/lock-bold-properties-spec-md.md]]

## Time

- 2026-09-06 1h 30m — migrated live ticket Status/Type headers and Feature-set Project Stage tokens (from chat)
