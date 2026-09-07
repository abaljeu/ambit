---
name: ask-matt
description: Ask which skill or flow fits your situation. A router over the skills in this repo.
disable-model-invocation: true
---

# Ask Matt

You do not remember every skill, so ask. This file is the human advisor. [[.agents/rules/gambol.md]] is the primary Agent instruction file. Both remain. The job lists below match that file in name and sequence. Default grill is `/grill-me`.

Shared runtime state:

- [[plan/index.md]] — leftover snapshot left on disk; Stage of record is each `plan/<slug>/project.md`. Work is that project's issues/
- [[CONTEXT.md]] — concise domain glossary

Imported engineering skill configuration:

- [[doc/agents/issue-tracker.md]] — local Markdown issue tracker, shared language, and Wayfinder operations
- [[doc/agents/triage-labels.md]] — ticket Status list
- [[doc/agents/domain.md]] — canonical project docs under [[doc/]] and Committed Decisions under [[doc/Decisions/]]
- [[doc/agents/scope-vs-commitment.md]] — scope is effort-local; product commitments need an authorized record
- [[doc/agents/project-status.md]] — per-project `Stage:` vocabulary

A **flow** is a path through the skills. Most paths run along **idea to ship**. On-ramps merge onto it. Gambol adapters apply on every path. Do not skip them.

Workflow skills in [[.agents/skills/]]:

## Idea to ship

- [[.agents/skills/grill-me/SKILL.md]] — default grill (`/grill-me`)
- [[.agents/skills/prototype/SKILL.md]] — throwaway code that answers one design question; capture on `dev`
- [[.agents/skills/to-spec/SKILL.md]] — publish spec.md on the Project
- [[.agents/skills/to-tickets/SKILL.md]] — tracer-bullet vertical slices
- [[.agents/skills/to-feature-tickets/SKILL.md]] — cohesive testable capabilities
- [[.agents/skills/implement/SKILL.md]] — ticket-build entry (`/implement`)
- [[.agents/skills/tdd/SKILL.md]] — red-green at pre-agreed seams; augments `/implement`
- [[.agents/skills/implement-fsharp-feature/SKILL.md]] — F# layout and Client compile gate; augments `/implement`
- [[.agents/skills/add-shared-test/SKILL.md]] — Shared.Tests coverage; augments `/implement`
- [[.agents/skills/code-review/SKILL.md]] — Standards and Spec review of a diff

Start with `/grill-me`. If a question needs a runnable answer (state, business logic, a UI you have to see), detour through `/prototype`, bridged by `/handoff` in both directions. Capture the prototype on `dev`, then `ready` / `master`.

Multi-session build: `/to-spec` (spec.md on the Project, not a ticket), then `/to-tickets` or `/to-feature-tickets`, each ticket declaring its **blocking edges**. Work blockers-first. `/implement` per ticket, `/clear`ing context between each one. Same-session build: `/implement` in this window.

Either way, `/implement` is the entry. It drives `/tdd` at pre-agreed seams, then `/code-review` (Standards + Spec) before commit. Reach for `/tdd` alone when you want one behaviour test-first. Reach for `/code-review` alone to review a diff.

