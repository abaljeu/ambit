# 05 — Fill a placeholder
**Status**: coded
One job: The outliner sees an Absent child become a normal row when its Header arrives. The story Fill a placeholder in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [03 — See a moved Node](plan/client-never-fails/issues/03-see-moved-node/03-see-moved-node-ticket.md); [04 — Use a placeholder](plan/client-never-fails/issues/04-use-placeholder/04-use-placeholder-ticket.md)

## Inputs

### Working

[x] ``ResidentProjection.installWantAnswer`` in `src/Shared/ResidentProjection.fs` merges `nodes` and `edges`.
[x] ``SyncLogic.graphAfterWant`` calls `installWantAnswer` in `src/Shared/SyncLogic.fs`.
[x] ``ViewModelDomPlan.planPatchDOM`` in `src/Shared/ViewModelDomPlan.fs` emits `RecreateRow` when the row kind changes.
[x] `graphAfterWant` is the production caller of `installWantAnswer`.
[x] `planPatchDOM` compares `Map.tryFind` on the old model with the new Header after 03 — See a moved Node.
[x] `View.render` calls `planPatchDOM`. This ticket leaves `src/Client/View.fs` as it is.
[x] ``installWantAnswer accepts a Child already Resident`` in [[tests/Shared.Tests/WantTests.fs]] merges an edge when the child Header is already stored.
[x] ``planPatchDOM recreates row when Unloaded leaf becomes Loaded with children`` in [[tests/Shared.Tests/ViewModelRowStateTests.fs]] expects `RecreateRow` when row chrome changes.
[x] ``applySyncResponse Load Fetch answer installs through installWantAnswer`` in [[tests/Shared.Tests/SyncLogicTests.fs]] installs a Want answer through `graphAfterWant`.
[x] ``stale installWantAnswer after split drops suffix and restores hello text`` in `tests/Shared.Tests/SplitOriginTraceTests.fs` merges a Header from the `nodes` list.
[x] [[src/Shared/ResidentProjection.fs]] — `installWantAnswer` stores a Header delivered for a child id that is already on a parent list.
[x] [[src/Shared/ViewModelDomPlan.fs]] — `planPatchDOM` emits `RecreateRow` when a placeholder row gains a Header.

### Reference

[x] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[x] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[x] Closed: `ChildrenStatus` and Bullet color #444444.
[x] Closed: `src/Shared/SyncLogic.fs`. `graphAfterWant` already calls `installWantAnswer`.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change `installWantAnswer` so a `nodes` list that carries a Header for an id already on a parent list returns `Ok` and stores that Header.
3. [x] Change `planPatchDOM` so the next plan emits `RecreateRow` when that id gains a Header.
4. [x] Add the three facts named in Outputs.

## Outputs

[x] `src/Shared/ResidentProjection.fs`
[x] `src/Shared/ViewModelDomPlan.fs`
[x] `tests/Shared.Tests/WantTests.fs` — add ``installWantAnswer adds Header for Absent child already on parent list``
[x] `tests/Shared.Tests/ViewModelTests.fs` — add ``planPatchDOM recreates row when placeholder gains Header``
[x] `tests/Shared.Tests/SyncLogicTests.fs` — add ``applySyncResponse nodes-only answer fills Absent child Header``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/05-fill-placeholder-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``installWantAnswer adds Header for Absent child already on parent list``
  - `tests/Shared.Tests`: ``planPatchDOM recreates row when placeholder gains Header``
  - `tests/Shared.Tests`: ``applySyncResponse nodes-only answer fills Absent child Header``
[x] All executed tests pass.
[x] Write the verdict to `plan/client-never-fails/reports/05-fill-placeholder-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
