# IF/IS example grouping

The user form is `(text IF ("b" IS left 1)) OR "isn't a b word"`. The ungrouped `(text IF "b" IS left 1)` parses as `(text IF "b") IS (left 1)` because `IF` binds tighter than `IS`. Precedence is unchanged. `"d" "e"` stays a parse error.

Replaced in [[text-ops-impl.md]] (example plus one note paragraph) and [[tests/Shared.Tests/ExprTextOpsTests.fs]] (`quoted strings stay legal as combinator operands and slots`). That test still only asserts `OR` with a quoted right operand; it does not lock IF/IS precedence. [[spec.md]] and [[../issues/30-text-operations.md]] did not contain the ungrouped form (they use `"d" OR "e"` / a generic combinator-legal sentence). No product parse change. No commit. WORK.md untouched.

Focused test: 1 passed.
