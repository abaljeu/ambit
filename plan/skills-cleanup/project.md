# Skills cleanup

Stage: done
Summary: Skill home is .agents/skills/. Frontier is canonical Status and Stage lists. Shared ticket-publish core stays out of this Project.
Updated: 2026-09-07
Started: 2026-09-06
Finished: 2026-09-06
Actual: 13h 15m

## Core problem

We did not have one Stage list and one Status list. Those lists are now locked below. Ticket Status collided with Wayfinder `open|claimed|resolved`. Grilling and steering were treated as Stages. Live files and always-apply rules still use the old tokens until a later ticket.

## Chart closed

2026-09-06. Contradiction clusters and leftover choices are locked. The Locked section is the destination. Remaining work is later obedience (instruction edits, ticket Status migrate). See [[plan/skills-cleanup/reports/chart-closed.md]] and [[plan/skills-cleanup/reports/unresolved-from-initial-chart.md]].

## Destination

Build order: [[plan/skills-cleanup/spec.md]]. Canonical lists in [[doc/agents/project-status.md]] and [[doc/agents/triage-labels.md]] (Status catch-up later; lock lives here until those files match). Glossary names in [[CONTEXT.md]]. Ask-Matt and gambol.md must agree (one job catalog in gambol; Ask-Matt points at it; no skip of Gambol adapters). Keep both ticket skills; later extract their common core. Repo-shared skills live in [[.agents/skills/]]. Wayfinder is process and points at [[doc/agents/issue-tracker.md]] (structure). All work on `dev` (then `ready` / `master` per [[.agents/skills/git-protocol/SKILL.md]]). The vendor merge/flatten/setup-matt bootstrap is obsolete; Ask-Matt stays. Default grill entry is [[.agents/skills/grill-me/SKILL.md]]. Reports live under `plan/<slug>/reports/` unless a skill names another path. [[.agents/skills/request-refactor-plan/SKILL.md]] is a process that creates and charts a new Project (like Wayfinder), not a third spec/ticket skill. [[.agents/skills/domain-modeling/SKILL.md]] writes [[CONTEXT.md]]; always say Committed Decision, not ADR. Entry implement is [[.agents/skills/implement/SKILL.md]]; ditch [[.cursor/rules/testing-workflow.mdc]] later; tdd and implement-fsharp-feature augment by reference. Delete [[.agents/skills/triage/SKILL.md]] later. Keep [[.agents/skills/qa/SKILL.md]] (process); issues it files must be valid tracker files (structure already locked). Later: skills and rules obey those lists. Forced Stage gates wait.

## Locked

