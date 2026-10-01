# Code review — Article writing

Range: `.agents/skills/writing-fragments`, `writing-beats`, `writing-shape`, `edit-article` (and companions in those dirs). Working tree clean for these paths (`git diff HEAD --` empty); tip is current file contents. scan: none.

## Standards

**Missing checkable Done** — writing-for-agents/SKILL.md Recipe §3. No skill ends a step with a Done criterion. writing-fragments has no ordered steps. writing-beats steps 1–5 and writing-shape loop 1–6 lack Done; “natural end” / “user decides” are not checkable. edit-article stops at `2a` with no Done.

**One normative home / Duplicated Code (judgement)** — Gates **One normative home**; SKILL-MECHANICS **External Reference**. Both exploit skills restate Grounding:

> Every **concept** has to be **grounded** before a beat/block can lean on it…

Same Prerequisite / Introduced split and lever text. Simpler: one external grounding file; each skill points at it.

**User-invoked description** — SKILL-MECHANICS.md Invocation. edit-article has `disable-model-invocation: true` but keeps model trigger phrasing: “Use when user wants to edit, revise, or improve an article draft.”

**Information hierarchy** — GLOSSARY **Steps** primary / Recipe §4. writing-shape puts **The loop** (the steps) under `<supporting-info>` while prose sits in `<what-to-do>`.

**Prompt the positive (judgement)** — Gates. writing-fragments: “no metadata, no TOC, no date”; writing-shape **Out of scope** is mostly bans. Prefer the target (“H1 title only”; “read-only pile; handle gaps in Pulling…”).

**Overcomplication (judgement)** — writing-shape in-file **Conversational feel**, **Format arguments…**, and **Out of scope** (~40 lines) bury the loop. Disclose format/grill moves; keep every-run caveats only.

**Cluster note:** fragments correctly stays explore / no structure. beats and shape both naming **exploit** is fine as alternate exploit skills; the stand-behind overlap is the duplicated Grounding home, not the shared stage word.

## Spec

no spec available

Standards: 6 findings (worst: missing checkable Done on all four skills). Spec: 0 (no spec available).
