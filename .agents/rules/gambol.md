Canonical project rules are split across [[.agents/rules/]]. Cursor attaches them through thin stubs in [[.cursor/rules/]] (`alwaysApply` or `globs`).

- [[.agents/rules/core-agent-behavior.md]] — interaction, planning mindset, surgical changes
- [[.agents/rules/project-values.md]] — project aims and stack
- [[.agents/rules/environment.md]] — shell, paths, tooling
- [[.agents/rules/fsharp-source.md]] — F# conventions (scoped to *.fs)
- [[.agents/rules/core-api.md]] — Core vs Adapter pointers (scoped to Server Core / Adapter)
- [[.agents/rules/markdown-writing.md]] — markdown conventions (scoped to *.md)
- [[.agents/rules/planning-docs.md]] — roadmap/plan documents (scoped to doc/)
- [[.agents/rules/project-stage.md]] — keep each `plan/` project's `Stage:` current

Shared runtime state:

- [[plan/index.md]] — leftover snapshot left on disk; Stage of record is each `plan/<slug>/project.md`. Work is that project's issues/
- [[CONTEXT.md]] — concise domain glossary

Imported engineering skill configuration:

- [[doc/agents/issue-tracker.md]] — local Markdown issue tracker, shared language, and Wayfinder operations
- [[doc/agents/triage-labels.md]] — ticket Status list
- [[doc/agents/domain.md]] — canonical project docs under [[doc/]] and Committed Decisions under [[doc/Decisions/]]
- [[doc/agents/scope-vs-commitment.md]] — scope is effort-local; product commitments need an authorized record
- [[doc/agents/project-status.md]] — per-project `Stage:` vocabulary

Ask-Matt is the human advisor: [[.agents/skills/ask-matt/SKILL.md]]. This file is the primary Agent instruction file. Both remain. The job lists below match that skill in name and sequence. Default grill is `/grill-me`.

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

## On-ramps

- [[.agents/skills/qa/SKILL.md]] — conversational file-issues
- [[.agents/skills/diagnosing-bugs/SKILL.md]] — hard bugs with a tight feedback loop
- [[.agents/skills/wayfinder/SKILL.md]] — chart a large effort when the way is not yet visible
- [[.agents/skills/request-refactor-plan/SKILL.md]] — chart a new Feature-set Project for a refactor

## Vocabulary and health

- [[.agents/skills/domain-modeling/SKILL.md]] — glossary [[CONTEXT.md]] and Committed Decisions
- [[.agents/skills/codebase-design/SKILL.md]] — deep-module vocabulary
- [[.agents/skills/improve-codebase-architecture/SKILL.md]] — deepening opportunities

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

Tool-specific notes:

- Codex: [[.agents/codex-context.md]]
- Copilot: [[.agents/copilot-instructions.md]]