- **Stage:** `chart` | `spec` | `slice` | `build` | `done` | `dead`. Project may use all. Epic and Chapter skip `slice`. Roadmap has no Stage and no Status. Tickets have no Stage.
- **Status:** `ready-for-agent` | `ready-for-human` | `needs-info` | `blocked` | `done` | `cancelled`. Tickets only. Takeable = `ready-for-agent` or `ready-for-human`. Closed = `done` only. `cancelled` is reject/abandon (not Stage `dead`). Drop `needs-triage`, `wontfix`, `open`, `resolved`, `claimed`, `closed`, `agent-done`, `in-progress`. See [[plan/skills-cleanup/reports/lock-status-set-v2.md]].
- **Who writes Stage:** `/wayfinder` → `chart`; `/to-spec` → `spec`; `/to-tickets` and `/to-feature-tickets` → `slice`; first implement → `build`; delivered → `done`; abandon → `dead`. Stamp Epic/Chapter `build` when a pointed Project enters `slice` or `build`.
- **Grilling** is a method, not a field. **Archive** is an action from `done`. **Rework** is a move back. Revive from `dead` by naming a live Stage.
- Forced gates wait.
- **Ask-Matt and gambol.md:** Ask-Matt ([[.agents/skills/ask-matt/SKILL.md]]) is the human advisor agent. [[.agents/rules/gambol.md]] is the primary agent instruction file and holds the job catalog. Keep both. Ask-Matt references that catalog; do not paste it twice. Same jobs, skill names, and sequence; do not skip Gambol adapters. Do not pick one router and delete the other. See [[plan/skills-cleanup/reports/lock-ask-matt-gambol-agree.md]].
- **Both ticket skills:** Keep [[.agents/skills/to-tickets/SKILL.md]] (tracer-bullet vertical slices) and [[.agents/skills/to-feature-tickets/SKILL.md]] (cohesive testable capabilities). Later: refactor the common core (shared publish path, template, tracker wiring). That refactor is a destination, not work now. See [[plan/skills-cleanup/reports/lock-tickets-home-wayfinder.md]].
- **Skill home:** Repo-shared skills live in [[.agents/skills/]]. [[.agents/skills/prepare-agent-instruction-change/SKILL.md]] names that home.
- **Wayfinder and tracker:** [[.agents/skills/wayfinder/SKILL.md]] must reference and not repeat [[doc/agents/issue-tracker.md]]. Wayfinder is a process; issue-tracker is a structure. Issue-tracker wins on structure (claim, Type, Status, frontier, file layout). Wayfinder owns the process (when to research, grill, or prototype; how to chart a destination).
- **All git on `dev`:** No extra long-lived places (`research/`, `prototype/`, `vendor/`, `update/`). Work stays on the git-protocol `dev` place, then `ready` / `master` per [[.agents/skills/git-protocol/SKILL.md]]. Wayfinder research and prototype skills must not teach throwaway topic branches. See [[plan/skills-cleanup/reports/lock-git-all-on-dev.md]].
- **Vendor merge obsolete:** Alan will not use the merge-vendor-skills process. [[.agents/skills/update-matt-skills/SKILL.md]] is obsolete: do not run it; do not keep it as a live workflow. Places `vendor/mattpocock-skills` and `update/mattpocock-skills` are dead (also forbidden by all-git-on-`dev`). Live skills must not say run [[.agents/skills/setup-matt-pocock-skills/SKILL.md]] first. Copies under that folder are leftover, not a second tracker. Ask-Matt stays. See [[plan/skills-cleanup/reports/lock-vendor-merge-obsolete.md]].
- **Default grill:** The default grill entry in this repo is [[.agents/skills/grill-me/SKILL.md]]. Other grill skills (grilling, grill-with-docs, wait-what, loop-me) are not the default invoke. Grilling remains a method, not a Stage. See [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]].
- **Reports path:** Gambol reports live under `plan/<slug>/reports/` unless a skill names another path. Matches [[.agents/rules/core-agent-behavior.md]] subagent reports.
- **Request-refactor-plan:** [[.agents/skills/request-refactor-plan/SKILL.md]] is like Wayfinder: a process that creates a new Project under `plan/` and charts it. It is not a third spec/ticket skill in a linear wayfinder→to-spec→tickets sequence. The new Project's structure follows [[doc/agents/issue-tracker.md]] and [[.agents/skills/project-work/SKILL.md]] (reference, do not repeat).
- **One glossary:** [[.agents/skills/domain-modeling/SKILL.md]] writes [[CONTEXT.md]]. Always say **Committed Decision**, not ADR. [[.agents/skills/ubiquitous-language/SKILL.md]] is not a second glossary owner. Later: ADR wording in skills → Committed Decision; do not create UBIQUITOUS_LANGUAGE.md.
- **Implement path:** Entry skill is [[.agents/skills/implement/SKILL.md]] (`/implement`). Ditch [[.cursor/rules/testing-workflow.mdc]] later (do not delete it in this pass). [[.agents/skills/tdd/SKILL.md]], [[.agents/skills/implement-fsharp-feature/SKILL.md]], and [[.agents/skills/add-shared-test/SKILL.md]] stay as referenced augmentations; they must augment `/implement`, not contradict or duplicate it. Do not treat tdd as leftover-to-delete. See [[plan/skills-cleanup/reports/lock-implement-path.md]].
- **Delete triage:** [[.agents/skills/triage/SKILL.md]] is not used here (GitHub labels, `#42`, PRs, `.out-of-scope/`). Later: remove the skill and pointers (gambol.mdc, Ask-Matt, ticket docs). Do not delete or edit those files in this pass. `needs-triage` is dropped from Status (triage deleted). See [[plan/skills-cleanup/reports/lock-delete-triage.md]].
- **Keep QA:** Keep [[.agents/skills/qa/SKILL.md]]. Do not delete it. It stays the conversational file-issues skill. Ticket skills still own the ticket template for `/to-tickets` work. QA process stays; tracker structure is already locked — issues QA files must be valid tracker files (reference [[doc/agents/issue-tracker.md]], do not duplicate a second template). See [[plan/skills-cleanup/reports/lock-keep-qa.md]].
- **Bold field labels:** Ticket and similar fields use bold labels (`**Status:**`, `**Type:**`, and the same for other inline properties). Not unbolded `Status:`. Later: migrate live unbolded fields. Do not migrate now. See [[plan/skills-cleanup/reports/lock-bold-properties-spec-md.md]].
- **spec.md only:** [[.agents/skills/to-spec/SKILL.md]] publishes a spec as `spec.md` on the Project (per [[doc/agents/issue-tracker.md]]). It must not file a spec as a ticket. Later: edit to-spec if it still says publish to the issue tracker / `ready-for-agent`.
- **Architecture review reports:** [[.agents/skills/improve-codebase-architecture/SKILL.md]] writes Markdown under `plan/<slug>/reports/` (current Project or the Project under review). Not OS temp HTML. See [[plan/skills-cleanup/reports/lock-architecture-review-reports-path.md]].

