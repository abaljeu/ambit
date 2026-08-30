---
name: co-edit-format-plan
description: Co-edits a document-format with the user one section at a time and specs in place. Use when planning a new persisted file format in doc/, hyper-interactive plan drafting
---

# Co-Edit Format Plan

Follow [[.cursor/rules/planning-docs.mdc]], [[.cursor/rules/markdown-writing.mdc]], and [[.cursor/rules/core-agent-behavior.mdc]].

Pair with [[.cursor/skills/plan-roadmap-change/SKILL.md]] for roadmap shape and [[.cursor/skills/maintain-doc-currency/SKILL.md]] when touching `doc/index.md` or stage sequencing.

## Interaction

Hyper-interactive: **you draft one slice → user edits → you react**. Do not one-shot the whole plan.

- Work **one section** (or one design question) per turn unless the user asks for more.
- The user sees the diff; **summarize actions only** — do not repeat the diff back.
- **Small corrective edits** in the same files are in scope when they follow directly from the turn (stale wording, cross-doc alignment, terminology).
- When several interpretations exist, **present them** — do not pick silently.
- **Planning only** until the user explicitly requests implementation.
- **Implicit license** any prompt is implicitly a license to make focused edits to the document to match the request.
- **Expect changes** after you change a thing the user will likely change things.  Therefore keep edits small so they are easier to change, rather than comprehensive and needing more change.

## Reference vs active plan

Except when the user says otherwise, work only on the active document.
Do not edit previously finished plans if they no longer match the new plan.  They describe past situations and are relevant to their time.

## Plan file skeleton

Use these headings in referenced documents to kickstart an outline in the current.

## Documentation workflow

Update roadmap markdown **in the same session** as design decisions land:

Adjust [[doc/index.md]] "Might be next" when sequencing changes.

Use [[wikilinks]]; one blank line between blocks; no hard-wrapped paragraphs.

## Design co-editing

- Use **real example files** the user attaches when reasoning about structure (e.g. prologue, single root element, mixed content).
- Mark **TBD** honestly (identity anchors, classification path vs sniff).

## Turn endings

After each edit, state what you changed in one short paragraph and name the **next section or question** — do not pad with optional follow-ups unless a real fork remains.

## Do not

- Implement `src/` or tests while co-editing unless explicitly asked.
- Add a "Planned Doc Changes" checklist when roadmap files can be updated directly.
- Copy finished-plan assumptions verbatim when the codebase or format differs.
- Edit finished plans as part of a new format slice.
