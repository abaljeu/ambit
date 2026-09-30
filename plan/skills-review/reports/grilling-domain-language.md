# Code review: Grilling and domain language

Range: `.agents/skills/{grilling,grill-me,batch-grill-me,grill-with-docs,domain-modeling,ubiquitous-language,loop-me}` — **clean tip** (current file contents; `git diff HEAD --` those paths empty). Unrelated dirty files ignored. scan: none.

## Standards

1. **One normative home** — writing-for-agents Gates. `batch-grill-me/SKILL.md` restates grilling’s design-tree / frontier / facts body; `grill-me` already does `Run a /grilling session.` Batch also drops grilling’s 2000-char cap and Q format — a second, thinner home.
2. **One normative home** — Committed-Decision triad restated in `domain-modeling/SKILL.md` (“Offer Committed Decisions sparingly”) and `COMMITTED-DECISION-FORMAT.md` (“When to offer…”).
3. **One normative home** + structure vs method — Repo layout / single-vs-multi-context / lazy create in `domain-modeling/SKILL.md` (“File structure”) and again in `CONTEXT-FORMAT.md`. Structure belongs in FORMAT; SKILL keeps method.
4. **One normative home** — Glossary rules in `ubiquitous-language/SKILL.md` Rules restate `CONTEXT-FORMAT.md` Rules.
5. **Checkable Done** — writing-for-agents Recipe §3. `ubiquitous-language` Process steps 1–6 and Re-running 1–6 have no `Done:` criteria.

Smells (judgement): **Duplicated Code / Overcomplication** — `batch-grill-me` body; simpler: thin pointer to `/grilling` like `grill-me`. **Overcomplication** — domain-modeling SKILL file trees; simpler: point at CONTEXT-FORMAT for layout.

## Spec

Spec: [[plan/skills-cleanup/spec.md]].

1. **(c)** `grill-with-docs/SKILL.md` description still says “ADR's and glossary.” Spec Decision 12 / US 20: “Always say Committed Decision, not ADR.”

No other Spec findings at tip for this cluster (US 16 / Decision 9: `/grill-me` default; Decision 12 / US 20 hold in domain-modeling and ubiquitous-language; loop-me Out of Scope).

Standards: 5 findings (worst: batch-grill-me second home for grilling). Spec: 1 finding (worst: grill-with-docs still says ADR).
