# Move selected node up — no view update

Date: 2026-08-17  
Branch: `w/large-node-cursor-perf`  
Related: [[implement-fix.md]], [[investigation.md]], [[further-speedups.md]]

## Symptom

**Move Up** (and similarly Move Down among siblings) updated the graph / selection but the outline DOM order did not change.

## Root cause

The selection-only `patchDOM` fast path in `src/Client/View.fs` treated “no `CreateRow` / `RemoveRow` / `RecreateRow`” as “non-structural — skip visible walk and `atCorrectPos` reordering.”

Sibling reorder keeps the same instance ids and typically emits only `PatchRow` (class/selection) or empty structural markers. Visible preorder still changes. Skipping the order walk left DOM siblings in the old sequence.

Selection-only `planPatchDOM` (same `siteMap`/`graph` refs) was not the misclassification; MoveUp rebuilds those. The bug was the Client heuristic after planning.

## Fix

- Shared: `ViewModelDomPlan.needsDomOrderWalk` — true if mutations are structural **or** `getVisibleInstanceIds` old ≠ new.
- Client: `patchDOM` uses that instead of Create/Remove/Recreate-only.
- CursorUp/Down: same visible order → still skips the full walk.

## Tests

In `tests/Shared.Tests/ViewModelTests.fs`:

- selection move still asserts `needsDomOrderWalk` is false
- sibling reorder up/down: no Create/Remove/Recreate, but `needsDomOrderWalk` is true

```text
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~planPatchDOM|…childIndex"
→ Passed 15
dotnet build src/Client -c Debug → succeeded
```

## Board (for root)

- `add` Active: [[plan/large-node-cursor-perf/move-up-view-regression.md]] — fix MoveUp no DOM reorder after view-opt (owner: this subagent; done pending root verify)
- `remove` that Active entry after root verifies
- Related Pending already: [[delete-children-cost.md]] (unchanged)
