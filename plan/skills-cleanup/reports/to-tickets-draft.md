# To-tickets draft — skills cleanup

Alan approved this 13-ticket breakdown on 2026-09-06. Published. Do not merge 1+2. Do not split 13. Ticket 3 stays blocked on 2. The one live `in-progress` ticket maps to takeable inside ticket 13.

Issue files:

1. [[plan/skills-cleanup/issues/01-move-skills-to-agents-home.md]]
2. [[plan/skills-cleanup/issues/02-allow-agents-skill-home-in-prepare-rule.md]]
3. [[plan/skills-cleanup/issues/03-write-status-and-stage-lists-into-canonical-docs.md]]
4. [[plan/skills-cleanup/issues/04-remove-vendor-merge-and-setup-matt.md]]
5. [[plan/skills-cleanup/issues/05-wayfinder-process-git-on-dev.md]]
6. [[plan/skills-cleanup/issues/06-to-spec-publishes-spec-md-only.md]]
7. [[plan/skills-cleanup/issues/07-request-refactor-plan-charts-a-project.md]]
8. [[plan/skills-cleanup/issues/08-reports-land-under-plan-reports.md]]
9. [[plan/skills-cleanup/issues/09-one-glossary-committed-decision.md]]
10. [[plan/skills-cleanup/issues/10-implement-entry-ditch-testing-workflow.md]]
11. [[plan/skills-cleanup/issues/11-delete-triage-qa-files-tracker-issues.md]]
12. [[plan/skills-cleanup/issues/12-ask-matt-and-gambol-mdc-agree.md]]
13. [[plan/skills-cleanup/issues/13-migrate-live-status-labels-and-stage.md]]

Publish report: [[plan/skills-cleanup/reports/to-tickets-publish.md]]. This Project Stage is `slice` per [[doc/agents/project-status.md]]. Stale [[.cursor/rules/project-stage.mdc]] still says `tickets`; ticket 3 catches that copy up.

Source: [[plan/skills-cleanup/spec.md]], Locked section on [[plan/skills-cleanup/project.md]], [[plan/skills-cleanup/reports/unresolved-from-initial-chart.md]]. Status set: [[plan/skills-cleanup/reports/lock-status-set-v2.md]]. Inventory: [[plan/skills-cleanup/reports/status-reconsider-inventory.md]].

Do not close or modify a parent GitHub issue. Do not extract the shared ticket-publish core in this Project (locked later, not now).

## Pointers

- Spec: [[plan/skills-cleanup/spec.md]]
- Destination lock: [[plan/skills-cleanup/project.md]]
- Ask-Matt / gambol.mdc: [[plan/skills-cleanup/reports/lock-ask-matt-gambol-agree.md]]
- Skill home, both ticket skills, Wayfinder vs tracker: [[plan/skills-cleanup/reports/lock-tickets-home-wayfinder.md]]
- Git on `dev`: [[plan/skills-cleanup/reports/lock-git-all-on-dev.md]]
- Vendor merge obsolete: [[plan/skills-cleanup/reports/lock-vendor-merge-obsolete.md]]
- Grill, reports, refactor-plan, glossary: [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]]
- Implement path: [[plan/skills-cleanup/reports/lock-implement-path.md]]
- Delete triage: [[plan/skills-cleanup/reports/lock-delete-triage.md]]
- Keep QA: [[plan/skills-cleanup/reports/lock-keep-qa.md]]
- Bold fields; spec.md only: [[plan/skills-cleanup/reports/lock-bold-properties-spec-md.md]]
- Architecture review reports: [[plan/skills-cleanup/reports/lock-architecture-review-reports-path.md]]
- Canonical lists today: [[doc/agents/project-status.md]] (Stage already matches), [[doc/agents/triage-labels.md]], [[doc/agents/issue-tracker.md]]
- Glossary: [[CONTEXT.md]]
- Git procedure: [[doc/Decisions/0002-git-protocol.md]]
- Instruction-edit method: [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]] (moves with ticket 1)

## Prefactors

