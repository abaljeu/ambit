# Skill contradiction chart

Status and Stage **token lists are already locked**. Do not re-open them. This pass looked at jobs, procedures, homes, and triggers. The skill tree still has several independent collisions that will send an agent to the wrong skill, the wrong tracker, or the wrong git place. The locked lists live in [[doc/agents/project-status.md]] and [[doc/agents/triage-labels.md]]. Live skills and always-apply rules still copy old Stage and Status words; that catch-up is later obedience, not this chart.

## Clustered issues

### 1. Two skill homes, two routers, a false ban

**Where:** [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]] says do not store repo-shared skills in [[.agents/skills/]] — use [[.cursor/skills/]]. [[.cursor/skills/update-matt-skills/SKILL.md]] flattens vendor skills into [[.agents/skills/]]. [[.cursor/rules/gambol.mdc]] lists ticket skills under [[.agents/skills/]] and workflow skills under [[.cursor/skills/]], but it names only [[.agents/skills/to-tickets/SKILL.md]] and [[.agents/skills/to-feature-tickets/SKILL.md]] from that tree. [[.agents/skills/ask-matt/SKILL.md]] is a second full router: grill → spec → `to-tickets` → `implement`/`tdd`. It never names `to-feature-tickets` or [[.cursor/skills/implement-fsharp-feature/SKILL.md]]. Several engineering skills still say run [[.agents/skills/setup-matt-pocock-skills/SKILL.md]] first; this repo already has a local tracker.

**Why it confuses:** Ask-Matt currently skips Gambol adapters (a later-obedience punch list, not an open router choice). [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]] still forbids the locked skill home. Several skills still say run setup-matt (now an obsolete bootstrap).

**Disposition:** **This Project.** Router **locked:** Ask-Matt is the human advisor; [[.cursor/rules/gambol.mdc]] is the primary agent instruction file; they must agree. Skill home **locked:** repo-shared skills live in [[.agents/skills/]]; today's split is not the destination. Vendor merge/setup-matt bootstrap **locked obsolete** (Ask-Matt stays). Leftover: change prepare-agent-instruction-change to match the home lock (**later** obedience; do not edit that skill now). See [[plan/skills-cleanup/reports/lock-ask-matt-gambol-agree.md]], [[plan/skills-cleanup/reports/lock-tickets-home-wayfinder.md]], and [[plan/skills-cleanup/reports/lock-vendor-merge-obsolete.md]].

### 2. Two ticket skills, two issue templates

**Where:** [[.agents/skills/to-tickets/SKILL.md]] (tracer-bullet vertical slices) and [[.agents/skills/to-feature-tickets/SKILL.md]] (cohesive testable capabilities) copy the same publish path and almost the same template. [[.agents/skills/ask-matt/SKILL.md]] only knows `to-tickets`. [[.agents/skills/qa/SKILL.md]] files issues under `plan/` with a different body (what happened / expected), no `Status:` line, and no quiz before publish. [[.agents/skills/triage/SKILL.md]] assumes GitHub labels, `#42`, PRs, and a [[.agents/skills/triage/OUT-OF-SCOPE.md]] knowledge base at `.out-of-scope/` (that directory does not exist). [[.cursor/rules/environment.mdc]] forbids GitHub/GitLab issues.

**Why it confuses:** Same user ask (“break this into tickets” or “file a bug”) can yield two slice rules and two file shapes. Ask-Matt still names only `to-tickets` and still has a `/triage` on-ramp. QA still uses its own bug-file body.

**Disposition:** Ticket-skill pair **locked:** keep both (tracer-bullet and capability). Later destination: refactor the common core (shared publish path, template, tracker wiring) — not work now. Triage **locked delete:** [[.agents/skills/triage/SKILL.md]] is not used here; later remove the skill and pointers; do not delete it in this pass. QA **locked keep:** [[.agents/skills/qa/SKILL.md]] stays the conversational file-issues skill; do not rewrite it as a pointer. Ticket skills own the `/to-tickets` template. QA process stays; issues it files must be valid tracker files (structure already locked). See [[plan/skills-cleanup/reports/lock-tickets-home-wayfinder.md]], [[plan/skills-cleanup/reports/lock-delete-triage.md]], and [[plan/skills-cleanup/reports/lock-keep-qa.md]].

