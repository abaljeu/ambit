# 10 — Merge in progress build

## Stage

[x] Stage is build in [10 — Merge in progress](CONTEXT.md).

## Edits

[x] `reconcileAck` in `src/Shared/SyncLogic.fs` accepts a reply that carries our own changes, and returns `AckReconcile.Ignored` when a different reply arrives while pending extends the submitted prefix.
[x] `Update.reconcileSubmit` in `src/Client/Update.fs` keeps that `Ignored` result. `applyIgnoredSubmitAnswer` posts the remainder with `SubmitPendingBatch` and leaves Sync `Sending`.
[x] ``different post reply while more changes are pending is Ignored`` expects `AckReconcile.Ignored`.
[x] ``Ignored reply posts the rest of the pending changes`` expects the remainder batch, Sync `Sending`, an empty catch-up, and the local graph text.

## Check

[x] Verdict is `pass` in [10 — Merge in progress check](plan/client-never-fails/reports/10-merge-in-progress-check.md).

## Status

[x] Ticket status is `coded`.
