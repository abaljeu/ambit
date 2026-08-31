# 26 — `section` filter and `subsection` catalog spelling

**Context:** HITL 2026-08-28 locked two catalog rows. `section` is a zero-argument pure filter: keep the input when it is a named Normal Node. `subsection` is the spoken spelling of cluster `#` (search for sections below), parallel to `tree` for `**`. Work on branch `w/expr` (cut from `selective-client-sync`).

**What to build:** Add `section` as a pure filter row in [[src/Shared/ExprWalk.fs]] and [[src/Shared/ExprPrimitive.fs]], same shape as `dir` and `normal`. Register `subsection` as a spelling of the existing `#` search row (NameGlob slot; `subsection "todo"` equals `#todo`), the same way `tree` and `**` share a row. Add `subsection` to the parse list of words that take a trailing quoted literal ([[src/Shared/ExprParse.fs]]). Do not change `/`. Do not add combinators. Do not implement ticket 23.

**Blocked by:** none.

**See also:** [[.scratch/expression-language/spec.md]] chapter 7 `section` and `subsection` rows; [[.scratch/expression-language/reports/section-filter-lock.md]]; [[.scratch/expression-language/issues/20-pure-filter-catalog-rows.md]]; [[.scratch/expression-language/issues/18-content-search-path-step-evaluation.md]]. Ticket 25's chapter 11 harness should include `section` and the `subsection "todo"` / `#todo` equivalence when that harness lands; do not rewrite [[.scratch/expression-language/issues/25-spec-ch11-worked-example-regression-harness.md]] here.

**Status:** done

- [x] `section` keeps a named Normal Node and yields nothing on an unnamed Normal Node, File Node, Directory Node, or Workspace Node; it does not walk Children.
- [x] `subsection "todo"` and `#todo` yield the same Answers from the same input.
- [x] Bare `subsection` is a missing-argument parse error, uniform with bare `#`.
- [x] `named` and `section` stay distinct: `named "blue"` is a name glob on the input; `section` is named-Normal classification with no argument.
