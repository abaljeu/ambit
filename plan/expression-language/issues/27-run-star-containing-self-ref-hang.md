# 27 — Run `!* containing` does not terminate

**Context:** Production HITL 2026-08-29. Run `=!* containing "OpenDrive"` never finishes. `!*` includes the query node; `containing` matches the expression text, so Run writes a self-Ref. [[src/Shared/AmbleRun.fs]] `applyUnfold` called [[src/Shared/ViewModelSiteMap.fs]] `applyFoldSession` for that NodeId. Session restore expands every instance of the NodeId, so the new self-Ref instance is expanded too and the queue never empties. One-level unfold of a self-Ref does not hang. Spec chapter 8: when Run writes Children it unfolds that Node — the query node's instance, not every occurrence.

**What to build:** `applyUnfold` calls `expandEntry` on the query node's instance (captured before apply). Do not use `applyFoldSession` for Run. Do not change session restore.

**Blocked by:** none.

**See also:** [[plan/expression-language/spec.md]] chapter 8; [[plan/expression-language/reports/run-unfold-node.md]].

**Status:** done

- [x] Run unfolds the query node's instance one level via `expandEntry`.
- [x] Run `=!* containing "OpenDrive"` on a node whose text is that line finishes and unfolds that instance.
