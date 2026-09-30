---
name: maintain-doc-currency
description: Maintains doc/ currency by auditing document placement, contradictions, stale roadmap material, and redundant coverage. Use when updating docs, moving docs between doc/ subdirs, reviewing doc accuracy, or reconciling implemented work with roadmap/current/reference/history docs.
---

# Maintain Doc Currency

Follow [[.agents/rules/markdown-writing.md]] and read [[doc/README.md]] first. Treat [[doc/README.md]] as the source of truth for what each documentation subdir means and which doc wins on conflict.

## Currency Workflow

Before promoting any exclusion or "Gambol does not …" into `doc/`, confirm it is a product **commitment** with an authorized source — not **scope** or **surmise** from a Project. See [[doc/agents/scope-vs-commitment.md]].

1. Read the relevant current docs before changing roadmap, history, or unsorted material.
   Done: every current doc that bears on the change has been read.
2. Check the Feature index [[doc/index.md]] for current-program coverage.
   Done: Feature index coverage for the touched area is checked.
3. Check whether implemented behavior is still described as future work; if so, move the durable truth into current or reference docs and reduce leftover roadmap text, or cite it from a `plan` Project.
   Done: each implemented-as-future case is moved, reduced, or Project-cited — or none found.
4. Update the Feature index when a change affects what is current. Planned work is not sequenced there.
   Done: Feature index matches current docs for the change, with no planned-work sequencing added.
5. Check whether roadmap commitments became obsolete; mark the mismatch and ask before deleting or rewriting direction.
   Done: each obsolete roadmap mismatch is marked and asked about — or none found.
6. Keep one authoritative home for each fact. Link to it from other docs instead of restating it, unless local clarity requires a short recap.
   Done: each fact in scope has one home; other mentions link or use a brief consistent recap.
7. When redundancy is useful for clarity, keep it brief and make it consistent with the authoritative doc.
   Done: any kept redundancy is brief and matches the authoritative doc.

## Contradictions

Do not silently resolve contradictions between docs. Surface the conflict for user clarification when:

- Two current docs disagree.
- A current doc conflicts with observed source behavior.
- A roadmap direction conflicts with current docs and the intended future is unclear.
- History or unsorted material appears to describe current behavior differently.

Authority on roadmap/history/unsorted vs current: [[doc/README.md]].