- Accidental [[.agents/skills/to-tickets - Copy/]] is junk. Delete it in ticket 1.
- Sixteen live skill directories sit under [[.cursor/skills/]], including obsolete [[.cursor/skills/update-matt-skills/SKILL.md]] (scripts included). Ticket 1 moves the whole set so [[.cursor/skills/]] holds no SKILL.md. Ticket 4 then deletes the vendor skill from the new home.
- Pointer retarget is a path expand–contract, not F# product work. Retarget live skills, rules, [[CONTEXT.md]], [[doc/agents/]], [[.cursor/rules/gambol.mdc]], and live Committed Decision procedure links. Do not rewrite chart-time lock reports that name the old directory as a fact from that day.
- After the move, [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]] still forbids [[.agents/skills/]] until ticket 2. Merge 1 and 2 if one window should leave the home legal.
- [[.cursor/skills/implement-fsharp-feature/SKILL.md]] already holds the Client compile gate. Ticket 10 removes [[.cursor/rules/testing-workflow.mdc]] (stop-for-review always-apply). Keep the compile gate in the F# skill. Do not drop it with the rule.
- [[.cursor/skills/plan-roadmap-change/SKILL.md]] still names missing `doc/plan`. Fix that pointer when that skill is edited. Not a separate ticket.
- Live Status migrate is a wide mechanical change (185 issue files at inventory time). Canonical docs expand first (ticket 3). Ticket 13 contracts old tokens. Split 13 by `plan/<slug>/` if one window cannot finish the judgment pass on `open`.

## Uncertainties

- Granularity: thirteen slices follow the spec's numbered operations. Coarser merge candidates: 1+2 (home plus rule flip), 6+7 (planning skills), 8+9 (reports plus glossary). Finer split candidate: ticket 13 into mechanical bulk (`resolved` / `closed` / `agent-done` / unbold) versus a judgment pass on `open`.
- Blocking: ticket 3 could run beside ticket 1 (it edits [[doc/agents/]], not the skill home). The spec still puts the move first. This draft blocks 3 on 2 so later body edits are legal in one home with one Status list.
- Ticket 12 (routers) waits on catalog changes (4, 7, 10, 11), not on every body rewrite. Ticket 13 waits until those bodies agree, so migrate is last.
- One live ticket is `in-progress`. The lock drops that token and does not map it. Propose takeable unless Alan says otherwise.
- [[plan/roadmap/project.md]] still has `Stage: steering`. The lock says the Roadmap has no Stage. Ticket 13 should drop that field on the Roadmap Project only, and must not rewrite [[plan/roadmap/map.md]].
- Shared publish core for the two ticket skills stays out of this draft.

## Proposed tickets

### 1. Move repo-shared skills to the agents home

- **Blocked by:** None — can start immediately.
- **What it delivers:** A coding agent that opens a Gambol workflow skill finds it under [[.agents/skills/]]. [[.cursor/rules/gambol.mdc]] and other live indexes name that path. [[.cursor/skills/]] holds no live SKILL.md. [[.agents/skills/to-tickets - Copy/]] is gone. Chart-time lock reports may still name the old path as history.
- **See also:** [[plan/skills-cleanup/reports/lock-tickets-home-wayfinder.md]], [[plan/skills-cleanup/spec.md]]

### 2. Allow the agents skill home in the prepare rule

- **Blocked by:** Ticket 1.
- **What it delivers:** An agent that follows prepare-agent-instruction-change (under [[.agents/skills/]] after ticket 1) may keep repo-shared skills in [[.agents/skills/]]. The old rule that forbids that home is gone. Later slices in this Project are legal in the locked home.
- **See also:** [[plan/skills-cleanup/reports/lock-tickets-home-wayfinder.md]], [[plan/skills-cleanup/project.md]] (Skill home lock)

### 3. Write Status and Stage lists into canonical docs

