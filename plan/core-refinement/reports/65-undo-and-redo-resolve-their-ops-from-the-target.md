# Code review — Undo and Redo resolve their ops from the target

- Range: `git diff HEAD` plus untracked `src/Shared/UndoTarget.fs`
- Spec: [65 — Undo and Redo resolve their ops from the target](../issues/65-undo-and-redo-resolve-their-ops-from-the-target.md)
- Arch: [core-refinement architecture](../arch.md)

## 1. Standards

1. **Hard** — [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md) forbids rebuilding a whole structure once per item. `SyncLogic.rememberEvent` calls `EventLog.restore` on every folded event, and `restore` maps the whole log. `EventLog.recover` calls `recoverOps`, which scans that log per event. `ClientHistory.logOf` rebuilds `kept @ eventPast @ eventFuture` from `appliedOps` inside the pending fold.
2. **Hard** — [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md) says to group related parameters into a named type. `CoreEventDispatch.acceptUndoRedo` takes six positional arguments. `writeUndo` repeats caller, submission id, command name, and body.
3. **Judgement — Duplicated code** — `EventLog.recoverOps` and `SyncLogic.projectOps` repeat the same `Undo` and `Redo` match. `CoreEventDispatch.carriedBody` and `EventLog.withCarried` both wrap those cases as `Some ops`.
4. **Judgement — Mysterious name** — `ClientHistory` field `kept` does not say that it holds actions taken off the undo and redo stacks.
5. Clean scan. `standards-scan.py` exited 0. No line breaks a cited limit. No glossary phrase hit. `History.fs` stays 800 lines.

## 2. Spec

1. **Partial** — `BootCache.foldLog` resolves a target-only undo or redo only when that target is in the delta. A target already inside the snapshot is not in the delta, so that fold has no ops to apply.
2. Closed in this diff, not an open finding: a posted undo whose target is still `EventId.zero` waits until `ClientHistory.retarget` fills it. The pending copy keeps the resolved ops. `SyncBatch.toWireBatch` strips them. `SyncPlanner.restorePending` and `ResidentProjection.pendingOps` then read `Ev.ops`.

## 3. Arch

1. No findings.

## 4. Totals

1. Standards: 4 findings. Worst: `EventLog.restore` rebuilds the log once per folded event.
2. Spec: 1 finding. Worst: a boot delta cannot resolve a target that is already inside the snapshot.
3. Arch: 0 findings. Worst: none.
