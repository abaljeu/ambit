---
name: to-arch
description: Design whole-feature architecture from the Project's spec and publish it as arch.md for the user to critique.
disable-model-invocation: true
---

This skill turns User Stories and map Decisions so far into a whole-feature architecture on the Project. Publish the complete architecture immediately; the written file is the review surface. Do not slice implementation tickets — that is [[.agents/skills/to-tickets/SKILL.md]]. Write this skill per [[.agents/skills/writing-for-agents/SKILL.md]] (one normative home).

## Process

1. Load the Project via [[.agents/skills/project-work/SKILL.md]]. Read `spec.md` (especially User Stories) and the Wayfinder map's Decisions so far when present. Done: you can name the Project slug, the stories, and settled map decisions.

2. Explore the codebase enough to name existing modules and seams. Prefer existing seams. Use [[.agents/skills/codebase-design/SKILL.md]] vocabulary (module, interface, seam, adapter, depth). Done: you can point at candidate modules and seams that already exist.

3. Design the layout (canonical form rules for `arch.md` — the template below cites these; it does not restate them):
   - **Story paths:** Trace each numbered User Story as a numbered Story path, in order. Short title plus nested hops (one hop / one responsibility). Each hop names the module, door, or observable outcome so a reader can follow one story end-to-end — not Module map Interface detail (field shapes, validation, Uses). Not an arrow-chain (`A → B → C`) and not a prose paragraph.
   - **Shared segments / test seam:** After the stories, a short nested list of shared hop *sequences* (modules or doors crossed), not contract dumps. Segments every path shares are core modules; the narrowest point every path crosses is the test seam.
   - **Module map:** Number and name each module (tickets cite by name per [[.agents/rules/refer-by-name.md]]). Nested lists under each: State / Interface / Uses — never a prose paragraph packing those together. Sole home of State / Interface / Uses detail for the Project (field shapes, validation rules, Uses lists, event policy). Focus on deltas for this project.
   - **Seams:** Where each interface lives; which seam tests cross. One short role line, or `Interface on **Module**` — do not restate that module's Interface bullets. Prefer naming the owning module over `see §2.N` clutter; do not add a Shared contracts section that competes with Module map.
   - **Checklists:** Every build-tracking leaf is a numbered task (`1. [ ]` / `1. [x]`); no bare `- [ ]`. Restart numbering at 1 inside each nested list. Parents (stories, modules) are ordinary numbered items (`1. **Name**`) with numbered task children. Mark `[x]` only when the arch already records that work as done. Alternative considered and Unsettled stay ordinary numbered lists (design record, not a build checklist). Covers Story path hops, Module map State / Interface / Uses items, Seams, shared-segment and test-seam lists.
   - Capture Implementation Decisions and Testing Decisions as outcomes of the paths and seams — method, not a bullet dump.
   - Pick **Sequence** once for the Project: `tracer-cut` | `module-build` | `expand-contract` (modes in [[.agents/skills/to-tickets/SKILL.md]]). Derive it from the story paths and seams. [[.agents/skills/to-tickets/SKILL.md]] propagates this field — it does not re-choose.
   - For Alternative considered, design it twice when the arrangement is non-obvious: invoke [[.agents/skills/codebase-design/DESIGN-IT-TWICE.md]].
   - Escape hatch: when a path will not settle, record it under Unsettled only. Do not open Wayfinder map tickets for unsettled paths. Resolve during Stage `arch` (grill / research / prototype as needed), then clear or shrink Unsettled. Do not send Stage back to `chart`.
   - Status notes in arch cite tickets, sections, or Points — not git branch names ([[.agents/rules/planning-docs.md]]).

   Done: a complete draft exists for Story paths, Module map, Seams, Sequence, Alternative considered, and any Unsettled items.

4. Publish `arch.md` on the Project immediately using the template below; do not ask for approval first. Apply Process step 3 form rules. Number and name every section and list item per [[.agents/rules/refer-by-name.md]]. Set `Stage: arch` and `Updated:` on `project.md` per [[doc/agents/project-status.md]]. The arch path is in [[doc/agents/issue-tracker.md]]. An arch is not a ticket and has no `**Status:**`. Done: `plan/<slug>/arch.md` exists and project Stage is `arch`.

5. Stop. Do not run [[.agents/skills/to-tickets/SKILL.md]].

<arch-template>

# <name> architecture

Spec: [[spec.md]]
Updated: <YYYY-MM-DD>
Sequence: <tracer-cut | module-build | expand-contract>

## 1. Story paths

Per Process step 3 **Story paths**, **Shared segments / test seam**, and **Checklists**.

## 2. Module map

Per Process step 3 **Module map** and **Checklists**. Under each module: State, Interface, Uses.

## 3. Seams

Per Process step 3 **Seams** and **Checklists**.

## 4. Alternative considered

The other arrangement; why this one won.

## 5. Unsettled

Paths that will not settle — record only; resolve during Stage `arch` per Process step 3 escape hatch.

</arch-template>
