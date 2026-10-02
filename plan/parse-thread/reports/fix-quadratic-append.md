# Fix quadratic append

[Directory reconcile](src/Shared/dotnet/DirectoryReconcile.fs) `createMissing` no longer plans and applies one full-list `Op.Replace` per missing file. It allocates each missing File Node with `Op.NewSpecialNode`, then returns one `ChildListWire.append` for the whole batch. Apply builds the graph once for that append. `Graph.addDetachedNode` still inserts each new node. No `Op.SetDocumentState`. No Unparsed marking for a disk-newer file.

Directory reconcile tests: 3 passed, 0 failed.

## Other findings

These findings are from [Code review — Directory reconcile](plan/parse-thread/reports/code-review-05-directory-reconcile-uncommitted.md). This pass did not fix them.

- Spec (b) — An inaccessible outline member that stays in both child lists no longer blocks `Op.Replace`, and that rule applies to every caller. Still open.
- Spec (c) — A disk-newer File Node came back as `Op.SetDocumentState` to Unparsed, so `parseState` stayed Parsed and a later reconcile could miss the file. Already handled by the move onto [06 — Setting Unparsed, recursive update](plan/parse-thread/issues/06-setting-unparsed-recursive-update.md).
- Spec (d) — `DirectoryReconcile.Input` is `dataDir`, `graph`, and `directoryId`. [05 — Directory reconcile](plan/parse-thread/issues/05-directory-reconcile.md) names a Directory Node and the Body. The Body is not on that record. Output still matches. Still open.
