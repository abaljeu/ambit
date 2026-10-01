# Code review: Product docs and format plans

**Range:** `.agents/skills/plan-roadmap-change`, `.agents/skills/maintain-doc-currency`, `.agents/skills/co-edit-format-plan` — clean tip (empty `git diff HEAD --` those paths); current file contents. `scan: none`.

## Standards

**writing-for-agents Recipe §3 (checkable Done)** — Hard. [plan-roadmap-change](.agents/skills/plan-roadmap-change/SKILL.md) Workflow 1–5 and [maintain-doc-currency](.agents/skills/maintain-doc-currency/SKILL.md) Currency Workflow 1–7 have no Done lines. [co-edit-format-plan](.agents/skills/co-edit-format-plan/SKILL.md) is a procedure with no ordered steps/Done.

**writing-for-agents prune / cache + One normative home** — Hard. maintain-doc-currency names [[doc/README.md]] as SoT then caches it:

> `- Top-level [[doc/]] docs are front-door docs…` through `- [[doc/unsorted/]] is temporary…`

Simpler: delete Directory Fit; keep the README pointer.

Same skill’s “assume the current doc wins…” restates README’s Authority rule — cite README only.

**One normative home** — Hard. plan-roadmap-change L8 Follow [[planning-docs.md]], then Workflow 2 “Follow [[.agents/rules/planning-docs.md]]” again.

**cache** — Hard. co-edit-format-plan L8 Follow markdown-writing, then L39: `Use [[wikilinks]]; one blank line between blocks; no hard-wrapped paragraphs.` Drop L39.

**One normative home** — Hard. co-edit “**Planning only**…” vs Do not “Implement `src/`…”; “Reference vs active plan” vs Do not “Edit finished plans…”. Keep one home each.

**Prompt the positive** — Hard. plan-roadmap Do not; co-edit Do not list. Pair each ban with the target behaviour (or drop if the positive already exists).

**Overcomplication** (judgement) — co-edit:

> `## Plan file skeleton` / `Use these headings…` (no headings)

Delete the empty section or list the headings.

**Overcomplication** (judgement) — maintain-doc Finishing Checklist restates Currency Workflow. Drop the checklist; put Done on each workflow step.

**Overcomplication** (judgement) — co-edit Interaction: “Implicit license…”, “Expect changes…”, “Small corrective edits…”, “one section…” — collapse to one rule: one section per turn, small focused edits, summarize actions.

## Spec

no spec available

Standards: 9 findings (worst: missing checkable Done on workflow steps). Spec: 0 (no spec available).
