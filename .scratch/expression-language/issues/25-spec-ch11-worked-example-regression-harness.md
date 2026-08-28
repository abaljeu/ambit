# 25 — Spec ch.11 worked-example regression harness

**Context:** Chapter 11 lists worked examples that lock parse, type, and evaluation outcomes across the full slice. A regression harness keeps later edits from drifting off spec. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Add Shared tests (or an equivalent harness) that exercise the chapter 11 table: valid Expressions with expected Answer sets or outcomes, plus parse-error, type-error, and zero-Answer rows. Cover structural desugar (`//ws`, `d/e`), structural search composed with a classification filter (`/ "d" dir`), content search (`d#e`, `a#b#c`, `^#blue`), pure filters (`class "h1"`, `dir`, and similar same-input subset rows), combinators (`#x , #y`, `NOT containing`), and consumer-relevant forms where testable in Shared. Each row asserts the spec outcome, not today's RefExpr behavior.

**Blocked by:** [[.scratch/expression-language/issues/23-and-or-not-and-comma-combinators.md]], [[.scratch/expression-language/issues/21-run-consumer-equals-and-name-equals-statements.md]], [[.scratch/expression-language/issues/22-search-and-move-consumer-leading-equals.md]].

**See also:** [[.scratch/expression-language/spec.md]] chapter 11; [[.scratch/expression-language/reports/pipeline-examples.md]].

**Status:** ready-for-agent

- [ ] At least one Shared test fixture encodes a small graph adequate for structural, content, and filter examples.
- [ ] Valid rows such as `root tree`, `//ws/x`, `d#e`, and `containing "the" AND named "blue"` assert expected Answers.
- [ ] Error rows such as `// ws`, `"d" "e"`, `/`, `// OR /`, and `text #todo` assert parse or type error per spec.
- [ ] Zero-Answer rows such as `!-249053534` assert empty sequence, not an exception.
