# Boundary vs End-user wiki and Committed Decisions

**Type:** grilling
**Status:** done
Blocked by:

## Question

What is in the architecture wiki vs [[plan/end-user-wiki/map.md]] (operation) vs Committed Decisions under [[doc/Decisions/]]? Draw the boundary. Do not write pages in this ticket.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md. Map: [[../map.md]].
- 2026-09-30 — Answer recorded from Alan.

## Answer

Four linked corpora: markdown for how it works, markdown for how to use, markdown for what to change, and code.

The architecture wiki is how it works. It is the wiki that [[doc/current/]] is becoming, and [[doc/current/arch.md]] is part of it. It holds what Is and what Should Become. That scope is broader than the Core seam. An outline is a fair name at some level, and too small. The wiki is not a checklist tree.

Alan's marks for a claim, not a formal spec: `[ ]` planned, `[/]` started, `[x]` implemented, `[o]` obsolete yet implemented. An `[o]` claim is written with the `[ ]` claim that retires it. When that new claim is `[x]`, remove the `[o]` claim.

How to use stays the [End-user wiki](plan/end-user-wiki/map.md). The architecture wiki does not restate how to use.

A Committed Decision stays under [[doc/Decisions/]]. The architecture wiki links the decision. It does not restate the decision. It does not become a second copy.

Plans are what to change. A plan is a window on the wiki. It focuses on one aspect. It points at the wiki. It is not a second home for the idea. [core-refinement architecture](plan/core-refinement/arch.md) is the wide window, because that project is the whole Core seam. That file holds what Is, what Should Become, and the path. The path stays with the effort. After the wiki holds what Is and what Should Become, the plan remains how to get there. A task of that effort is to update the arch documents so they match those aims. The outcome is restructured code, and the standing arch documents brought in line with the aims. That file stays sole authority for what will be coded on the Core seam. [Server Core](plan/architecture/server-core.md) stays the compact description. This adds the wiki as the broader home for Is and Should Become. It keeps the 2026-09-30 sole-authority note.

This Project does not change how the software runs.
