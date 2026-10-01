# Ticket build, F#, TDD, review

Range: clean tip vs `HEAD` for this cluster (dirs unchanged). Mechanical scan: none. Unrelated dirty files under `plan/skills-review/reports/` ignored.

## Standards

1. **Checkable Done** ([writing-for-agents/SKILL.md](.agents/skills/writing-for-agents/SKILL.md) Recipe 3). Process steps without a Done line: [implement](.agents/skills/implement/SKILL.md) §3 Code / §4 Check; [add-shared-test](.agents/skills/add-shared-test/SKILL.md) Workflow 1–4; [write-simple-parser](.agents/skills/write-simple-parser/SKILL.md) Before coding; [investigate-fable-client](.agents/skills/investigate-fable-client/SKILL.md) Investigation order; [code-review](.agents/skills/code-review/SKILL.md) steps 1, 3–6; [diagnosing-bugs](.agents/skills/diagnosing-bugs/SKILL.md) Phases 3–5.

2. **Why is never a step** (same Recipe 3). [diagnosing-bugs](.agents/skills/diagnosing-bugs/SKILL.md): `Why bother: a minimal repro shrinks…`

3. **One normative home** (same Gates). Good-test / anti-pattern meaning restated in [tdd/SKILL.md](.agents/skills/tdd/SKILL.md) and [tdd/tests.md](.agents/skills/tdd/tests.md). [write-simple-parser](.agents/skills/write-simple-parser/SKILL.md) Checklist restates Grammar / Separate passes / Avoid. Full-suite-at-end restated in [implement](.agents/skills/implement/SKILL.md) §4 and [implement-fsharp-feature](.agents/skills/implement-fsharp-feature/SKILL.md) Background.

4. **Prompt the positive** (same Gates). [write-simple-parser](.agents/skills/write-simple-parser/SKILL.md) `## Avoid` (prohibition list without paired positive targets). [diagnosing-bugs](.agents/skills/diagnosing-bugs/SKILL.md): `Do not proceed to hypothesise without a loop` / `Do not proceed until…` without a dominant positive phrasing.

5. **Cache** ([GLOSSARY.md](.agents/skills/writing-for-agents/GLOSSARY.md)). [investigate-fable-client](.agents/skills/investigate-fable-client/SKILL.md) `## Client layout` table restates repo paths the environment already answers.

6. **No consecutive blank lines** ([markdown-writing.md](.agents/rules/markdown-writing.md)). [implement/SKILL.md](.agents/skills/implement/SKILL.md) blank pairs before both `### 4.` headings.

7. **No mid-paragraph linebreaks** (same rule). [write-simple-parser/SKILL.md](.agents/skills/write-simple-parser/SKILL.md) wrapped list/step lines (e.g. EBNF step; “local lexical” bullet).

Smells (judgement):

- **Overcomplication** — [diagnosing-bugs/SKILL.md](.agents/skills/diagnosing-bugs/SKILL.md): ten loop recipes + six phases for one feedback-loop discipline.
- **Overcomplication** — [tdd/tests.md](.agents/skills/tdd/tests.md) / [tdd/mocking.md](.agents/skills/tdd/mocking.md): TypeScript/Jest examples in an F# cluster.
- **Mysterious Name** — [implement/SKILL.md](.agents/skills/implement/SKILL.md): two headings both `### 4.`
- **Duplicated Code** — same shapes as One normative home above (tdd↔tests.md; parser checklist↔body; suite-at-end).

## Spec

no spec available

Standards: 7 hard + 4 smell. Worst: missing Done across process skills; Overcomplication in diagnosing-bugs.
Spec: 0. Worst: n/a.
