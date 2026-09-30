# Nine review groups, plus a standalone list. Skills that call each other are ordered so the entry skill comes first.

## Git places and sharing
`git-protocol` → `git-share` → `git-master` → `cloud-agent-git` → `resolving-merge-conflicts` → `git-guardrails-claude-code`

These share the Desktop places (`dev`, `ready`, `master`, `staging`). The others cite `git-protocol`. Publish, squash, and cloud paths sit in `git-share`, `git-master`, and `cloud-agent-git`. Conflict resolution and Claude hooks are the exceptions that still point at the protocol.

`git-guardrails-claude-code` installs hooks that block pushes the protocol otherwise allows.

## Plan project pipeline
`project-work` → `wayfinder` → `request-refactor-plan` → `research` → `prototype` → `to-spec` → `to-arch` → `to-tickets` → `to-feature-tickets` → `to-archive` → `plan-or-doc-change`

Adjacent: `qa` (files issues under `plan/`, and says not to copy `to-tickets/PUBLISH.md`).

Shared lifecycle and Stage under `plan/<slug>/`. The explicit chain is `to-spec` → `to-arch` → `to-tickets`, with human stops between them. `wayfinder` and `request-refactor-plan` chart maps. Wayfinder invokes `/research`, `/prototype`, and grilling. `plan-or-doc-change` is the layer recipe from map through spec, architecture, ticket, and code.

`to-feature-tickets` is a retired stub that points at `to-tickets` and Sequence `module-build`. `request-refactor-plan` overlaps Wayfinder’s charting and owns the “tiny steps” grain.

## Grilling and domain language
`grilling` → `grill-me` → `batch-grill-me` → `grill-with-docs` → `domain-modeling` → `ubiquitous-language`

Adjacent: `loop-me` (grilling for personal `workflows/*.md`, outside the Gambol plan).

One interview discipline, with thin entry wrappers. `grill-with-docs` is grilling plus `domain-modeling`. `ubiquitous-language` proposes glossary terms; `domain-modeling` writes `CONTEXT.md`.

`grill-me`, `batch-grill-me`, and `grilling` likely duplicate body text, so look for one normative home. The default grill named from `ask-matt` and `request-refactor-plan` is `/grill-me`.

## Deep-module design
`codebase-design` → `improve-codebase-architecture` → `setup-ts-deep-modules`

Shared vocabulary for modules, seams, and depth. `improve-codebase-architecture` consults `codebase-design`, then grills a candidate. `setup-ts-deep-modules` enforces the same idea with dependency-cruiser. Also referenced from `to-arch`, `tdd`, and the diagnose handoff.

`codebase-design` is consulted, not run as a session. `improve-codebase-architecture` is a session that then grills.

## Ticket build, F#, TDD, review
`implement` → `tdd` → `implement-fsharp-feature` → `add-shared-test` → `write-simple-parser` → `investigate-fable-client` → `code-review` → `code-review-fsharp`

Adjacent: `diagnosing-bugs` (bisect via `git-protocol`; may hand off to `improve-codebase-architecture`).

`implement` is the ticket-build entry. It loads `project-work`, then the F# and test skills and `tdd`, and ends with `code-review` while the full suite runs. `code-review-fsharp` is invoked by the `code-review` standards scan; use it alone only for `--fn`, `--range`, or `--usage`. `write-simple-parser` pairs with F# implement and Shared tests. Client investigation escalates from F# implement.

The entry is `implement`. Refactoring is called out as a review-stage concern. Review has two surfaces: the agent skill and the human smell baseline.

## Product docs and format plans
`plan-roadmap-change` → `maintain-doc-currency` → `co-edit-format-plan`

Roadmap and architecture slices under `doc/`, currency audits, and interactive format-plan drafting. The format-plan skill pairs with the other two.

This group is distinct from `plan-or-doc-change` (the plan Project stack) and from agent-instruction docs.

## Authoring agent instructions
`writing-for-agents` → `prepare-agent-instruction-change`

How to write skills and bridges, and how to edit `.agents/rules`, skills, and tool bridges without duplication. `prepare-agent-instruction-change` requires writing skills per `writing-for-agents`.

## Article writing
`writing-fragments` → `writing-beats` → `writing-shape` → `edit-article`

Explore in `writing-fragments`, then two exploit paths (beat journey or paragraph shaping), then revise. Fragments leaves structure to the exploit skills.

`writing-beats` and `writing-shape` both claim the exploit step, so review which path is intended when.

## Session handoff
`handoff` → `claude-handoff`

Same job: a compact summary plus suggested skills. One writes a temp file; the other launches `claude --bg`.

## Standalone
- `ask-matt` — catalog router over all skills
- `wait-what` — one-shot re-pitch
- `to-questionnaire` — async questionnaire for a third party
- `teach` — multi-session teaching workspace
- `wizard` — human-only bash setup scripts
- `scratch-script` — write-then-run a temporary shell script
- `azure` — Azure CLI hygiene
- `setup-pre-commit` — Husky and lint-staged bootstrap
- `migrate-to-shoehorn` — TypeScript test `as` to shoehorn
- `scaffold-exercises` — course exercise stubs
- `obsidian-vault` — external vault operations
- `roll-dice` — dice notation