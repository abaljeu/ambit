# 02 — Survive a Server crash
**Status**: 
One job: The outliner keeps every reported event id backed by Events, because the Server writes the Event before the durable Graph. The story Survive a Server crash in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [01 — Accept Notices](plan/client-never-fails/issues/01-accept-notices/01-accept-notices-ticket.md)

## Inputs

### Working

[ ] ``CoreEventDispatch.persistNew`` has type `Context -> Caller -> Ev -> bool -> Result<Ev * CoreChangesAccepted option, string>` in `src/Server/Core/CoreEventDispatch.fs`.
[ ] `PersistHandlers` in `src/Server/Core/CoreMsg.fs` has `appendEvent`, `applyEvent`, and `replaceGraph: Notice -> Graph -> unit`.
[ ] `DbAgent.persistGraphProjection` and `DbAgent.appendPersistedEvent` in `src/Server/Core/DbAgent.fs`.
[ ] `FileAgent.processPostEvents` in `src/Server/Core/FileAgent.fs`.
[ ] `CoreMailboxLoop` copies `replaceGraph` in `src/Server/Core/CoreMailboxLoop.fs`.
[ ] `persistNew` runs `prepare`, then `persist` (`applyEvent`), then `store` (`appendNew`). The durable Graph write sits inside `applyEvent`, before the append.
[ ] `DbAgent.finishAppliedPostChange` calls `commitPostChange`, which runs `persistGraphProjection` before the append.
[ ] `FileAgent.processPostEvents` sets `loaded.state` before `appendEvent`.
[ ] `SyncLogic.applySyncResponse` already folds `response.events`. This ticket leaves that function as it is.
[ ] ``FileAgent applyEvent applies Ev Ops without leftover Change`` and ``DbAgent applyEvent applies Ev Ops without leftover Change`` in [[tests/Server.Tests/PersistApplyTests.fs]] call `applyEvent` then `appendEvent`.
[ ] ``File startup does not replay the event log`` and ``Db recover applies Ops Ev ahead of projection revision`` in [[tests/Server.Tests/MailboxHistoryDurabilityTests.fs]] restart from an appended log.
[ ] ``ActorStart persist Error does not keep Ev in mailbox log`` in [[tests/Server.Tests/Issue42PersistHandlersTests.fs]] covers an append `Error` of a string.
[ ] Private `applyThenAppend` in `tests/Server.Tests/PersistApplyTests.fs` already separates apply from append.
[ ] `DbAgent.createForTestWithDependencies` in `src/Server/Core/DbAgent.fs` is the seam for a projection `Error` of a string.
[ ] [[src/Server/Core/CoreEventDispatch.fs]] — append the Event, then write the durable Graph.
[ ] [[src/Server/Core/DbAgent.fs]] — `persistGraphProjection` runs after `appendPersistedEvent`.
[ ] [[src/Server/Core/FileAgent.fs]] — the durable Graph write runs after `appendEvent`.
[ ] [[src/Server/Core/CoreMsg.fs]] — change `replaceGraph` to return `Result<unit, string>` only when the Graph write's `Error` of a string must cross `PersistHandlers`.
[ ] [[src/Server/Core/CoreMailboxLoop.fs]] — pass that function through when its type changes.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/02-survive-server-crash-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: `src/Shared/SyncLogic.fs` and `src/Client`. The Browser already applies Events when the log has them.
[ ] Closed: placeholder rows, `ChildrenStatus`, and the reload prompt.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `persistNew` in `src/Server/Core/CoreEventDispatch.fs` so the in-memory apply finishes, `appendNew` stores the Event with its confirmed ops, and the durable Graph write runs after that append.
3. [ ] Change `DbAgent` so `persistGraphProjection` runs after `appendPersistedEvent`. A projection `Error` of a string leaves the appended Event in the log.
4. [ ] Change `FileAgent.processPostEvents` so the durable Graph write runs after `appendEvent`. A Graph write `Error` of a string leaves the appended Event in the log.
5. [ ] Change `PersistHandlers.replaceGraph` and `CoreMailboxLoop` only when that `Error` of a string must cross the handler. Otherwise leave both files as they are.
6. [ ] Add the two facts and change the two restart facts named in Outputs.

## Outputs

[ ] `src/Server/Core/CoreEventDispatch.fs`
[ ] `src/Server/Core/DbAgent.fs`
[ ] `src/Server/Core/FileAgent.fs`
[ ] `src/Server/Core/CoreMsg.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `tests/Server.Tests/PersistApplyTests.fs` — add ``postEvent appends the Event before the projection write``
[ ] `tests/Server.Tests/DbAgentFailureTests.fs` — add ``Db projection Error leaves the appended Event in the log``
[ ] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` — change ``File startup does not replay the event log`` and ``Db recover applies Ops Ev ahead of projection revision``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/02-survive-server-crash-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``postEvent appends the Event before the projection write``
  - `tests/Server.Tests`: ``Db projection Error leaves the appended Event in the log``
  - `tests/Server.Tests`: ``File startup does not replay the event log``
  - `tests/Server.Tests`: ``Db recover applies Ops Ev ahead of projection revision``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/02-survive-server-crash-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