### 3. Three implement / test procedures

**Where:** [[.agents/skills/implement/SKILL.md]] points at `implement-fsharp-feature` and `/tdd`, then a full suite at the end. [[.agents/skills/tdd/SKILL.md]] is a generic red-green loop (JS examples; refactor is not in the loop). [[.cursor/rules/testing-workflow.mdc]] says write test, stop for review, pass, stop, refactor, stop. [[.cursor/skills/implement-fsharp-feature/SKILL.md]] plus [[.cursor/skills/add-shared-test/SKILL.md]] is the F# path (targeted `dotnet test`, Client compile gate, background `./scripts/test.sh`).

**Why it confuses:** Stop-for-review vs vendor loop vs F# gate. An agent can skip the Client compile gate or run the slow full suite at the wrong time.

**Disposition:** **Locked.** Entry is [[.agents/skills/implement/SKILL.md]] (`/implement`). Ditch [[.cursor/rules/testing-workflow.mdc]] later (do not delete it in this pass). [[.agents/skills/tdd/SKILL.md]], [[.cursor/skills/implement-fsharp-feature/SKILL.md]], and [[.cursor/skills/add-shared-test/SKILL.md]] stay as referenced augmentations; they must augment each other and `/implement`, not contradict or duplicate a second loop. Do not treat tdd as leftover-to-delete. See [[plan/skills-cleanup/reports/lock-implement-path.md]].

### 4. Wayfinder operations vs the live tracker (not the locked Status words)

**Where:** [[.agents/skills/wayfinder/SKILL.md]] still teaches GitHub-shaped ops: claim by assignee, `wayfinder:` labels, research findings on a throwaway `research/<name>` branch, default “run setup-matt if no tracker.” Live [[doc/agents/issue-tracker.md]] already says: local `plan/` files, `Type:` line, claim without changing Status, frontier = takeable Status + unblocked. Vendor [[.agents/skills/setup-matt-pocock-skills/issue-tracker-local.md]] still says claim/resolve on `Status:` and a “Fog” body. Wayfinder ticket types (`research` | `prototype` | `grilling` | `task`) are a **Type** axis; that is not the locked Status list.

**Why it confuses:** The agent follows the skill body and fights the tracker doc: extra branches, assignee claim, old Status verbs.

**Disposition:** **Locked.** Wayfinder must reference and not repeat [[doc/agents/issue-tracker.md]]. Wayfinder is a process (when to research, grill, or prototype; how to chart a destination). Issue-tracker is a structure and wins on claim, Type, Status, frontier, and file layout. Do not change the locked Status enum here. “Run setup-matt if no tracker” is obsolete. Vendor seed files under [[.agents/skills/setup-matt-pocock-skills/]] are leftover, not a second tracker (**later** stop pointing at them). Extra `research/` branches are forbidden by the Q5 lock. See [[plan/skills-cleanup/reports/lock-tickets-home-wayfinder.md]], [[plan/skills-cleanup/reports/lock-git-all-on-dev.md]], and [[plan/skills-cleanup/reports/lock-vendor-merge-obsolete.md]].

### 5. Git: three places vs extra branches vs remotes vs all-push block

**Where:** [[.cursor/skills/git-protocol/SKILL.md]] — only `dev` / `ready` / `master`; no extra long-lived places. [[.cursor/skills/git-share/SKILL.md]] — pull `ready` freely; push `ready` only with Alan’s approval. [[.cursor/rules/environment.mdc]] — Desktop agent does not run remotes unless the user asks; remotes are git-share, which the human invokes. Wayfinder wants `research/<name>` branches. [[.agents/skills/prototype/SKILL.md]] wants a throwaway `prototype/<name>` branch “out of main.” [[.cursor/skills/update-matt-skills/SKILL.md]] uses `vendor/mattpocock-skills` and `update/mattpocock-skills` while also saying follow git-protocol. [[.agents/skills/git-guardrails-claude-code/SKILL.md]] blocks every `git push`, including the approved [[scripts/gitpush.sh]] path.

