# 08 — Apply Replace when parent Header is present
**Status**: 
One job: A parent change applies when the parent Header is present. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [07 — Send child list on command response](plan/client-never-fails/issues/07-send-child-list-on-command-response/07-send-child-list-on-command-response-ticket.md)

## Inputs

### Working

[ ] `ResidentProjection.applyOpAllowing` matches `Op.Replace` in `src/Shared/ResidentProjection.fs` and requires `GraphChildren.isLoaded`.
[ ] `GraphMutate.replace` returns `Error` of a string `parent children not loaded` when `GraphChildren.isLoaded` is false. The parent Header check is the `Map.tryFind` above that gate.
[ ] `applyOpAllowing` returns `ApplyResult.Unchanged` when the parent child list is absent.
[ ] `Graph.replace` forwards to `GraphMutate.replace`.
[ ] ``Graph.replace rejects Unloaded parent`` in [[tests/Shared.Tests/ModelTests.fs]] expects `Error` of a string `parent children not loaded`.
[ ] ``applyServerTail skips structural Replace on Unloaded parent`` in [[tests/Shared.Tests/SyncLogicTests.fs]] expects the Replace to be skipped.
[ ] The Header miss in `GraphMutate.replace` already returns `Error` of a string `parent not found`.
[ ] [[src/Shared/ResidentProjection.fs]] — `applyOpAllowing` applies `Op.Replace` when the parent Header is present.
[ ] [[src/Shared/GraphMutate.fs]] — `replace` drops the `parent children not loaded` gate and keeps the Header gate.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/08-apply-replace-when-parent-header-present-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: the new-child-without-Header case. That case is 03 — See a moved Node.
[ ] Closed: the `ChildrenStatus` type delete.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `applyOpAllowing` so `Op.Replace` runs when the parent Header is present.
3. [ ] Change `GraphMutate.replace` so the parent Header is the gate. Drop `parent children not loaded`.
4. [ ] Change the two facts named in Outputs. Assert the child list, and leave `Graph.childrenStatus` uncalled.

## Outputs

[ ] `src/Shared/ResidentProjection.fs`
[ ] `src/Shared/GraphMutate.fs`
[ ] `tests/Shared.Tests/ModelTests.fs` — change ``Graph.replace rejects Unloaded parent``
[ ] `tests/Shared.Tests/SyncLogicTests.fs` — change ``applyServerTail skips structural Replace on Unloaded parent``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/08-apply-replace-when-parent-header-present-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[ ] The arch names `applyOpAllowing` for this step. `GraphMutate.replace` is in this ticket because it is the other Replace caller of `GraphChildren.isLoaded`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``Graph.replace rejects Unloaded parent``
  - `tests/Shared.Tests`: ``applyServerTail skips structural Replace on Unloaded parent``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/08-apply-replace-when-parent-header-present-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
