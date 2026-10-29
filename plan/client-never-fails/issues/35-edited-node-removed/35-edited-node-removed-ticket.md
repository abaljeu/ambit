# 35 — Edited Node removed
**Status**: 
One job: The outliner leaves edit mode when a Poll removes the Node being edited, and the view draws from the Graph. The story Edited Node removed in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [34 — Type while others change](plan/client-never-fails/issues/34-type-while-others-change/34-type-while-others-change-ticket.md)

## Inputs

### Working

[ ] `UpdateHelpers.adjustModeAfterServerApply` in `src/Client/UpdateHelpers.fs` switches to `Selecting` when the edited Node's text changes.
[ ] `ViewModelDomPlan.planPatchDOM` draws rows from the Graph.
[ ] The function reads `Map.tryFind editingId` on the previous Graph and on the new Graph and compares text.
[ ] A missing Header is `None` from `Map.tryFind`.
[ ] No fact calls `adjustModeAfterServerApply`.
[ ] `planPatchDOM` in `src/Shared/ViewModelDomPlan.fs` is the draw path. This ticket leaves that file as it is when it already draws from the Graph.
[ ] [[src/Client/UpdateHelpers.fs]] — `adjustModeAfterServerApply`.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/35-edited-node-removed-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: `src/Shared/ViewModelDomPlan.fs` when the plan already draws from the Graph.
[ ] Closed: edit during Load. That case is 36 — Edit during a Load.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `adjustModeAfterServerApply` so `mode` becomes `Selecting` when the edited NodeId has no Header.
3. [ ] Leave the row plan drawing from the Graph.
4. [ ] Add the fact named in Outputs.

## Outputs

[ ] `src/Client/UpdateHelpers.fs`
[ ] `tests/Server.Tests/BrowserResidencyClientTests.fs` — add ``adjustModeAfterServerApply selects when edited Header is absent``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/35-edited-node-removed-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``adjustModeAfterServerApply selects when edited Header is absent``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/35-edited-node-removed-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