**Why it confuses:** Research and prototype skills still teach throwaway topic branches. The obsolete vendor merge still names `vendor/` and `update/` places. Guardrails, if installed, block the share path.

**Disposition:** Extra places **locked:** all work on `dev`; no `research/`, `prototype/`, `vendor/`, or `update/`. Then `ready` / `master` per [[.cursor/skills/git-protocol/SKILL.md]]. Wayfinder research and prototype must not teach throwaway topic branches (**later** obedience). `vendor/mattpocock-skills` and `update/mattpocock-skills` are **dead** with the obsolete vendor merge (also forbidden by Q5). Remotes stay as already written in [[.cursor/rules/environment.mdc]] and [[.cursor/skills/git-share/SKILL.md]]. Guardrails: **ignore** unless Claude Code hooks are in use. See [[plan/skills-cleanup/reports/lock-git-all-on-dev.md]] and [[plan/skills-cleanup/reports/lock-vendor-merge-obsolete.md]].

### 6. Two glossaries, and ADR vs Committed Decision

**Where:** [[.agents/skills/domain-modeling/SKILL.md]] writes [[CONTEXT.md]] and records hard decisions as ADRs. [[.agents/skills/ubiquitous-language/SKILL.md]] writes UBIQUITOUS_LANGUAGE.md for the same job (description even says "domain model"). [[CONTEXT.md]] says always say **Committed Decision**, not ADR. Grill-with-docs, Ask-Matt, improve-codebase-architecture, and [[.agents/skills/domain-modeling/ADR-FORMAT.md]] still say ADR. Vendor [[.agents/skills/setup-matt-pocock-skills/domain.md]] still says ADR.

**Why it confuses:** Two glossary files. Wrong name for [[doc/Decisions/]].

**Disposition:** **Locked.** One glossary writer: [[.agents/skills/domain-modeling/SKILL.md]] writes [[CONTEXT.md]]. Always say **Committed Decision**, not ADR. [[.agents/skills/ubiquitous-language/SKILL.md]] is not a second glossary owner. Later obedience: ADR wording in skills → Committed Decision; do not create UBIQUITOUS_LANGUAGE.md. See [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]].

### 7. Planning stack overlap

**Where:** [[.agents/skills/wayfinder/SKILL.md]] — decisions, not deliverables. [[.agents/skills/to-spec/SKILL.md]] — no interview; “publish to the issue tracker” and apply `ready-for-agent` (a spec is not a ticket in [[doc/agents/issue-tracker.md]]; the spec path is spec.md). [[.agents/skills/request-refactor-plan/SKILL.md]] — interview, then a steps plan under `plan/` with a template close to to-spec. [[.cursor/skills/plan-roadmap-change/SKILL.md]] — still says align with [[doc/plan]], which does not exist; new work lives in Projects per [[.cursor/rules/planning-docs.mdc]]. Ask-Matt says after Wayfinder always go to-spec then to-tickets, never to-feature-tickets (now a Q1 agreement punch list: both ticket skills stay).

**Why it confuses:** to-spec may file a spec as a ticket. Ask-Matt still omits `to-feature-tickets`. request-refactor-plan still reads like a third linear planning skill.

**Disposition:** Ticket-skill pair **locked** (both stay). request-refactor-plan **locked** as a process like Wayfinder: it creates a new Project under `plan/` and charts it; it is not a third spec/ticket skill in a linear wayfinder→to-spec→tickets sequence. The new Project's structure follows [[doc/agents/issue-tracker.md]] and project-work (reference, do not repeat). Dead `doc/plan` pointer: **later** when that skill is edited. Ask-Matt naming both ticket skills is Q1 later obedience. See [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]].

### 8. Where subagent and handoff output goes

**Where:** [[.cursor/rules/core-agent-behavior.mdc]] — subagents write [[plan/]] `reports/`. [[.agents/skills/code-review/SKILL.md]] — two subagents, then chat headings. [[.agents/skills/research/SKILL.md]] — one Markdown file “where the repo keeps such notes,” else “somewhere sensible.” [[.agents/skills/handoff/SKILL.md]] — OS temp, not the workspace. [[.agents/skills/improve-codebase-architecture/SKILL.md]] — HTML in OS temp (Tailwind CDN). Wayfinder research still names a git branch (forbidden by the Q5 lock; **later** obedience). Core-agent-behavior also says use subagents; Wayfinder says do not delegate grilling (facts may be delegated).

