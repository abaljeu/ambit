# 11 — Type while others change
**Status**: 
One job: The outliner in edit mode sees the edit post after a one-second pause in typing, with edit mode still on. The story Type while others change in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [10 — Merge in progress](plan/client-never-fails/issues/10-merge-in-progress/10-merge-in-progress-ticket.md)

## Inputs

### Working

[ ] `RowView.Layout.onEditKeyDown` in `src/Client/RowView.fs`.
[ ] `AutoDownloadTimer.armOrRearm` in `src/Client/AutoDownloadTimer.fs` takes `delayMs` and `onFire`.
[ ] `UpdateHelpers.commitTextEdit` in `src/Client/UpdateHelpers.fs` sets `mode` to `Selecting` after a post today.
[ ] `armOrRearm` clears the previous timeout id, stores the new id, and clears it when the callback fires.
[ ] `commitTextEdit` posts through `applyAndPost` when `tryTextCommitOps` returns ops.
[ ] No fact calls `commitTextEdit` or `onEditKeyDown`.
[ ] `AutoDownloadTimer.armOrRearm` is the timer helper. The auto-download caller passes its own delay. This ticket passes 1000 for the edit box.
[ ] [[src/Client/RowView.fs]] — `onEditKeyDown` re-arms the timer on each key.
[ ] [[src/Client/UpdateHelpers.fs]] — `commitTextEdit` posts the text and leaves `mode` as `Editing`.
[ ] [[src/Client/AutoDownloadTimer.fs]] — `armOrRearm` holds the timer. The delay for this path is 1000.

### Reference

[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: the auto-download delay. `armOrRearm` keeps that caller's delay.
[ ] Closed: leaving edit mode when the edited Node loses its Header. That case is 12 — Edited Node removed.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `onEditKeyDown` so each key calls `armOrRearm` with delay 1000.
3. [ ] Change `commitTextEdit` so a fired timer posts the text and `mode` stays `Editing`.
4. [ ] Add the fact named in Outputs.

## Outputs

[ ] `src/Client/RowView.fs`
[ ] `src/Client/UpdateHelpers.fs`
[ ] `src/Client/AutoDownloadTimer.fs`
[ ] `tests/Server.Tests/BrowserResidencyClientTests.fs` — add ``commitTextEdit posts text and stays Editing``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/11-type-while-others-change-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``commitTextEdit posts text and stays Editing``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/11-type-while-others-change-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
