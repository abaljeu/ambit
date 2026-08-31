# Committed Decisions

A **Committed Decision** records a choice that is costly to reverse, surprising without context, and made between genuine alternatives. Skip routine or easily reversible choices.

(The mattpocock skills call this an ADR; in this project we always say Committed Decision.)

Out of scope for a Project is not a Committed Decision. Do not infer product exclusions from specs, maps, roadmap leftovers, or code absence. See [[doc/agents/scope-vs-commitment.md]].

Name records sequentially: `0001-short-title.md`, `0002-short-title.md`, and so on. Scan this directory for the highest existing number before creating one.

Use this minimal format:

```markdown
# Short title of the decision

In one to three sentences, state the context, the decision, and why it was chosen.
```

Add status, considered options, or consequences only when they provide lasting value.