**Why it confuses:** Reports scatter. Research and grilling disagree on what a subagent may do.

**Disposition:** Reports path **locked:** Gambol reports live under `plan/<slug>/reports/` unless a skill names another path (matches [[.cursor/rules/core-agent-behavior.mdc]]). Architecture review **locked** to that path (Markdown, not OS temp HTML). Grill-delegation leftover (Wayfinder: do not delegate grilling) is not a new Alan question. See [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]] and [[plan/skills-cleanup/reports/lock-architecture-review-reports-path.md]].

### 9. Grill family: which skill to invoke

**Where:** [[.agents/skills/grilling/SKILL.md]] is the interview primitive. [[.agents/skills/grill-me/SKILL.md]] is an alias. [[.agents/skills/grill-with-docs/SKILL.md]] adds domain-modeling. [[.agents/skills/loop-me/SKILL.md]] grills into `workflows/*.md`. Ask-Matt says use grill-me only when there is no working directory. [[.agents/skills/wait-what/SKILL.md]] is a re-pitch command; ticket templates also say “write in the style of wait-what.” Locked: grilling is a **method**, not a Stage or Status. [[.cursor/rules/project-stage.mdc]] and [[.cursor/skills/project-work/SKILL.md]] still treat `grilling` as a Stage directive (old tokens — **later** obedience).

**Why it confuses:** Four entry points. Wait-what is both a command and a prose style. Old Stage gates still fire on a word that is no longer a Stage.

**Disposition:** Default grill **locked:** [[.agents/skills/grill-me/SKILL.md]] is the default invoke in this repo. grilling, grill-with-docs, wait-what, and loop-me are not the default. Grilling remains a method, not a Stage. Old Stage copies: **later** (lists already locked). loop-me: **ignore** unless someone runs it here. Ask-Matt still prefers grill-with-docs in a working directory (Q1 later obedience). See [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]].

### 10. Dead pointers and foreign-stack skills

