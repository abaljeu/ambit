# 05 — Query fulfillment while eval stays local

**Type:** grilling
**Status:** defined
**Blocked by:** [02 — Want package of Nodes](02-want-package-of-nodes.md), [03 — Expression result insert today](03-expression-result-insert-today.md)

## 1. Question

Expression eval stays on the client Graph. A query expression must insert the Nodes the server finds under the query line. That insert is Want-fulfillment. What does the server fulfill for a query?

1. **Eval** — Eval stays local. See [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md).
2. **Limit** — The function defines its own limit.
3. **Trash** — Trash is skipped, except a trash function that works like root.
4. **Separate spec** — The Find dialog is the Search spec, not this spec.

Use the findings on [02 — Want package of Nodes](02-want-package-of-nodes.md) and [03 — Expression result insert today](03-expression-result-insert-today.md).
