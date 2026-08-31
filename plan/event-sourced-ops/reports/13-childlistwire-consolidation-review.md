# Issue 13 — ChildListWire consolidation review

**Date:** 2026-08-22
**Branch:** `w/event-sourced-ops`
**Issue:** [[../issues/13-migrate-producers-full-list-replace-wire.md]]

## Summary

Parallel migration landed cleanly: all listed producers route child-list edits through [[src/Shared/ChildListWire.fs]]. One duplicate pattern (sparse index removal in delete hard-delete paths) was merged. No dead span-wire helpers remain in producers; [[src/Shared/GraphMutate.fs]] full-list validation (`isFullListWireReplace`) is separate apply-layer logic and correctly untouched.

## Already well-centralized (no action)

| Area | Notes |
| --- | --- |
| [[src/Shared/ChildListWire.fs]] core API | `removeRange`, `insertAt`, `updateChildAt`, `append` all delegate to `replace`; list transforms (`dropRange`, `insertInto`, `updateAt`) are shared primitives. |
| Client producers | [[src/Client/UpdatePaste.fs]], [[src/Client/UpdateMove.fs]], [[src/Client/UpdateOps.fs]], [[src/Client/UpdateHelpers.fs]] — each calls one ChildListWire helper per edit; no raw `Op.Replace` with non-zero index. |
| Shared producers | [[src/Shared/FileNodeOps.fs]], [[src/Shared/ImportText.fs]], [[src/Shared/Paste.fs]], [[src/Shared/ViewModelJoinOps.fs]], [[src/Shared/ChangeAmendment.fs]], [[src/Shared/dotnet/LazyLoadReconciliation.fs]] — same pattern. |
| Amendment path | [[src/Shared/ChangeAmendment.fs]] `tryAmendReplace` rejects `index <> 0`, then `ChildListMerge.resolve` + `ChildListWire.replace`. |
| Apply / validation | [[src/Shared/GraphMutate.fs]] `replace` — wire-valid full-list CAS (`index = 0 && oldCount = childCount`) is apply-time only; not duplicated in planners. |
| Thin local wrappers | `FileNodeOps.appendOwnedOp`, `ViewModelJoinOps.removeCurrentChildOp` — one-liner domain naming; not worth inlining. |
| `ownedChildren` alias | [[src/Shared/Paste.fs]] and [[src/Shared/ImportText.fs]] both bind `ChildNode.owners`; trivial duplication, no behavioral risk. |

## Safe merges implemented

### 1. Sparse index removal helper

**Problem:** [[src/Shared/ViewModelDeleteOps.fs]] repeated the same “filter children by index set, then full-list replace” block in three places (`buildHardDeleteOps`, `buildHardDeleteOpsExcluding`, `planDeleteDroppedOwnedMany`).

**Change:**

- Added `excludingIndices` and `removeIndices` to [[src/Shared/ChildListWire.fs]].
- Replaced three inline filter/replace blocks in [[src/Shared/ViewModelDeleteOps.fs]] with `ChildListWire.removeIndices`.

### 2. Minor `edit` cleanup

- [[src/Shared/ChildListWire.fs]] `edit` — inlined `shortened` binding; same `dropRange` + `insertInto` + `replace` composition.

**Tests:** 35 focused Shared.Tests passed (`DeleteOps`, `ViewModelJoinOps`, `ChangeAmendment`, `ImportText`).

## Optional merges deferred (higher risk / marginal benefit)

| Candidate | Why deferred |
| --- | --- |
| Merge `buildHardDeleteOps` + `buildHardDeleteOpsExcluding` | Same shape, different parent-exclusion filter; merging adds parameter plumbing for ~15 lines saved. |
| Cross-parent move helper (`removeRange` + `insertAt` pair) | [[src/Client/UpdateMove.fs]] and [[src/Shared/dotnet/LazyLoadReconciliation.fs]] use two-parent two-op sequences with different ordering/context; a shared helper would hide important op ordering. |
| `removeRange` via `removeIndices` | Contiguous range is clearer as `dropRange`; converting to a set is slower and less readable. |
| Shared `ownedChildren = ChildNode.owners` | Two private bindings; extracting to a module adds indirection for no wire benefit. |
| Outline reconcile index filters | [[src/Shared/documents/OutlineReconcile.fs]], [[src/Shared/documents/OutlineDocumentWarm.fs]] use similar index-filter list logic on **document reconcile** paths, not Change wire posts — different layer; pulling into ChildListWire would blur wire vs parse concerns. |
| `DocumentColdParse` span Replace | Still uses internal span `Op.Replace` for parse reconcile (pre-wire batch); out of issue 13 producer scope. |

## No further consolidation recommended

- No unused `replaceChildrenSpan` or legacy span wrappers found in migrated producers.
- Producer call sites are already one-liners around the right ChildListWire primitive; further abstraction would reduce locality without reducing bug surface.
- `ChildListWire` internal structure is a thin stack: list transforms → `replace` → `Op.Replace(..., 0, ...)`. No redundant layers to remove.
