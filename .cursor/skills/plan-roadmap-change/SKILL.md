---
name: plan-roadmap-change
description: Plans roadmap and architecture doc changes in doc/ with incremental slices and wikilinks. Use when editing doc/roadmap, writing plans, scoping features, or when a change exceeds a small increment.
---

# Plan Roadmap Change

Follow [[.cursor/rules/planning-docs.mdc]] and [[.cursor/rules/markdown-writing.mdc]].

## Workflow

1. Read [[doc/arch.md]] and related current docs before proposing structure.
2. Align with [[doc/plan]] priorities when choosing what to plan next.
3. Prefer a **slice** with clear user value over a full-system design.
4. State assumptions and tradeoffs before writing the doc.
5. Defer unrelated work explicitly in the plan.

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

## Do not

Implement source code while planning unless the user explicitly asks to implement.
