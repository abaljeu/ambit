---
name: plan-roadmap-change
description: Plans roadmap and architecture doc changes in doc/ with incremental slices and wikilinks. Use when editing doc/roadmap, writing plans, scoping features, or when a change exceeds a small increment.
---

# Plan Roadmap Change

Follow [[.agents/rules/planning-docs.md]] and [[.agents/rules/markdown-writing.md]].

## Workflow

1. Read [[doc/arch.md]] and related current docs before proposing structure.
   Done: every current doc that bears on the proposed structure has been read.
2. Align with live `plan/` Projects and [[plan/roadmap/map.md]] when choosing what to plan next.
   Done: the chosen next plan matches live Projects and the map (or the mismatch is stated for the user).
3. Prefer a **slice** with clear user value over a full-system design.
   Done: the plan names one slice and its user-visible value (or a simpler outline when the topic needs less).
4. State assumptions and tradeoffs before writing the doc.
   Done: assumptions and tradeoffs are written before the plan body.
5. Defer unrelated work explicitly in the plan.
   Done: unrelated work is listed under deferrals, or the plan states there is none.

## Plan shape

Use this outline unless the topic needs something simpler:

```markdown
# [Feature or slice name]

See also: [[doc/...]]

## What it gives you
[Concrete user-visible outcomes]

## What it avoids for now
[Explicit deferrals]

## Minimal state / API / ops
[Only what this slice needs]

## Implementation steps
[Numbered, small, verifiable increments]

## Tests
[Which Shared.Tests files or cases prove the slice]
```

## Review checkpoints

Stop for review when:

- The slice boundary or deferrals change materially.
- A step would touch Server, Client, and Shared in one pass.
- The plan spans multiple unrelated features.

## Scope

Plan docs only until the user explicitly asks to implement.
