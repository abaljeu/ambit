# Code review: Plan project pipeline skill cluster (clean tip / HEAD contents)

Range: `.agents/skills/{project-work,wayfinder,request-refactor-plan,research,prototype,to-spec,to-arch,to-tickets,to-feature-tickets,to-archive,plan-or-doc-change,qa}` — uncommitted diff empty; tip under review is current file contents. Report path `plan/skills-review/reports/plan-project-pipeline.md` under Project [[plan/skills-review/]].

## Standards

### Documented-standard violations

**writing-for-agents/SKILL.md Recipe §3** — each step needs a checkable Done. No `Done:` / `Done when` on steps in:
- project-work/SKILL.md §§1–4 (esp. §4 “Finish as **done**.”)
- research/SKILL.md jobs 1–3
- wayfinder/SKILL.md Chart / Work steps

**writing-for-agents/SKILL.md Gates — structure vs method wholly separate documents**
- wayfinder: map template + Ticket Types + Fog + Chart/Work in one file
- to-spec/SKILL.md and to-arch/SKILL.md: Process and `<…-template>` together (unlike to-tickets → PUBLISH.md)

**writing-for-agents/SKILL.md Gates — One normative home**
- to-tickets/SKILL.md §4 “Checklists use unnumbered tasks” vs PUBLISH template `1. [ ]` and to-arch Process §3 Checklists (numbered leaves, no bare `- [ ]`)
- project-work §3 restates Time / Status / Started / Finished that [[doc/agents/issue-tracker.md]] owns — pointer only

**writing-for-agents Recipe §5 / GLOSSARY No-Op**
- to-arch lead: `Write this skill per [[…/writing-for-agents/SKILL.md]] (one normative home).` — authorship meta; does not steer a run

**writing-for-agents Gates / WHY Negation — prompt the positive**
- prototype/LOGIC.md Anti-patterns: six `Don't …` lines that restate Process positives

### Smells (judgement)

**Overcomplication** — request-refactor-plan/SKILL.md §§1–7 chart destination/options/scope/fog, then §8: `Follow [[…/wayfinder/SKILL.md]] Chart the map`. Keep only refactor-specific interview (steps grain, coverage); chart via the Wayfinder pointer alone.

**Middle Man / Overcomplication** — to-feature-tickets/SKILL.md: retired stub → to-tickets `module-build`. Delete the skill; leave callers on `/to-tickets`.

scan: none

## Spec

no spec available

Standards: 7 findings (worst: missing checkable Done across project-work, research, wayfinder). Spec: 0 (no spec available).
