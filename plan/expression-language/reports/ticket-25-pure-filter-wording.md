# Ticket 25 — Pure-filter wording

Branch: `w/expr`. Tree left dirty. No commit. No `src/` or `tests/` edits. Ticket 24 not touched.

## HITL 2026-08-28

A pure filter takes a set of Nodes and returns a subset of them. `class "h1"` and `dir` are such. `/ "d" dir` is not: its left-hand side is typically a Directory Node, and the output is Children of that directory (structural search composed with a classification filter).

## Ticket 25

[[plan/expression-language/issues/25-spec-ch11-worked-example-regression-harness.md]] — **What to build** grouped `/ "d" dir` under "pure filters (`/ "d" dir`, `class "h1"`)". That grouping is wrong.

The paragraph now keeps structural desugar (`//ws`, `d/e`) and content search (`d#e`, `a#b#c`, `^#blue`). Pure-filter examples are `class "h1"`, `dir`, and similar same-input subset rows. `/ "d" dir` stays as a chapter 11 row, labeled structural search composed with a classification filter.

Checkboxes did not call `/ "d" dir` a pure filter. No checkbox edit.

## Spec and other docs

[[plan/expression-language/spec.md]] chapter 11 already says `/ "d" dir` yields structural Nodes named `d` below the current Node, kept only when they are Directory Nodes. Nearby catalog rows call `dir` (and `class`) a pure filter and give `x / "d" dir` as the composed spelling for directories named `d`. Neither place labels the composition itself as a pure filter. No spec rewrite.

[[plan/expression-language/issues/20-pure-filter-catalog-rows.md]] and [[plan/expression-language/reports/ticket-20-pure-filters.md]] mention `/ "d" dir` among filter tests, with "among structural matches". They do not copy the false "pure filters (`/ "d" dir`, …)" claim. Left as-is.

## WORK.md mutations

None. Ticket 25 is not on the board. This is a wording correction only.
