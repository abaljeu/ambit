# 145 — Server logs a notice only after applying it

**Status:** `defined`

One job: The server adds a status message to its record only after the message has taken effect on the server. The record then never holds a message that failed, so no page ever receives one ([[plan/core-refinement/reports/missing-changes-reload-findings.md]] §6 F5).

Type: bug-fixing

Blockers: none

## Inputs

### Working

- [[src/Server/Core/CoreMailboxLoop.fs]]
  - `dispatchItem` (`CoreQueue.Notice`): `logNotice` (259), then `applyNotice` (265). A failed apply only reports.
  - `applyNotice` (98–135)
- [[src/Server/Core/CoreEventDispatch.fs]]
  - `logNotice` (53)
  - `writeAxis` (188)
  - `markForPersist` (207–225): `logNotice` (219), then `writeAxis`
- [[src/Server/Core/CoreMailboxFilesAxis.fs]]: `writeNotice`, `applyNodeParsed`, `writeReceived`, `writePersisted`
- [[src/Shared/NoticeGraph.fs]] `writeGraph`: the shared rule
- Tests: [[tests/Server.Tests/DbAgentNoticeTests.fs]], and the mailbox loop tests under [[tests/Server.Tests/]]

### Reference

- [[plan/core-refinement/spec.md]] §2
- [[plan/core-refinement/cqrs-messages.md]]

## Do not load

- client code
- the parse thread and the persist thread bodies

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `dispatchItem`: run `applyNotice` first. On `Ok`, `logNotice`. On `Error`, report it and do not log.
3. [ ] Change `markForPersist` in the same way: write first, log on `Ok`.
4. [ ] A notice with no graph effect (`NodeParsing`, `NodePersisting`, `ParseFailed`, `PersistFailed`) is logged as today.
5. [ ] Expand the tests in Outputs.

## Outputs

- Changed files: [[src/Server/Core/CoreMailboxLoop.fs]], [[src/Server/Core/CoreEventDispatch.fs]]
- Added tests:
  - `Server`: ``A notice whose write fails is not logged``
  - `Server`: ``A notice whose write succeeds is logged after the write``

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/145-server-logs-a-notice-after-applying-it-stop.md`, then ask.

- a handler needs its own notice on the log before it writes (name it)
- the fix needs a file outside Inputs

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `Server`: ``A notice whose write fails is not logged``
  - `Server`: ``A notice whose write succeeds is logged after the write``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/145-server-logs-a-notice-after-applying-it-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
