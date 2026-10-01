# Code review — Authoring agent instructions

**Range:** `.agents/skills/writing-for-agents/`, `.agents/skills/prepare-agent-instruction-change/` — clean tip (current file contents; `git diff HEAD --` those paths empty). Unrelated dirty: `plan/skills-review/reports/Nine review groups, plus a standalone li.md` only (not reviewed).
**scan:** none

## Standards

**Hard — writing-for-agents/SKILL.md Recipe §3 (checkable Done):** prepare-agent-instruction-change/SKILL.md Edit workflow steps 1–3 have no `Done:` lines. A reader cannot tell done from not-done per step.

**Hard — same SKILL.md Gate “Prompt the positive”:** prepare-agent `## Do not` lists bare prohibitions with no paired target behaviour.

**Hard — same SKILL.md Gate “One normative home” (+ brief):** prepare-agent checklist restates writing-for-agents Recipe §4 Done (“every paragraph is a step or an in-file caveat…”) instead of citing only. Principles “link to rules, do not copy them” is restated again in the checklist.

**Hard — same SKILL.md Gate “structure vs method”:** prepare-agent keeps Principles (WHAT/WHERE) and Edit workflow (HOW) in one file.

**Hard — same SKILL.md Gate “One normative home”:** SKILL-MECHANICS.md Invocation + Router paragraphs restate GLOSSARY.md Model-Invoked / User-Invoked / Router Skill rather than citing them.

**Smell (judgement) — Overcomplication / Duplicated Code:** SKILL-MECHANICS hunk:
> “A **model-invoked** skill keeps a `description`, so the agent can fire it autonomously… A **user-invoked** skill strips the description…”
Simpler: keep only frontmatter mechanics (`disable-model-invocation`, description role); point at GLOSSARY for the definitions.

**Smell (judgement) — Overcomplication / Cache:** prepare-agent Principles Rules bullet restates gambol.md stub layout after already pointing at it. Simpler: one pointer to gambol.md + Bridges-only delta; fold review into `Done:` on the three steps.

## Spec

no spec available

Standards: 7 findings (5 hard, 2 smells); worst: prepare-agent Edit workflow steps lack checkable `Done:`. Spec: 0 findings (no spec available).
