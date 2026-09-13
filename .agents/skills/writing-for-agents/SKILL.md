---
name: writing-for-agents
description: Writing documents for agents. Use when creating or editing skills, or modifying AGENTS.md or .agents/ bridges.
---

Write any document an agent consumes — a skill, [[AGENTS.md]], a `.agents/` bridge, or a doc reached by a pointer. Skill packaging: [[SKILL-MECHANICS.md]]. Terms: [[GLOSSARY.md]]. Method (why the levers work): [[WHY.md]].

## Recipe

1. Name the document and its **branches**. Done: each always-loaded pointer has one trigger per genuine branch, and the leading word is front-loaded.
2. If the document is a skill, apply [[SKILL-MECHANICS.md]] for frontmatter, invocation, and routers. Done: invocation matches the reach you need.
3. Write ordered **steps** in the main file. Each step ends on a checkable, exhaustive **completion criterion**. Why is never a step. Done: a reader can tell done from not-done for every step.
4. Keep in-file only the caveats every run needs, beside the rule they justify. Disclose other **reference** behind a pointer. Product and architecture why live in a Committed Decision ([[doc/Decisions/]]) or spec, not a skill. Done: every in-file paragraph is a step or an every-run caveat.
5. Prune. One meaning, one home. Leave environment lookups in the environment. Delete **no-ops** as whole sentences. Done: no **cache** of a cheap lookup, and no sentence that does not change behaviour.

## Gates

- Pointer: cut identity the body already carries. Sharpen wording before you inline.
- Split only when the cut earns a load: by sequence (hide **post-completion steps** across a real context boundary) or by invocation ([[SKILL-MECHANICS.md]]).
- Prompt the positive. A prohibition is a last-resort guardrail, paired with the target.
- Keep structure (WHAT, WHERE) and method (HOW) in wholly separate documents. Put why in [[WHY.md]] or another disclosed file, not in a Method section of the recipe.
