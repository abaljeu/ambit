# 03 — Expression result insert today

**Type:** research
**Status:** defined
**Blocked by:** None

## 1. Question

Where does a leading `=` expression place Nodes today?

Report current behavior for Find, Move, and Run. State whether any path inserts Answers as Children under the expression line. Cite the functions that do the work. Do not propose the Query spec.

Primary sources start at [ExprDialog.fs](src/Shared/ExprDialog.fs) (`tryHits`), [ExprEval.fs](src/Shared/ExprEval.fs), and [ViewModelSearch.fs](src/Shared/ViewModelSearch.fs) (`searchNodes`). Follow the Run path from those call sites.

Write the findings to [expression result insert today](plan/online-search/reports/expression-result-insert-today.md).

## 2. Context

1. **Findings** — [Expression result insert today](plan/online-search/reports/expression-result-insert-today.md). This chart did not accept an Answer.