- **Blocked by:** Ticket 2.
- **What it delivers:** [[doc/agents/triage-labels.md]] and [[doc/agents/issue-tracker.md]] list ticket Status `ready-for-agent` | `ready-for-human` | `needs-info` | `blocked` | `done` | `cancelled`. Takeable is the first two. Closed is `done`. `cancelled` is reject or abandon of a ticket, not Stage `dead`. `needs-triage`, `wontfix`, `open`, `resolved`, `claimed`, `closed`, `agent-done`, and `in-progress` are not Status values. Tracker templates use bold field labels (`**Status:**`, `**Type:**`). [[.cursor/rules/project-stage.mdc]] and [[.cursor/skills/projects-overview/SKILL.md]] (new home) use Stage `chart` | `spec` | `slice` | `build` | `done` | `dead` and the Who-writes-Stage table. [[CONTEXT.md]] matches those lists. Live tickets and other Projects' `Stage:` lines are not rewritten yet. `blocked` stays in the Status list even when no live ticket uses it.
- **See also:** [[plan/skills-cleanup/reports/lock-status-set-v2.md]], [[plan/skills-cleanup/reports/lock-bold-properties-spec-md.md]]

### 4. Remove the vendor merge and setup-matt bootstrap

- **Blocked by:** Ticket 2.
- **What it delivers:** A coding agent does not run update-matt-skills or setup-matt. Those skills and the seed copies under [[.agents/skills/setup-matt-pocock-skills/]] are deleted or archived. Live skills do not say run setup-matt first. Ask-Matt stays. Git places `vendor/mattpocock-skills` and `update/mattpocock-skills` are not taught.
- **See also:** [[plan/skills-cleanup/reports/lock-vendor-merge-obsolete.md]], [[plan/skills-cleanup/reports/lock-git-all-on-dev.md]]

### 5. Wayfinder is process; all git stays on `dev`

- **Blocked by:** Ticket 3, Ticket 4.
- **What it delivers:** An agent that runs Wayfinder charts a destination and decides when to research, grill, or prototype. For claim, Type, Status, frontier, and file layout it follows [[doc/agents/issue-tracker.md]] and does not copy GitHub-shaped ops (assignee claim, `wayfinder:` labels, close-the-issue). It does not teach `research/<name>` or `prototype/<name>` places. Prototype capture stays on `dev`, then `ready` / `master` per git-protocol. Ticket Type (`research` | `prototype` | `grilling` | `task`) stays a Type axis, not Status.
- **See also:** [[plan/skills-cleanup/reports/lock-tickets-home-wayfinder.md]], [[plan/skills-cleanup/reports/lock-git-all-on-dev.md]]

### 6. to-spec publishes spec.md only

- **Blocked by:** Ticket 3, Ticket 4.
- **What it delivers:** An agent that runs `/to-spec` writes spec.md on the Project. It does not file a spec as a ticket and does not apply `ready-for-agent` to a spec. The spec has no `**Status:**`.
- **See also:** [[plan/skills-cleanup/reports/lock-bold-properties-spec-md.md]], [[doc/agents/issue-tracker.md]]

### 7. request-refactor-plan charts a new Project

- **Blocked by:** Ticket 2, Ticket 3.
- **What it delivers:** An agent that runs request-refactor-plan creates a new Feature-set Project under `plan/` and charts it, like Wayfinder. It is not a third spec-or-ticket skill in a line. New Project structure follows [[doc/agents/issue-tracker.md]] and project-work by reference. The skill does not copy that structure.
- **See also:** [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]], [[doc/agents/issue-tracker.md]]

### 8. Reports land under plan reports

- **Blocked by:** Ticket 2.
- **What it delivers:** Research and architecture review write Markdown under `plan/<slug>/reports/` (current Project or the Project under review) unless a skill already names another path. Architecture review does not write HTML in OS temp and does not open a Tailwind CDN page. Subagent reports stay in that same reports folder, as core-agent-behavior already says.
- **See also:** [[plan/skills-cleanup/reports/lock-architecture-review-reports-path.md]], [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]]

### 9. One glossary; say Committed Decision

- **Blocked by:** Ticket 2.
- **What it delivers:** domain-modeling writes [[CONTEXT.md]] and records hard choices as Committed Decisions under [[doc/Decisions/]]. It does not say ADR. ubiquitous-language is not a second glossary owner and does not create `UBIQUITOUS_LANGUAGE.md`. Skills this ticket touches use Committed Decision for that record. Ask-Matt wording waits for ticket 12. Architecture-review wording can land here or in ticket 8 (same files must not fight).
- **See also:** [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]], [[CONTEXT.md]] (Committed Decision)

