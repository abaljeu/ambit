# 25 — Spec ch.11 worked-example regression harness

**Context:** Chapter 11 lists worked examples that lock parse, type, and evaluation outcomes across the full slice. A regression harness keeps later edits from drifting off spec. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Add Shared tests (or an equivalent harness) that exercise the chapter 11 table: valid Expressions with expected Answer sets or outcomes, plus parse-error, type-error, and zero-Answer rows. Cover structural desugar (`//ws`, `d/e`), structural search composed with a classification filter (`/ "d" dir`), content search (`d#e`, `a#b#c`, `^#blue`), pure filters (`class "h1"`, `dir`, and similar same-input subset rows), combinators (`#x , #y`, `NOT containing`), and consumer-relevant forms where testable in Shared. Each row asserts the spec outcome, not today's RefExpr behavior.

**Blocked by:** none. Combinator table rows remain leftover in the harness (ticket 23 is done).

**See also:** [[.scratch/expression-language/spec.md]] chapter 11; [[.scratch/expression-language/reports/pipeline-examples.md]].

**Status:** done

- [x] At least one Shared test fixture encodes a small graph adequate for structural, content, and filter examples.
- [x] Valid rows such as `root tree`, `//ws/x`, and `d#e` assert expected Answers. Combinator row `containing "the" AND named "blue"` is leftover (ticket 23).
- [x] Error rows such as `// ws`, `"d" "e"`, `/`, and `text #todo` assert parse or type error per spec. Combinator row `// OR /` is leftover (ticket 23).
- [x] Zero-Answer rows such as `!-249053534` assert empty sequence, not an exception.

## Comments

Non-combinator chapter 11 harness: [[.scratch/expression-language/reports/ticket-25-ch11-harness.md]]. Ticket 23 still owns parse of `AND`, `OR`, `NOT`, and comma, so those table rows were omitted. Ticket 26 `section` / `subsection` rows were omitted; `#todo` stands in for `subsection "todo"` where the table allows it.
