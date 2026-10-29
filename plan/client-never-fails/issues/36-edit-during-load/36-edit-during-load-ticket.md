# 36 — Edit during a Load
**Status**: 
One job: The outliner can edit while a Load is in flight. The story Edit during a Load in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [35 — Edited Node removed](plan/client-never-fails/issues/35-edited-node-removed/35-edited-node-removed-ticket.md)

## Inputs

### Working

[ ] `AutoDownloadTimer.armOrRearm` holds the edit timer after 34 — Type while others change.
[ ] `UpdateHelpers.commitTextEdit` posts while `mode` is `Editing`.
[ ] `SyncPlanner.tryStartLoad` sets `syncState` to `Loading` in `src/Shared/SyncPlanner.fs`.
[ ] `tryStartPoll` returns no effect for a state other than `Idle` with an empty queue. `Loading` is one of those states.
[ ] The edit timer is separate from that Poll gate.
[ ] No fact shows an edit firing while `syncState` is `Loading`.
[ ] `commitTextEdit` is the post the timer fires. It stays available during `Loading`.
[ ] [[src/Client/UpdateHelpers.fs]] — `commitTextEdit` still posts when `syncState` is `Loading`.
[ ] [[src/Client/RowView.fs]] — `onEditKeyDown` still re-arms the one-second timer when `syncState` is `Loading`.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/36-edit-during-load-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: `applyLoadResponse`. The Load reply path is 31 — Load without a race.
[ ] Closed: the reload prompt.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change the edit path so `Loading` leaves the one-second timer armed and leaves `commitTextEdit` free to post.
3. [ ] Leave the Load reply on `applyLoadResponse`.
4. [ ] Add the fact named in Outputs.

## Outputs

[ ] `src/Client/UpdateHelpers.fs`
[ ] `src/Client/RowView.fs`
[ ] `tests/Server.Tests/BrowserResidencyClientTests.fs` — add ``edit timer posts while Load is in flight``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/36-edit-during-load-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[ ] Stop when no guard blocks the edit during `Loading`. Record that the timer and `commitTextEdit` already run, and add the fact that shows it.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``edit timer posts while Load is in flight``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/36-edit-during-load-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
