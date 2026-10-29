# 32 — Poll with pending changes
**Status**: 
One job: The outliner with changes not yet posted sees a Poll apply, and those changes post after it. The story Poll with pending changes in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [31 — Load without a race](plan/client-never-fails/issues/31-load-without-race/31-load-without-race-ticket.md)

## Inputs

### Working

[ ] `SyncPlanner.tryStartPoll` in `src/Shared/SyncPlanner.fs` emits `PollServer` only when the pending queue is empty and the state is `Idle`.
[ ] `SyncLogic.applySyncResponse` replays trailing pending after a precondition undo.
[ ] ``tryStartPoll returns no effects when queue is non-empty`` is the current gate.
[ ] A Poll response carries an empty undo list relative to a post of our own changes.
[ ] ``tryStartPoll returns no effects when queue is non-empty`` in [[tests/Shared.Tests/SyncPlannerTests.fs]].
[ ] ``applySyncResponse re-applies trailing pending after precondition undo`` in [[tests/Shared.Tests/SyncLogicTests.fs]].
[ ] `SyncLogic.reapplyLeftoverPending` is the replay helper `applySyncResponse` already uses.
[ ] [[src/Shared/SyncPlanner.fs]] — `tryStartPoll`.
[ ] [[src/Shared/SyncLogic.fs]] — post-reply undo and replay. A Poll passes an empty undo list.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/32-poll-with-pending-changes-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: `AckReconcile.Ignored` for a different reply. That case is 33 — Merge in progress.
[ ] Closed: the one-second typing pause.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `tryStartPoll` so it emits `PollServer` while pending changes wait.
3. [ ] Change the post-reply path so it undoes the posted changes and replays the server Events. A Poll uses an empty undo list, so those Events apply, and the pending changes post after that apply.
4. [ ] Change the planner fact and add the Sync fact named in Outputs.

## Outputs

[ ] `src/Shared/SyncPlanner.fs`
[ ] `src/Shared/SyncLogic.fs`
[ ] `tests/Shared.Tests/SyncPlannerTests.fs` — change ``tryStartPoll returns no effects when queue is non-empty``
[ ] `tests/Shared.Tests/SyncLogicTests.fs` — add ``pending changes post after Poll applies``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/32-poll-with-pending-changes-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``tryStartPoll returns no effects when queue is non-empty``
  - `tests/Shared.Tests`: ``pending changes post after Poll applies``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/32-poll-with-pending-changes-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