## Later implementation

When this Project edits live skills, rules, or bridges, follow [[.agents/skills/prepare-agent-instruction-change/SKILL.md]]. Later obedience must write the new Status set into [[doc/agents/triage-labels.md]] and [[doc/agents/issue-tracker.md]], migrate live ticket Status values and unbolded labels to `**Status:**` (and the same for other fields), drop the obsolete vendor merge, delete [[.agents/skills/triage/SKILL.md]] and its pointers, ditch [[.cursor/rules/testing-workflow.mdc]], make tdd and implement-fsharp-feature agree with `/implement` by reference (no second loop), retarget Ask-Matt to default `/grill-me`, change ADR wording to Committed Decision, make request-refactor-plan reference the tracker instead of copying Project structure, make to-spec publish `spec.md` only (not a ticket), change [[.agents/skills/improve-codebase-architecture/SKILL.md]] from temp HTML to a Markdown report under `plan/<slug>/reports/`, and make issues QA files carry tracker structure fields without turning QA into a pointer skill. Catch-up includes [[.cursor/rules/project-stage.mdc]] (still the old grilling directive) and live `Stage:` tokens on other Projects.

## In scope

- [[.agents/skills/]]
- [[.cursor/skills/]]
- Instruction files that duplicate skill text, including vendor copies under [[.agents/skills/setup-matt-pocock-skills/]]
- [[doc/agents/]] lists and [[CONTEXT.md]] names (this increment)

## Out of scope

Rewriting product code, rewriting the whole Roadmap map body, implementing Core, ADR or Learning Record Status fields.

## Reports

- [[plan/skills-cleanup/reports/establish-project.md]] — Project established.
- [[plan/skills-cleanup/reports/status-surface-inventory.md]] — Status/Stage surface inventory.
- [[plan/skills-cleanup/reports/status-set-candidates.md]] — Early set candidates (superseded by Locked).
- [[plan/skills-cleanup/reports/skill-contradiction-chart.md]] — Skill-tree contradictions beyond the locked lists.
- [[plan/skills-cleanup/reports/lock-ask-matt-gambol-agree.md]] — Ask-Matt / gambol.mdc lock and current disagreements.
- [[plan/skills-cleanup/reports/dry-ask-matt-gambol.md]] — One catalog in gambol; Ask-Matt points at it.
- [[plan/skills-cleanup/reports/lock-tickets-home-wayfinder.md]] — Both ticket skills, skill home, and Wayfinder-vs-tracker locks.
- [[plan/skills-cleanup/reports/lock-git-all-on-dev.md]] — All git work on `dev`; no extra long-lived places.
- [[plan/skills-cleanup/reports/lock-vendor-merge-obsolete.md]] — Vendor merge/flatten/setup-matt bootstrap is obsolete; Ask-Matt stays.
- [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]] — Default grill, reports path, request-refactor-plan as process, one glossary.
- [[plan/skills-cleanup/reports/lock-implement-path.md]] — `/implement` is the entry; ditch testing-workflow later; tdd and F# skills augment by reference.
- [[plan/skills-cleanup/reports/lock-delete-triage.md]] — Delete triage later.
- [[plan/skills-cleanup/reports/lock-keep-qa.md]] — Keep QA process; tracker structure already locked.
- [[plan/skills-cleanup/reports/status-reconsider-inventory.md]] — Live `Status:` values at reconsider time (facts stay). Status is re-locked in [[plan/skills-cleanup/reports/lock-status-set-v2.md]].
- [[plan/skills-cleanup/reports/lock-status-set-v2.md]] — Ticket Status set v2.
- [[plan/skills-cleanup/reports/unresolved-from-initial-chart.md]] — Leftover choices vs later obedience vs ignore.
- [[plan/skills-cleanup/reports/lock-bold-properties-spec-md.md]] — Bold field labels; to-spec publishes spec.md only.
- [[plan/skills-cleanup/reports/lock-architecture-review-reports-path.md]] — Architecture review writes Markdown under plan reports/.
- [[plan/skills-cleanup/reports/chart-closed.md]] — Chart complete; Stage spec; no skill or ticket migrate.
- [[plan/skills-cleanup/reports/to-spec.md]] — Spec published; first operation is the skill-home move.
- [[plan/skills-cleanup/reports/to-tickets-draft.md]] — Approved 13-ticket draft; published to issues/.
- [[plan/skills-cleanup/reports/skill-flaws-found-while-ticketing.md]] — Skill flaws noted during publish; not repaired.
- [[plan/skills-cleanup/reports/to-tickets-publish.md]] — Published issue list and frontier.
- [[plan/skills-cleanup/reports/implement-issue-01.md]] — Skill-home move.
- [[plan/skills-cleanup/reports/implement-issue-02.md]] — Prepare-rule home flip.
