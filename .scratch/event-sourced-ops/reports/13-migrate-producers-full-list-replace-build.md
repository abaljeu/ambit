# Report — Issue 13 migrate producers to full-list Replace wire shape

**Date:** 2026-08-22
**Branch:** `w/event-sourced-ops`
**Issue:** [[../issues/13-migrate-producers-full-list-replace-wire.md]]

## Summary

Client and Shared Change planners now emit wire-valid full-list `Replace` ops (`index = 0`, complete parent `oldList`/`newList` at the planning anchor). New [[../../../src/Shared/ChildListWire.fs]] centralises list edits (`insertAt`, `removeRange`, `edit`, `append`, `updateChildAt`, `replace`). [[../../../src/Shared/GraphMutate.fs]] validates placement and name conflicts on **introduced** children only when `index = 0` and `oldCount = childCount` (full-list wire replace), so apply behaviour matches former span inserts.

## Files changed

| File | Change |
| --- | --- |
| [[../../../src/Shared/ChildListWire.fs]] | New wire helper module |
| [[../../../src/Shared/Gambol.Shared.fsproj]] | Register `ChildListWire.fs` |
| [[../../../src/Shared/GraphMutate.fs]] | Introduced-child validation for full-list replace |
| [[../../../src/Client/UpdatePaste.fs]] | Paste/cut selecting and editing |
| [[../../../src/Client/UpdateMove.fs]] | Cross-parent move; same-parent via `edit` |
| [[../../../src/Client/UpdateOps.fs]] | Duplicate selection |
| [[../../../src/Client/UpdateHelpers.fs]] | Split at cursor |
| [[../../../src/Shared/ImportText.fs]] | Directory merge append |
| [[../../../src/Shared/FileNodeOps.fs]] | Create/insert file refs |
| [[../../../src/Shared/ViewModelDeleteOps.fs]] | Promote, span remove, TRASH append, batch remove |
| [[../../../src/Shared/ViewModelJoinOps.fs]] | Join remove/reparent |
| [[../../../src/Shared/dotnet/LazyLoadReconciliation.fs]] | Ref replace, trash, reparent |
| [[../../../src/Shared/Paste.fs]] | Cold paste / clipboard via `insertAt` |
| [[../../../src/Shared/ChangeAmendment.fs]] | Amb-conflict child uses full prior list |
| [[../../../tests/Shared.Tests/ImportTextTests.fs]] | Directory merge expectations |
| [[../../../tests/Shared.Tests/ViewModelJoinOpsTests.fs]] | Full-list join ops; fixture fix |

## Test results

```bash
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~ImportTextTests|FullyQualifiedName~ViewModelJoinOpsTests|FullyQualifiedName~DeleteOpsTests|FullyQualifiedName~ChildListMergeTests|FullyQualifiedName~ChangeAmendmentTests|FullyQualifiedName~LazyLoadReconciliationTests|FullyQualifiedName~FileNodeOpsTests"
```

Result: Failed 0, Passed 98, Skipped 0.

```bash
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~StateEndpointTests&FullyQualifiedName~same-parent"
```

Result: Failed 0, Passed 2, Skipped 0.

Client build: green (`dotnet build src/Client -c Debug`).

## Acceptance criteria

- [x] Every Client/Shared Change planner emits only full-list Replace on the wire.
- [x] Cross-parent move, paste, delete, import, join, lazy-load reconciliation, and file-node insert covered.
- [x] Focused tests updated; merge/amend tests still green.

## Remaining gaps

- §10 JSON field rename (`oldList`/`newList`, drop `index`) still open.
- Issue 10 order polish not started.
- Some Server/integration tests still construct span `Replace` in fixtures (not producers); harmless for wire posts from app code.
- No commit in this session.

## Board mutation (for root)

- **remove** — [[../issues/13-migrate-producers-full-list-replace-wire.md]] from Pending (verified on focused tests).
