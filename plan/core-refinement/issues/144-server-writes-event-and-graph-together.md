# 144 — Server writes the event and the graph together

**Status:** `defined`

One job: When the server saves a change, it saves the record of the change and the changed outline together, or neither. A crash can then never leave the server holding a change that it has no record of, and a restart never throws away the change history. Every page can always catch up from the records ([[plan/core-refinement/reports/missing-changes-reload-findings.md]] §6 F8).

Type: bug-fixing

Blockers: none

## Inputs

### Working

- [[src/Server/Core/CoreEventDispatch.fs]] `persistNew` (around 299–316): `persist` (projection write, 308), then `store` (event append, 311). These are two steps.
- [[src/Server/Core/DbAgent.fs]]
  - `applyEvent` → `processPostEvents` (482): the projection write
  - `writePersistedEvent` (393): the event row, outside the projection transaction
  - `loadReconciled` (48–75): clears the events table when `recover` drops the log (73)
- [[src/Server/Database.fs]]: `appendEvent` and the projection write functions; the transaction to share
- [[src/Shared/EventLog.fs]] `recover` (128–132): `graphId > logId` → `afterCheckpoint` drops the log
- [[src/Server/Core/FileAgent.fs]] `reconcileLoaded`: the same drop for the file agent
- Tests: [[tests/Server.Tests/DbAgentTests.fs]], [[tests/Server.Tests/DbAgentFailureTests.fs]]

### Reference

- [[doc/current/persistence-model.md]]
- [[plan/core-refinement/arch.md]]

## Do not load

- client code
- the persist thread and file writes (`DocumentPersistChange`)

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change the DbAgent change path. Append the event row and write the projection in one database transaction. Change the in-memory graph and log only after that commit.
3. [ ] Change `persistNew`: an append failure leaves the in-memory graph unchanged and returns `Error`.
4. [ ] Change `EventLog.recover`. `graphId > logId` returns an error value that names both ids. It does not drop the log. Contract `Database.clearEvents` from `loadReconciled`.
5. [ ] Change `FileAgent.reconcileLoaded` to the same rule, or stop if the file agent cannot write both together.
6. [ ] Expand the tests in Outputs.

## Outputs

- Changed files: each file under Working.
- Added tests:
  - `Server`: ``A failed event append leaves the graph unchanged``
  - `Server`: ``Event row and projection commit together``
  - `Shared`: ``Recover with the graph ahead of the log is an error and keeps the log``

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/144-server-writes-event-and-graph-together-stop.md`, then ask.

- the projection write and the event append cannot share a transaction
- the file agent cannot meet the rule
- startup must still run when `recover` returns the error (Alan decides what startup does)

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `Server`: ``A failed event append leaves the graph unchanged``
  - `Server`: ``Event row and projection commit together``
  - `Shared`: ``Recover with the graph ahead of the log is an error and keeps the log``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/144-server-writes-event-and-graph-together-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
