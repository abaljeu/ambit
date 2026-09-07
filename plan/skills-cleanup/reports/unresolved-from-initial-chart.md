# Unresolved from the initial chart

Contradiction clusters in [[plan/skills-cleanup/reports/skill-contradiction-chart.md]] are locked. Status set v2 is locked in [[plan/skills-cleanup/reports/lock-status-set-v2.md]]. This file splits leftovers only. No new questions. No obedience started.

## A. Still unresolved (Alan must decide)

None. Clusters are locked. Leftover choices are locked (architecture review reports path in [[plan/skills-cleanup/reports/lock-architecture-review-reports-path.md]]). Remaining work is later obedience.

## B. Locked, later obedience

- Ask-Matt and [[.cursor/rules/gambol.mdc]] agree (same jobs, names, sequence; name both ticket skills; default `/grill-me`; keep `/implement` plus F# augmentations).
- Change [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]] so repo-shared skills may live in [[.agents/skills/]]; retarget or move workflow skills; update the gambol.mdc index.
- Extract ticket-skill common core; leave both named skills.
- Wayfinder body references [[doc/agents/issue-tracker.md]] (no copied GitHub ops, no `research/` branches).
- Prototype skill drops `prototype/` branches; all git stays on `dev`.
- Delete [[.cursor/skills/update-matt-skills/SKILL.md]] and stop setup-matt preconditions; archive vendor seed copies under [[.agents/skills/setup-matt-pocock-skills/]].
- Delete [[.agents/skills/triage/SKILL.md]] and pointers.
- Ditch [[.cursor/rules/testing-workflow.mdc]]; make tdd and implement-fsharp-feature agree with `/implement` by reference.
- Write Status set v2 into [[doc/agents/triage-labels.md]] and [[doc/agents/issue-tracker.md]]; migrate live tickets (`open` → takeable, `resolved`/`closed`/`agent-done` → `done`, `wontfix` → `cancelled`); migrate unbolded field labels to `**Status:**` / `**Type:**`.
- Edit [[.agents/skills/to-spec/SKILL.md]] so it publishes `spec.md` only (not a ticket / `ready-for-agent`).
- ADR wording → Committed Decision; do not create UBIQUITOUS_LANGUAGE.md.
- request-refactor-plan references tracker and project-work; fix dead `doc/plan` when editing that skill.
- Point research / code-review leftovers at `plan/<slug>/reports/` unless a skill already names another path; change [[.agents/skills/improve-codebase-architecture/SKILL.md]] from temp HTML to a Markdown report under that path; QA files carry tracker fields without becoming a pointer skill.
- Old Stage copies ([[.cursor/rules/project-stage.mdc]], other Projects, projects-overview sort). Forced Stage gates still wait.
- Duplicate writing/design theory: later trim (no winner locked).

## C. Explicitly ignore

- [[.agents/skills/git-guardrails-claude-code/SKILL.md]] unless Claude Code hooks are in use.
- Foreign-stack skills ([[.agents/skills/setup-ts-deep-modules/SKILL.md]], [[.agents/skills/migrate-to-shoehorn/SKILL.md]], [[.agents/skills/setup-pre-commit/SKILL.md]], prototype `pnpm`/`bun`) unless they auto-invoke.
- [[.agents/skills/loop-me/SKILL.md]] unless someone runs it here.
- GitHub triage encoding (triage skill is delete-later).
- Product code, whole Roadmap map rewrite, Core, ADR or Learning Record Status fields ([[plan/skills-cleanup/project.md]] Out of scope).
