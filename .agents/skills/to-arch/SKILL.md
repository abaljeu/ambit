---
name: to-arch
description: Design whole-feature architecture from the Project's spec and publish it as arch.md for the user to critique.
disable-model-invocation: true
---

This skill turns User Stories and map Decisions so far into a whole-feature architecture on the Project. Publish the complete architecture immediately; the written file is the review surface. Do not slice implementation tickets — that is [[.agents/skills/to-tickets/SKILL.md]].

## Process

1. Load the Project via [[.agents/skills/project-work/SKILL.md]]. Read `spec.md` (especially User Stories) and the Wayfinder map's Decisions so far when present. Done: you can name the Project slug, the stories, and settled map decisions.

2. Explore the codebase enough to name existing modules and seams. Prefer existing seams. Use [[.agents/skills/codebase-design/SKILL.md]] vocabulary (module, interface, seam, adapter, depth). Done: you can point at candidate modules and seams that already exist.

3. Design the layout (canonical form rules for `arch.md` — [[ARCH.md]] cites these; it does not restate them):
   - **Committed only:** Put only committed plan detail into Story paths, Shared segments / test seam, Module map, and Seams. Committed means a Wayfinder map Decision, or another settled lock for this Project (not scope-only chat conjecture). Uncommitted or still-grilling detail goes under Unsettled only — do not write it into those sections, and do not invent checklist leaves for it.
   - **Story paths:** Trace each numbered User Story as a numbered Story path, in order. Short title plus nested hops (one hop / one responsibility). Each hop names the module, door, or observable outcome so a reader can follow one story end-to-end — not Module map Interface detail (field shapes, validation, Uses). Not an arrow-chain (`A → B → C`) and not a prose paragraph.

      This path is the implementation sequence.  It may be organized according to tracer-cut, module-build, or expand-contract strategies.
   - **Shared segments / test seam:** After the stories, a short nested list of shared hop *sequences* (modules or doors crossed), not contract dumps. Segments every path shares are core modules; the narrowest point every path crosses is the test seam.
   - **Module map:** Number and name each module (tickets cite by name per [[.agents/rules/refer-by-name.md]]). Name the file that will hold the module.  Nested lists under each: State / Interface / Uses — never a prose paragraph packing those together. Sole home of State / Interface / Uses detail for the Project (field shapes, validation rules, Uses lists, event policy).  The map must be declarative: State what the architecture should become, not actions to take.  (If a name changes, identify the new in terms of the old.)  Focus on deltas for this project, by ignoring things that don't change.
   - **Seams:** Where each interface lives; which seam tests cross. One short role line, or `Interface on **Module**` — do not restate that module's Interface bullets. Prefer naming the owning module over `see §2.N` clutter; do not add a Shared contracts section that competes with Module map.
   - **Checklists:** Leaf marks per [[.agents/skills/to-tickets/PUBLISH.md]]. Restart numbering at 1 inside each nested list. Parents (stories, modules) are ordinary numbered items (`1. **Name**`) with numbered task children. Mark `[x]` only when the arch already records that work as done. Alternative considered and Unsettled stay ordinary numbered lists (design record, not a build checklist). Covers Story path hops, Module map State / Interface / Uses items, Seams, shared-segment and test-seam lists.
   - Capture Implementation Decisions and Testing Decisions as outcomes of the paths and seams — method, not a bullet dump.
   - Pick **Sequence** once for the Project: `tracer-cut` | `module-build` | `expand-contract` (modes in [[.agents/skills/to-tickets/SKILL.md]]). Derive it from the story paths and seams. [[.agents/skills/to-tickets/SKILL.md]] propagates this field — it does not re-choose.
   - For Alternative considered, design it twice when the arrangement is non-obvious: invoke [[.agents/skills/codebase-design/DESIGN-IT-TWICE.md]].
   - Escape hatch: when a path will not settle, record it under Unsettled only. Do not open Wayfinder map tickets for unsettled paths. Resolve during Stage `arch` (grill / research / prototype as needed), then clear or shrink Unsettled. Do not send Stage back to `chart`.
   - Status notes in arch cite tickets, sections, or Points — not git branch names ([[.agents/rules/planning-docs.md]]).

   Done: a complete draft exists for Story paths, Module map, Seams, Sequence, Alternative considered, and any Unsettled items; no uncommitted detail sits outside Unsettled.

4. Publish `arch.md` on the Project immediately using [[ARCH.md]]; do not ask for approval first. Apply Process step 3 form rules. Number and name every section and list item per [[.agents/rules/refer-by-name.md]]. Set `Stage: arch` and `Updated:` on `project.md` per [[doc/agents/project-status.md]]. The arch path is in [[doc/agents/issue-tracker.md]]. An arch is not a ticket and has no `**Status:**`. Done: `plan/<slug>/arch.md` exists and project Stage is `arch`.

5. Bring central architecture and the project plan inline ([[doc/current/]], claim marks on [[doc/current/arch.md]] Claims; currency via [[.agents/skills/maintain-doc-currency/SKILL.md]]; do not treat plan material as product commitment without promotion per [[doc/agents/scope-vs-commitment.md]]):
   - For each **committed** code element in the Module map (and other committed build sections), ensure a subject home under [[doc/current/]] (create or update the page). Mark new Should Become claims `[ ]` until coded, then `[x]` when the arch already records them done.
   - When a committed Should Become contradicts a live `[x]` claim on current, remark that claim `[o]` and pair it with the retiring `[ ]` (remove the `[o]` only when that pair is `[x]`).
   - Link **from** `plan/<slug>/arch.md` **to** the `doc/current/` subject pages (current is the lasting home). Do not make current a permanent index of every project `arch.md`; a short path cite on the current page is optional only while that effort's Should Become is open.
   - Skip Unsettled and any other uncommitted plan detail — do not write it into `doc/current/`.
   Done: every committed Module map element has a current home and a link from the project arch; contradictory completed claims are `[o]` or cleared; uncommitted detail unchanged in current.

6. Stop. Do not run [[.agents/skills/to-tickets/SKILL.md]]. Done: no tickets were written in this run.
