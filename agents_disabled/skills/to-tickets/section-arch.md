# Architecture

The behaviour this ticket adds. Each architecture decision sits on its checklist item.

Inputs: the ticket's one-line goal; the Project `spec.md` path; the Project `arch.md` path.
Depends: piece-goal.

Output: the checklist under `## What to build`.

1. Read the given `arch.md` and the goal output. Write one checklist item for each behaviour this ticket adds, from the user's view ([[.agents/skills/wait-what/SKILL.md]]). Write the architecture decision on that item. When Sequence is `tracer-cut`, name items by the modules the path crosses. When Sequence is `module-build`, name the capability. When Sequence is `expand-contract`, name the expand, migrate, or contract action this ticket performs. Do not add a `###` prose subsection. Paths and links follow [[PUBLISH.md]].

Done: What to build is one checklist. No item restates Context. No prose subsection restates an item.
