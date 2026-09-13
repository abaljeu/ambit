---
name: to-arch
description: Design whole-feature architecture from the Project's spec and write it as arch.md — interactive design discussion, then an explicit layout the user can critique.
disable-model-invocation: true
---

This skill turns User Stories and map Decisions so far into a whole-feature architecture on the Project. Unlike [[.agents/skills/to-spec/SKILL.md]], work **with** the user: propose ideas in small bites, walk story paths together, and leave an explicit layout they can critique. Do not slice implementation tickets — that is [[.agents/skills/to-tickets/SKILL.md]].

## Process

1. Load the Project via [[.agents/skills/project-work/SKILL.md]]. Read `spec.md` (especially User Stories) and the Wayfinder map's Decisions so far when present. Done: you can name the Project slug, the stories, and settled map decisions.

2. Explore the codebase enough to name existing modules and seams. Prefer existing seams. Use [[.agents/skills/codebase-design/SKILL.md]] vocabulary (module, interface, seam, adapter, depth). Done: you can point at candidate modules and seams that already exist.

3. With the user, design the layout:
   - Trace each numbered User Story as a numbered Story path across modules, in order.
   - Read the module map off shared segments: segments every path shares are core modules; the narrowest point every path crosses is where the test seam goes.
   - Number and name each module map entry; later tickets cite modules by name per [[.agents/rules/refer-by-name.md]].
   - Capture Implementation Decisions and Testing Decisions as outcomes of the paths and seams — method, not a bullet dump.
   - Pick **Sequence** once for the Project: `tracer-cut` | `module-build` | `expand-contract` (modes in [[.agents/skills/to-tickets/SKILL.md]]). Prefer from the story paths and seams; confirm with the user.
   - For Alternative considered, design it twice when the arrangement is non-obvious: invoke [[.agents/skills/codebase-design/DESIGN-IT-TWICE.md]].
   - Contribute ideas proactively but small; keep the discussion interactive (HITL).
   - Escape hatch: when a path will not settle, record it under Unsettled only. Do not open Wayfinder map tickets for unsettled paths. Resolve those open questions during Stage `arch` (grill / research / prototype as needed, HITL), then clear or shrink Unsettled. Do not send Stage back to `chart` for unsettled items.

   Done: the user can critique Story paths, Module map, Seams, Sequence, Alternative considered, and any Unsettled items.

4. Write `arch.md` on the Project using the template below. Number every section and every list item; give each a name per [[.agents/rules/refer-by-name.md]]. Set `Stage: arch` and `Updated:` on `project.md` per [[doc/agents/project-status.md]]. The arch path is in [[doc/agents/issue-tracker.md]]. An arch is not a ticket and has no `**Status:**`. Done: `plan/<slug>/arch.md` exists and project Stage is `arch`.

5. Stop. Do not run [[.agents/skills/to-tickets/SKILL.md]].

<arch-template>

# <name> architecture

Spec: [[spec.md]]
Updated: <YYYY-MM-DD>
Sequence: <tracer-cut | module-build | expand-contract>

## 1. Story paths

Numbered list: each User Story (by its number and name) as the modules it crosses, in order.

## 2. Module map

Focus on deltas for this project.
Numbered-and-named modules. For each module:
   The state it defines.
   Its interface.
      What the interface does
   What modules it uses.

## 3. Seams

Where each interface lives. Which seam the tests cross.

## 4. Alternative considered

The other arrangement; why this one won.

## 5. Unsettled

Numbered list of paths that will not settle. Record only — resolve during Stage `arch` (grill / research / prototype); do not open Wayfinder map tickets; do not return Stage to `chart`.

</arch-template>
