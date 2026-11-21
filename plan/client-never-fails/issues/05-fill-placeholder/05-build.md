# 05 — Fill a placeholder build

Ticket: [05 — Fill a placeholder](05-fill-placeholder-ticket.md)

Scope: [05 — Fill a placeholder scope](../../reports/05-fill-placeholder-scope.md)

Check: [05 — Fill a placeholder check](../../reports/05-fill-placeholder-check.md)

## Work

[ ] `ResidentProjection.installWantAnswer` in `src/Shared/ResidentProjection.fs` folds a `nodes` list into `graph.nodes` and returns `Ok`. A Header for an id already on a parent list is stored. The parent list stays.

[ ] `ViewModelDomPlan.planPatchDOM` in `src/Shared/ViewModelDomPlan.fs` emits `RecreateRow` when that id gains a Header. `visibleRowMutation` takes the resident arm. `residentNeedsRecreate` sees no old Header, so `oldKind` is `None`.

[ ] Those two functions stay at their current text. The three Output tests pass against that text.

[ ] `SyncLogic.graphAfterWant` in `src/Shared/SyncLogic.fs` stays the production caller of `installWantAnswer`. `src/Client/View.fs` stays as it is.

## Tests

[ ] ``installWantAnswer adds Header for Absent child already on parent list`` in `tests/Shared.Tests/WantTests.fs`. Seam: `ResidentProjection.installWantAnswer`. An Absent child id is already on the parent list. A `nodes` list carries that Header and `edges` is empty. The result is `Ok`, the stored text is `arrived`, and the parent list still holds that id.

[ ] ``planPatchDOM recreates row when placeholder gains Header`` in `tests/Shared.Tests/ViewModelTests.fs`. Seam: `ViewModel.planPatchDOM`. The old model shows the Absent child. The new model adds that Header. The plan contains `RecreateRow` for that row.

[ ] ``applySyncResponse nodes-only answer fills Absent child Header`` in `tests/Shared.Tests/SyncLogicTests.fs`. Seam: `SyncLogic.applySyncResponse`. The graph has the Absent child on the parent list. The `SyncResponse` has that Header in `nodes` and an empty `childMap`. The result is `Ok`, the stored text is `arrived`, and the parent list still holds that id.

## Size

[ ] Source files changed: 0. Tests added: 3. Inside the [Size limit](../../context/size-limit.md). The Project file-count exception is 40 files. This build stays inside that count.

## Check

[ ] Verdict line: `pass`.

[ ] Ticket status is `coded`.
