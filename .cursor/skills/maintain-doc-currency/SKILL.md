---
name: maintain-doc-currency
description: Maintains doc/ currency by auditing document placement, contradictions, stale roadmap material, and redundant coverage. Use when updating docs, moving docs between doc/ subdirs, reviewing doc accuracy, or reconciling implemented work with roadmap/current/reference/history docs.
---

# Maintain Doc Currency

Follow [[.cursor/rules/markdown-writing.mdc]] and read [[doc/README.md]] first. Treat [[doc/README.md]] as the source of truth for what each documentation subdir means.

## Directory Fit

- Top-level [[doc/]] docs are front-door docs for the current system as a whole: global index, architecture, spec, and API.
- [[doc/current/]] holds current subsystem or feature baselines.
- [[doc/roadmap/]] holds committed direction and rollout tracking, not fully implemented behavior.
- [[doc/history/]] holds assessed historical project materials.
- [[doc/reference/]] holds operational and reference material.
- [[doc/unsorted/]] is temporary, unassessed, and non-authoritative.

If a doc's content no longer matches its directory, propose the smallest correction: move it, split it, archive it to history, or rewrite the stale section.

## Currency Workflow

1. Read the relevant current docs before changing roadmap, history, or unsorted material.
2. Check the global index named by [[doc/README.md]] for status and sequencing.
3. Check whether implemented behavior is still described as future work; if so, move the durable truth into current or reference docs and reduce roadmap text to remaining rollout or follow-up work.
4. Update the global index when a change affects what is done, what remains, or the order of planned work.
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
- [ ] Global index status and sequencing still match the changed docs.
- [ ] Fully implemented behavior is not treated as future roadmap work.
- [ ] Current docs remain the authority for current behavior.
- [ ] Redundant text is either removed, linked, or deliberately kept for clarity.
- [ ] Any contradiction that requires product judgment is called out instead of guessed.
