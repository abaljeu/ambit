# Code review: Deep-module design

Range: `.agents/skills/codebase-design` (SKILL.md, DESIGN-IT-TWICE.md, DEEPENING.md), `.agents/skills/improve-codebase-architecture` (SKILL.md), `.agents/skills/setup-ts-deep-modules` (SKILL.md, dependency-cruiser.config.cjs). Tip: clean vs HEAD (current file contents). `git diff HEAD --` those paths empty. scan: none.

## Standards

1. **writing-for-agents Recipe §3 — checkable Done.** DESIGN-IT-TWICE.md steps 1–3 and improve-codebase-architecture/SKILL.md steps 1–3 have no Done / completion criterion. setup-ts-deep-modules does.

2. **Gate One normative home.** “One adapter… / Two adapters…” and “interface is the test surface” are in codebase-design/SKILL.md Principles and restated in DEEPENING.md Seam discipline / Testing. improve’s opening restates the vocabulary avoid-list and principles instead of citing `/codebase-design` alone.

3. **Gate structure vs method.** codebase-design/SKILL.md mixes glossary/WHAT with “Designing for testability” HOW (numbered recipe + code samples) in one document.

4. **Gate prompt the positive.** DESIGN-IT-TWICE.md Anti-patterns is four bare “Do not…” lines with no positive target beside them.

5. **Recipe §5 prune / accuracy.** setup-ts claims “Four rules, all `error`” and Done “four forbidden rules,” but dependency-cruiser.config.cjs ships five forbidden rules (`tests-folder-is-private` unnamed in the skill’s four).

Smells (judgement):

- **Overcomplication** — deep/shallow ASCII blocks in codebase-design SKILL.md (~34–52). Simpler: two one-line definitions; drop the boxes.
- **Overcomplication / Duplicated Code** — setup-ts `## Notes` (~97–103) re-explains entry points, `lib/`/`tests/`, barrels already under “The shape this enforces.” Simpler: delete Notes or leave only the `$1` gotcha.
- **Overcomplication** — config header comment (~1–12) essays the same shape the skill already owns. Simpler: one-line pointer + `PACKAGES_ROOT`.
- **Negation (Overcomplication-adjacent)** — improve “Don't follow rigid heuristics — explore organically” (~28): no-op vs default Explore; drop the sentence.

## Spec

no spec available

Standards: 5 hard + 4 smell findings; worst: missing Done on design-it-twice and improve steps. Spec: 0 (no spec available).