**Where:** [[.cursor/skills/plan-roadmap-change/SKILL.md]] → missing [[doc/plan]]. [[.agents/skills/code-review/SKILL.md]] looks for CODING_STANDARDS.md / CONTRIBUTING.md (absent; F# rules are [[.cursor/rules/fsharp-source.mdc]]). Writing-for-agents / setup-matt mention CLAUDE.md (absent). Triage `.out-of-scope/` absent. Vendor skills aimed at TypeScript/npm: [[.agents/skills/setup-ts-deep-modules/SKILL.md]], [[.agents/skills/migrate-to-shoehorn/SKILL.md]], [[.agents/skills/setup-pre-commit/SKILL.md]] (Husky). Prototype assumes `pnpm`/`bun`. [[.agents/skills/writing-for-agents/SKILL.md]] and [[.agents/skills/writing-great-skills/SKILL.md]] repeat the same writing theory. [[.agents/skills/design-an-interface/SKILL.md]] and [[.agents/skills/codebase-design/DESIGN-IT-TWICE.md]] are two “design it twice” procedures.

**Why it confuses:** Missing files look like a setup failure. TS skills can fire on “tests” or “packages.” Duplicate writing/design skills inflate choice.

**Disposition:** Dead pointers **this Project** as a punch list when those skills are edited. Vendor merge/setup-matt bootstrap is **obsolete** (not a second tracker; stop pointing at seed copies **later**; do not delete in this pass). Foreign-stack skills **ignore** unless they auto-invoke. Duplicate writing/design theory **later**. See [[plan/skills-cleanup/reports/lock-vendor-merge-obsolete.md]].

## Already locked (out of this chart)

Stage: `chart` | `spec` | `slice` | `build` | `done` | `dead`. Status is re-locked in [[plan/skills-cleanup/reports/lock-status-set-v2.md]] (`ready-for-agent` | `ready-for-human` | `needs-info` | `blocked` | `done` | `cancelled`). Grilling is a method. Archive is an action. Rework is a move back. Live copies of old tokens are **later** obedience. Do not re-grill the lists.

Mismatch while catching up: [[.cursor/skills/projects-overview/SKILL.md]] still sorts by the old Stage list. This Project is stamped `chart` as directed. Other Projects still show old Stage words on their `project.md` until a later ticket.

## What is already fine

- [[.cursor/skills/git-protocol/SKILL.md]], [[.cursor/skills/git-share/SKILL.md]], and [[.cursor/skills/git-master/SKILL.md]] point at each other and do not copy the full procedure (conflict is with *other* skills, not among these three).
- [[AGENTS.md]], [[.cursor/copilot-instructions.md]], and [[.cursor/codex-context.md]] stay thin.
- Gambol F# companions ([[.cursor/skills/implement-fsharp-feature/SKILL.md]], [[.cursor/skills/add-shared-test/SKILL.md]], [[.cursor/skills/investigate-fable-client/SKILL.md]], [[.agents/skills/code-review-fsharp/SKILL.md]]) stay as referenced augmentations of `/implement` (they must not copy a second loop).
- [[.agents/skills/resolving-merge-conflicts/SKILL.md]] states an explicit merge-in-progress exception to git-protocol.
- Writing-shape / writing-beats / writing-fragments are a separate writing pipeline, not a second implement path.
- Standalone skills (teach, wizard, roll-dice, obsidian-vault, azure, scaffold-exercises, scratch-script) do not compete for the idea→ship route.
- Canonical Stage docs match the Locked Stage list. Status docs ([[doc/agents/triage-labels.md]]) still copy the old set; catch-up is later ([[plan/skills-cleanup/reports/lock-status-set-v2.md]]).

## Next charting questions for Alan

Do not answer these by guess. A later grill or lock should.

**Locked (do not ask):**
1. Ask-Matt is the human advisor; [[.cursor/rules/gambol.mdc]] is the primary agent instruction file; they must agree. See [[plan/skills-cleanup/reports/lock-ask-matt-gambol-agree.md]].
2. Both ticket skills stay. Later: refactor the common core. See [[plan/skills-cleanup/reports/lock-tickets-home-wayfinder.md]].
3. Repo-shared skills live in [[.agents/skills/]]. Today's split is not the destination. Later: change [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]] to match.
4. Wayfinder is process and must reference, not repeat, [[doc/agents/issue-tracker.md]] (structure).
5. All work on `dev`. No extra long-lived places (`research/`, `prototype/`, `vendor/`, `update/`). See [[plan/skills-cleanup/reports/lock-git-all-on-dev.md]].
6. Vendor merge/flatten/setup-matt bootstrap is obsolete. Ask-Matt stays. See [[plan/skills-cleanup/reports/lock-vendor-merge-obsolete.md]].
7. Default grill entry is [[.agents/skills/grill-me/SKILL.md]]. See [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]].
8. Reports live under `plan/<slug>/reports/` unless a skill names another path.
9. [[.agents/skills/request-refactor-plan/SKILL.md]] is a process that creates and charts a new Project (like Wayfinder), not a third spec/ticket skill.
10. One glossary writer: [[.agents/skills/domain-modeling/SKILL.md]] writes [[CONTEXT.md]]. Always say Committed Decision, not ADR.
11. Entry implement is `/implement`. Ditch testing-workflow later. tdd and implement-fsharp-feature augment by reference. See [[plan/skills-cleanup/reports/lock-implement-path.md]].
12. Delete [[.agents/skills/triage/SKILL.md]] later. Do not delete it in this pass. See [[plan/skills-cleanup/reports/lock-delete-triage.md]].
13. Keep [[.agents/skills/qa/SKILL.md]]. Do not rewrite it as a pointer. See [[plan/skills-cleanup/reports/lock-keep-qa.md]].

**Still open:** none. Contradiction clusters are locked. Remaining work is later obedience (punch lists in the lock reports).

## This pass did not do

No skill or rule body edits. No re-lock of Status or Stage tokens. No tickets. No commit.
