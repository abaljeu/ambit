# 31 — Load without a race
**Status**: 
One job: The outliner gets a Load reply applied like any other server reply, so an expand after a Notice works. The story Load without a race in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [30 — Delete includeWorkspace](plan/client-never-fails/issues/30-delete-include-workspace/30-delete-include-workspace-ticket.md)

## Inputs

### Working

[ ] `SyncLogic.applyLoadResponse` in `src/Shared/SyncLogic.fs` returns `Error` of a string `raced Load answer` when the answer is nodes-only and either pending local work is present or the event id differs.
[ ] `Update.update` matches LoadDone in `src/Client/Update.fs`.
[ ] The guard treats a nodes-only reply with pending work, and a nodes-only reply whose event id differs, as the same `Error` of a string.
[ ] Other replies already go through `applySyncResponse`.
[ ] ``applyLoadResponse answer-only with pending is raced Load answer`` and ``applyLoadResponse ignores a Notice when guarding a raced Load answer`` in [[tests/Shared.Tests/SplitOriginTraceTests.fs]].
[ ] ``answer-only Load at the same event id refuses pending local work`` and ``answer-only Load refuses an event id mismatch`` in [[tests/Shared.Tests/ClientHistoryRuntimeTests.fs]].
[ ] `SyncLogic.applySyncResponse` is the apply the Load reply uses after the guard is gone.
[ ] [[src/Shared/SyncLogic.fs]] — `applyLoadResponse`.
[ ] [[src/Client/Update.fs]] — LoadDone calls that apply.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/31-load-without-race-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: ``answer-only Load at the same event id preserves ClientHistory``. It already applies through `applyLoadResponse` when the ids match and the queue is empty.
[ ] Closed: Poll while pending changes. That path is 32 — Poll with pending changes.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `applyLoadResponse` so it applies the reply through `applySyncResponse`. Delete the `raced Load answer` guard.
3. [ ] Change LoadDone in `src/Client/Update.fs` so it uses that apply.
4. [ ] Change the four facts named in Outputs.

## Outputs

[ ] `src/Shared/SyncLogic.fs`
[ ] `src/Client/Update.fs`
[ ] `tests/Shared.Tests/SplitOriginTraceTests.fs` — change the two raced-answer facts
[ ] `tests/Shared.Tests/ClientHistoryRuntimeTests.fs` — change the two answer-only Load facts

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/31-load-without-race-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``applyLoadResponse answer-only with pending is raced Load answer``
  - `tests/Shared.Tests`: ``applyLoadResponse ignores a Notice when guarding a raced Load answer``
  - `tests/Shared.Tests`: ``answer-only Load at the same event id refuses pending local work``
  - `tests/Shared.Tests`: ``answer-only Load refuses an event id mismatch``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/31-load-without-race-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
