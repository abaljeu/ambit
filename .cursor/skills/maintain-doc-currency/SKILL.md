---
name: maintain-doc-currency
description: Maintains doc/ currency by auditing document placement, contradictions, stale roadmap material, and redundant coverage. Use when updating docs, moving docs between doc/ subdirs, reviewing doc accuracy, or reconciling implemented work with roadmap/current/reference/history docs.
---

# Maintain Doc Currency

Follow [[.cursor/rules/markdown-writing.mdc]] and read [[doc/README.md]] first. Treat [[doc/README.md]] as the source of truth for what each documentation subdir means.

## Directory Fit

- Top-level [[doc/]] docs are front-door docs for the current program: Feature index, architecture, spec, and API.
- [[doc/current/]] holds current subsystem or feature baselines.
- [[doc/roadmap/]] is leftover planned-direction text until a `.scratch` Project cites it or it moves to history. New planned work lives in Projects.
- [[doc/history/]] holds assessed historical project materials.
- [[doc/reference/]] holds operational and reference material.
- [[doc/unsorted/]] is temporary, unassessed, and non-authoritative.

If a doc's content no longer matches its directory, propose the smallest correction: move it, split it, archive it to history, or rewrite the stale section.

## Currency Workflow

Before promoting any exclusion or "Gambol does not …" into `doc/`, confirm it is a product **commitment** with an authorized source — not **scope** or **surmise** from a Project. See [[docs/agents/scope-vs-commitment.md]].

1. Read the relevant current docs before changing roadmap, history, or unsorted material.
2. Check the Feature index [[doc/index.md]] for current-program coverage.
3. Check whether implemented behavior is still described as future work; if so, move the durable truth into current or reference docs and reduce leftover roadmap text, or cite it from a `.scratch` Project.
4. Update the Feature index when a change affects what is current. Planned work is not sequenced there.
5. Check whether roadmap commitments became obsolete; mark the mismatch and ask before deleting or rewriting direction.
6. Keep one authoritative home for each fact. Link to it from other docs instead of restating it, unless local clarity requires a short recap.
7. When redundancy is useful for clarity, keep it brief and make it consistent with the authoritative doc.

## Contradictions

Do not silently resolve contradictions between docs. Surface the conflict for user clarification when:

- Two current docs disagree.
- A current doc conflicts with observed source behavior.
- A roadmap direction conflicts with current docs and the intended future is unclear.
- History or unsorted material appears to describe current behavior differently.

When a roadmap, history, or unsorted doc disagrees with a current doc, assume the current doc wins unless the user says the current doc is stale.

## Finishing Checklist

- [ ] The doc lives in the directory described by [[doc/README.md]].
- [ ] The Feature index still matches current docs. Planned work is not listed there.
- [ ] Fully implemented behavior is not treated as future roadmap work.
- [ ] Current docs remain the authority for current behavior.
- [ ] Redundant text is either removed, linked, or deliberately kept for clarity.
- [ ] Any contradiction that requires product judgment is called out instead of guessed.
