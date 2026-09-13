# Planning skills

Stage: chart
Summary: Give agents a planning-code path that keeps plan/ layout and Wayfinder; to-spec refined (front half of Spec only); to-tickets has three Sequence modes and consumes arch.md; arch Stage and arch.md before slice; implement consumes arch when present. to-feature-tickets retired (redirect stub → to-tickets module-build).
Updated: 2026-09-13

## Homes

- [[plan/roadmap/epics/process-improvement.md]]

## Keep

- `plan/<slug>/` layout: `project.md`, `reports/`, `issues/`
- [[.agents/skills/wayfinder/SKILL.md|Wayfinder]] — Destination = end-user feature increment; Map may generate grilling/research/prototype tickets
- [[.agents/skills/to-spec/SKILL.md|to-spec]] front half → `spec.md` (Problem Statement, Solution, User Stories, Out of Scope) as the load list later sessions must carry
- [[.agents/skills/to-arch/SKILL.md|to-arch]] → `arch.md` — how: whole-feature design before implement so first-session implementers do not design seams locally; includes **Sequence**
- [[.agents/skills/to-tickets/SKILL.md|to-tickets]] consuming `arch.md` — one skill, three Sequence modes (`tracer-cut`, `module-build`, `expand-contract`); pick once per Project from arch (ask once if missing)
- [[.agents/skills/implement/SKILL.md|implement]] once tickets are good; consumes `arch.md` when present (Module map, Seams, Sequence) — no migration when absent

## Current transitions to replace

- [[.agents/skills/to-tickets/SKILL.md|to-tickets]] — modes landed; further refine only if ticket body shape needs work
- ~~[[.agents/skills/to-feature-tickets/SKILL.md|to-feature-tickets]]~~ — retired; stub redirects to to-tickets `module-build`

## Decisions so far

- **Stages:** Keep existing stages; add `arch` after `spec`. Chain: `chart → spec → arch → slice → build`.
- **`arch.md`:** Interactive whole-feature layout (HITL). Header field **Sequence:** `tracer-cut` | `module-build` | `expand-contract`. Sections: Story paths; Module map (numbered-and-named; each entry: state, interface / what it does, modules it uses); Seams (where interfaces live; which seam tests cross); Alternative considered; Unsettled. Tickets cite modules by name (per [[.agents/rules/refer-by-name.md]]).
- **to-tickets Sequence modes:** Pick once per Project (from arch; ask once if missing).
  - **tracer-cut** — ticket = visible end-user increment across modules; sections named by modules crossed (exemplar: [[plan/core-creation/issues/29-prove-testactor-hello.md|Prove TestActor hello]])
  - **module-build** — ticket = one cohesive testable module/capability (former to-feature-tickets)
  - **expand-contract** — wide mechanical change (expand, migrate batches, contract)
- **to-feature-tickets:** Retired. Stub at [[.agents/skills/to-feature-tickets/SKILL.md]] points at to-tickets `module-build`. Who writes Stage: only `/to-tickets` → `slice`.
- **to-spec splits:** Problem Statement, Solution, User Stories, Out of Scope stay in `spec.md`. Implementation Decisions and Testing Decisions move to `arch.md` as outcomes of paths and seams (method, not a bullet list).
- **Arch method:** Trace each User Story as a path across modules; read the module map off shared segments. Segments every path shares are core modules. The narrowest point every path crosses is where the test seam goes. Design Alternative considered twice when the arrangement is non-obvious. Paths that will not settle go under Unsettled (grill/prototype before slice).
- **Unsettled → record only:** [[.agents/skills/to-arch/SKILL.md|to-arch]] records unsettled paths in Unsettled; it does not create Wayfinder map tickets for them. Resolving those open questions is part of Stage `arch` (grill / research / prototype as needed, HITL), then clear or shrink Unsettled — do not bounce Stage back to `chart` for that work.
- **Numbering:** Agent-produced planning artifacts number and name projects, issues, sections, and list items (per [[.agents/rules/refer-by-name.md]]).
