# Follow-up — Issue 13 closure

**Date:** 2026-08-22
**Prior work:** [[13-migrate-producers-full-list-replace-build.md]]

## Issue / board

- [[../issues/13-migrate-producers-full-list-replace-wire.md]] — Status `done`, acceptance boxes checked.
- [[../../../WORK.md]] — issue 13 not in Pending (already removed).

## Grep — span Replace outside ChildListWire

### `src/` producers (Change planners)

| File | Pattern | Verdict |
| --- | --- | --- |
| [[../../../src/Shared/ChildListWire.fs]] | Central `Op.Replace(..., 0, fullOld, fullNew)` | Canonical |
| [[../../../src/Shared/AmbleRun.fs]] | Direct `Op.Replace` at index 0 with full `existing` children | Wire-valid; pre-existing |
| [[../../../src/Shared/documents/DocumentColdParse.fs]] | Direct `Op.Replace` at index 0; `remainingOld` may omit ids also planned for delete | Wire-valid per [[wire-full-list-replace-contract.md]]; delete ops paired |

All catalogue producers (Client UpdatePaste/Move/Ops/Helpers; Shared ImportText, FileNodeOps, ViewModelDeleteOps, ViewModelJoinOps, LazyLoadReconciliation, Paste, ChangeAmendment) route through `ChildListWire.*`.

No `src/` emission with `index > 0`.

### Tests / infra (not wire producers)

Span or non-zero `index` `Op.Replace` remain only in test fixtures and apply/serialize paths, e.g. [[../../../tests/Shared.Tests/HistoryTests.fs]], [[../../../tests/Shared.Tests/ViewModelMoveOpsTests.fs]], [[../../../tests/Server.Tests/StateEndpointTests.fs]], [[../../../tests/Shared.Tests/SerializationTests.fs]]. Harmless for app wire posts; noted in build report.

## Open (out of scope for 13)

- §10 JSON field rename (`oldList`/`newList`, drop `index`).
- Issue 10 order polish.
- Optional: migrate AmbleRun / DocumentColdParse to `ChildListWire.replace` for consistency (behaviour already wire-valid).
