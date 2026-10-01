---
name: co-edit-format-plan
description: Co-edits a document-format with the user one section at a time and specs in place. Use when planning a new persisted file format in doc/, hyper-interactive plan drafting
---

# Co-Edit Format Plan

Follow [[.agents/rules/planning-docs.md]], [[.agents/rules/markdown-writing.md]], and [[.agents/rules/core-agent-behavior.md]].

Pair with [[.agents/skills/plan-roadmap-change/SKILL.md]] for roadmap shape and [[.agents/skills/maintain-doc-currency/SKILL.md]] when touching `doc/index.md` or stage sequencing.

## Workflow

1. Hyper-interactive: draft **one section** (or one design question) per turn with small focused edits; summarize actions only — the user sees the diff.
   Done: one section (or question) edited, actions summarized, next section or question named.
2. When several interpretations exist, present them.
   Done: each live fork is listed, or one reading is clear.
3. Plan docs only until the user explicitly asks to implement.
   Done: no `src/` or test changes unless asked.
4. Work only on the active document; treat finished plans as historical context for their time.
   Done: edits are only on the active plan unless the user says otherwise.
5. Update roadmap markdown in the same session as design decisions land; adjust [[doc/index.md]] "Might be next" when sequencing changes.
   Done: roadmap and index reflect landed decisions for this turn, or no sequencing change.
6. Use **real example files** the user attaches when reasoning about structure; mark **TBD** honestly (identity anchors, classification path vs sniff).
   Done: attached examples informed the edit when present; open TBDs are marked.