### 10. `/implement` is the entry; ditch testing-workflow

- **Blocked by:** Ticket 2.
- **What it delivers:** An agent that builds a ticket starts at `/implement`. tdd, implement-fsharp-feature, and add-shared-test stay as referenced augmentations. They do not publish a second red-green loop or a stop-for-review always-apply rule. [[.cursor/rules/testing-workflow.mdc]] is gone from always-apply and from the gambol.mdc index. The Client compile gate still runs from the F# skill when Client dependencies change. tdd is not deleted.
- **See also:** [[plan/skills-cleanup/reports/lock-implement-path.md]], [[.agents/skills/implement/SKILL.md]]

### 11. Delete triage; QA files valid tracker issues

- **Blocked by:** Ticket 3.
- **What it delivers:** The triage skill and its companions (`AGENT-BRIEF`, `OUT-OF-SCOPE`) are gone. No live skill or index tells an agent to apply GitHub labels to Markdown files. QA stays the conversational file-issues skill. An issue QA files is a valid tracker file: bold `**Status:**` with a locked Status value, and it points at [[doc/agents/issue-tracker.md]] instead of copying a second ticket template. Ticket skills still own the `/to-tickets` template.
- **See also:** [[plan/skills-cleanup/reports/lock-delete-triage.md]], [[plan/skills-cleanup/reports/lock-keep-qa.md]]

### 12. Ask-Matt and gambol.mdc agree

- **Blocked by:** Ticket 4, Ticket 7, Ticket 10, Ticket 11.
- **What it delivers:** Ask-Matt (human advisor) and gambol.mdc (primary agent instruction file) list the same jobs, the same skill names, and the same sequence. Both name the two ticket skills. Both name the Gambol adapters (git-protocol, git-share, git-master, project-work, and the rest of the live index). Default grill entry is `/grill-me`. Neither names setup-matt, update-matt-skills, or `/triage`. Neither router is deleted.
- **See also:** [[plan/skills-cleanup/reports/lock-ask-matt-gambol-agree.md]], [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]]

### 13. Migrate live Status, bold labels, and Stage tokens

- **Blocked by:** Ticket 3, Ticket 5, Ticket 6, Ticket 7, Ticket 8, Ticket 9, Ticket 10, Ticket 11, Ticket 12.
- **What it delivers:** Live tickets under `plan/**/issues/` use only the locked Status set and bold field labels. `open` becomes takeable (`ready-for-agent` unless the ticket is clearly human). `resolved` / `closed` / `agent-done` become `done`. Any `wontfix` becomes `cancelled`. Unbolded `Status:` / `Type:` become `**Status:**` / `**Type:**`. Other Feature-set Projects' `Stage:` tokens become `chart` | `spec` | `slice` | `build` | `done` | `dead` (`charting` → `chart`, `tickets` → `slice`, `active` → `build`). The Roadmap Project file does not carry Stage. The Status inventory search class in [[plan/skills-cleanup/reports/status-reconsider-inventory.md]] is re-run and shows no forbidden first tokens on live tickets. Product F# is untouched. The Roadmap map body is not rewritten.
- **See also:** [[plan/skills-cleanup/reports/lock-status-set-v2.md]], [[plan/skills-cleanup/reports/lock-bold-properties-spec-md.md]]

## Dependency

`1 → 2 → (3 ∥ 4 ∥ 8 ∥ 9 ∥ 10)` then `5` and `6` wait on 3+4; `7` waits on 2+3; `11` waits on 3; `12` waits on 4+7+10+11; `13` waits on 3 and 5–12.

Frontier after 1–2: tickets 3, 4, 8, 9, 10 (and 7 once 3 exists).

## Quiz

1. Does the granularity feel right (too coarse / too fine)?
2. Are the blocking edges correct — does each ticket depend only on tickets that genuinely gate it?
3. Should any tickets be merged or split further?
