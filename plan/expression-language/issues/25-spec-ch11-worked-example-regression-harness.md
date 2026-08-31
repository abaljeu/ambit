# 25 — Spec ch.11 worked-example regression harness

**Context:** Chapter 11 lists worked examples that lock parse, type, and evaluation outcomes across the full slice. A regression harness keeps later edits from drifting off spec. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Add Shared tests (or an equivalent harness) that exercise the chapter 11 table: valid Expressions with expected Answer sets or outcomes, plus parse-error, type-error, and zero-Answer rows. Cover structural desugar (`//ws`, `d/e`), structural search composed with a classification filter (`/ "d" dir`), content search (`d#e`, `a#b#c`, `^#blue`), pure filters (`class "h1"`, `dir`, and similar same-input subset rows), combinators (`#x , #y`, `NOT containing`), and consumer-relevant forms where testable in Shared. Each row asserts the spec outcome, not today's RefExpr behavior.

**Blocked by:** none. Combinator leftover filled (except ticket 26 `section` / `subsection` rows).

**See also:** [[plan/expression-language/spec.md]] chapter 11; [[plan/expression-language/reports/pipeline-examples.md]].

**Status:** done

- [x] At least one Shared test fixture encodes a small graph adequate for structural, content, and filter examples.
- [x] Valid rows such as `root tree`, `//ws/x`, and `d#e` assert expected Answers. Combinator rows `#x , #y`, `containing "the" AND named "blue"`, and `root descendant NOT containing "draft"` are in the harness.
- [x] Error rows such as `// ws`, `"d" "e"`, `/`, `// OR /`, and `text #todo` assert parse or type error per spec.
- [x] Zero-Answer rows such as `!-249053534` assert empty sequence, not an exception.

## Comments

Combinator leftover filled (except 26 rows). [[tests/Shared.Tests/ExprChapter11Tests.fs]] includes `#x , #y` (concatenates; a Node may appear twice), `containing "the" AND named "blue"` (same-input intersection), `root descendant NOT containing "draft"` (negation-as-failure), and parse error `// OR /`. Ticket 26 `section` / `subsection` rows stay omitted; `#todo` stands in for `subsection "todo"` where the table allows it. Report: [[plan/expression-language/reports/ticket-25-ch11-combinator-rows.md]].