Keep grill, spec, and tickets in **one unbroken context window**. Each `/implement` starts fresh from the ticket. The limit is the [smart zone](https://www.aihero.dev/ai-coding-dictionary/smart-zone) (~150k tokens). If a session approaches it before tickets, `/compact` at the nearest phase boundary.

## On-ramps

- [[.agents/skills/qa/SKILL.md]] — conversational file-issues
- [[.agents/skills/diagnosing-bugs/SKILL.md]] — hard bugs with a tight feedback loop
- [[.agents/skills/wayfinder/SKILL.md]] — chart a large effort when the way is not yet visible
- [[.agents/skills/request-refactor-plan/SKILL.md]] — chart a new Feature-set Project for a refactor

**Bugs and requests that arrive raw** → `/qa`. It files valid tracker issues. `/implement` later picks them up. Do not run `/qa` on tickets a ticket skill already produced.

**Something is broken** → `/diagnosing-bugs`. For the hard ones: it refuses to theorise until it has a tight feedback loop, then fixes with a regression test. Its post-mortem hands off to `/improve-codebase-architecture` when there is no good seam.

**A huge effort, too big for one session** → `/wayfinder`. It charts a shared map of **decision tickets** and resolves them one at a time until the way is clear. Then it hands off: merge onto idea to ship at `/to-spec`, then a ticket skill and `/implement`. Do not loop the map straight into `/implement` unless the effort is small.

**A refactor to chart** → `/request-refactor-plan`. Like Wayfinder, it creates and charts a Feature-set Project. It is not a third spec or ticket skill in the idea-to-ship line.

## Vocabulary and health

- [[.agents/skills/domain-modeling/SKILL.md]] — glossary [[CONTEXT.md]] and Committed Decisions
- [[.agents/skills/codebase-design/SKILL.md]] — deep-module vocabulary
- [[.agents/skills/improve-codebase-architecture/SKILL.md]] — deepening opportunities

`/improve-codebase-architecture` surfaces **deepening opportunities**. Picking one generates an idea you take into `/grill-me`. `/codebase-design` is the bench for the chosen shape. `/domain-modeling` sharpens domain language and records a hard-to-reverse choice as a Committed Decision. Reach for these when the **words** or the **shape** are the problem; or let the skills above pull them in.

## Gambol adapters

- [[.agents/skills/git-protocol/SKILL.md]] — git procedure (`dev`, `ready`, `master`)
- [[.agents/skills/git-master/SKILL.md]] — squash and publish `master`; explicit invocation only
- [[.agents/skills/git-share/SKILL.md]] — pull `ready`, catch up; push `ready` only with Alan's approval
- [[.agents/skills/project-work/SKILL.md]] — `plan` project files and Stage
- [[.agents/skills/plan-roadmap-change/SKILL.md]] — roadmap and plan doc changes
- [[.agents/skills/investigate-fable-client/SKILL.md]] — Fable client investigation
- [[.agents/skills/maintain-doc-currency/SKILL.md]] — doc placement and currency
- [[.agents/skills/co-edit-format-plan/SKILL.md]] — collaborative (interactive) document editing
- [[.agents/skills/prepare-agent-instruction-change/SKILL.md]] — maintain rules/skills/bridges
- [[.agents/skills/write-simple-parser/SKILL.md]] — simple document parsers / format codecs
- [[.agents/skills/scratch-script/SKILL.md]] — scratch a short .sh with Write, then run that file
- [[.agents/skills/to-archive/SKILL.md]] — move a `done` project into [[plan/done/]]

These apply on every path. Do not skip them. Git work stays on `dev`, then `ready` / `master`.

## Standalone

- [[.agents/skills/grill-with-docs/SKILL.md]] — stateful grill; not the default invoke
- [[.agents/skills/grilling/SKILL.md]] — interview primitive
- [[.agents/skills/handoff/SKILL.md]] — portable markdown for a new harness, directory, or colleague
- [[.agents/skills/resolving-merge-conflicts/SKILL.md]] — in-progress merge or rebase conflict
- [[.agents/skills/research/SKILL.md]] — background reading; write under `plan/<slug>/reports/`
- [[.agents/skills/to-questionnaire/SKILL.md]] — questionnaire for someone else to fill
- [[.agents/skills/wizard/SKILL.md]] — steps only a human can take
- [[.agents/skills/wait-what/SKILL.md]] — re-pitch a message that did not land
- [[.agents/skills/teach/SKILL.md]] — learn a concept over multiple sessions
- [[.agents/skills/writing-for-agents/SKILL.md]] — writing documents that agents consume
- [[.agents/skills/projects-overview/SKILL.md]] — retired; leave [[plan/index.md]] as a leftover snapshot

`/grill-me` is the default grill. `/grill-with-docs` leaves a paper trail in [[CONTEXT.md]]; it is not the default invoke. `/grilling` is the primitive: rounds, the frontier, facts are the agent's job and decisions are yours. `/grill-me` and `/grill-with-docs` are named ways in; `/wayfinder` and `/improve-codebase-architecture` run it internally. Reach for `/grilling` directly only when you want the interview with no wrapper.

Research feeds `/grill-me` or `/to-spec`. It does not replace them. `/wait-what` works after a message did not land; `/grill-me` is the upfront cure. `/wizard` is for steps only a human can take. If the agent can do the step, it should.

## Phase boundaries

A **phase** is a chunk of work inside a session — the grilling, the implementation, the QA. At the **boundary** between two of them you have five options:

- **Continue** — stay put. Costs nothing, loses nothing.
- **`/clear`** — empty the window, when nothing here matters to what is next.
- **`/handoff`** — write a portable markdown file. Narrow: only for a **new harness**, a **new directory**, a **colleague**, or forking a side task **mid-phase**.
- **Subagent** — send a tightly-scoped task to its own window and get a report back.
- **`/compact`** — compress this context and seed a fresh session with it. The **default**, at the bottom of the tree rather than the first reach.

Read [[PHASE-BOUNDARIES.md]] for the ordered tree. Make the decision **at** a boundary; mid-phase, continue or split the rest into subagents.
