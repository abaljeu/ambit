# 146 — Server logs startup-repair deletions

**Status:** `defined`

One job: When the server cleans up broken items while starting, it records each removal like any other change. A page that was open across the restart then learns about the removal and drops those items ([[plan/core-refinement/reports/missing-changes-reload-findings.md]] §6 F9).

Type: coding

Blockers: none

## Inputs

### Working

- [[src/Server/DatabaseProjection.fs]]
  - the startup sweep (around 540–590)
  - `ProjectionMaintenanceResult` (`deletedIds`, `requiresReload`)
- [[src/Server/Core/DbAgent.fs]]: `applyMaintenance` (around 110–125) reloads or trims the graph, with no event
- [[src/Shared/ProjectionOwnershipRepair.fs]] `ProjectionOwnershipRepair.plan`: what the repair removes and re-parents
- [[src/Server/Core/CoreEventDispatch.fs]]: how a change event is stored
- Tests: [[tests/Server.Tests/DbAgentTests.fs]]

### Reference

- [[doc/current/persistence-model.md]] (repair reload lines)
- [[plan/core-refinement/reports/missing-changes-reload-findings.md]] §1 (a removal reaches a page as a new child list for each parent)

## Do not load

- client code

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand `applyMaintenance`. After a repair that is not a no-op, store one `Change` event with `Op.Replace (parent, fullBefore, fullAfter)` for each parent whose child list changed. `fullBefore` comes from the graph before the repair, and `fullAfter` from the graph after it. Use the event path from [[src/Server/Core/CoreEventDispatch.fs]], so the event shares the write rule of 144.
3. [ ] Keep the reload behavior.
4. [ ] Expand the tests in Outputs.

## Outputs

- Changed files: [[src/Server/Core/DbAgent.fs]], and the repair result in [[src/Server/DatabaseProjection.fs]] if it must carry the parents
- Added tests:
  - `Server`: ``Startup repair stores a change for each parent it edits``
  - `Server`: ``A no-op startup repair stores no event``

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/146-server-logs-startup-repair-deletions-stop.md`, then ask.

- the sweep runs before the event log can accept a write
- the repair changes data that a child-list `Replace` cannot express (name it)

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `Server`: ``Startup repair stores a change for each parent it edits``
  - `Server`: ``A no-op startup repair stores no event``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/146-server-logs-startup-repair-deletions-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
