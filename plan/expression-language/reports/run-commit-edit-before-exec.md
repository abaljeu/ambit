# Run commits the edit before Exec

Branch: `w/expr`. Tree left dirty. No commit.

## Commit path reused

[[src/Client/UpdateHelpers.fs]] `commitIfEditing` — the same path as Escape, zoom, undo, and cursor move. It calls `commitTextEdit` → `tryTextCommitOps` → `applyAndPost` `EditNode`, then sets mode to Selecting. Indent stays in edit mode (`tryTextCommitOps` in the same change). Find and Save do not commit. Run follows Escape/zoom/undo, not indent.

## What changed

`runAmbleOp` no longer reads the textarea while Editing. Sequence:

1. `commitIfEditing` so `SetText` lands in the graph (or no-op when already Selecting / text unchanged).
2. `AmbleRun.runPlanOnNode` reads `graph.nodes.[focusId].text` from that committed graph.
3. Apply Run ops and unfold as before. Concatenate commit effects with Run effects, same as undo.

Empty Run / Ignore still keeps the committed text. Unfold and `//` / error strings are unchanged.

## Files changed

- [[src/Shared/AmbleRun.fs]] — `runPlanOnNode`
- [[src/Client/UpdateAmbleRun.fs]] — commit then `runPlanOnNode`
- [[tests/Shared.Tests/AmbleRunTests.fs]] — SetText then `runPlanOnNode`

## Tests

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~AmbleRunTests"
```

Result: **Passed — 20/20**.

```
bash ./scripts/client.sh build
```

Result: Fable + esbuild succeeded. [[src/Server/wwwroot/UpdateAmbleRun.js]] imports `commitIfEditing` and `runPlanOnNode`.

Commit is Client MVU (DOM `readEditInputValue`). Shared tests lock Graph `setText` then `runPlanOnNode`. No browser in this pass.

## WORK.md mutations

- `add` [[.scratch/expression-language/reports/run-commit-edit-before-exec.md]] — HITL: while Editing, change the line and Ctrl+Enter; the graph text commits, then Run uses that text (not a stale line, not textarea-only)
