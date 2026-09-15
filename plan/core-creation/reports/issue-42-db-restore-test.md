# Issue 42 — DbAgent Event persist + restore test

Date: 2026-09-15. Ticket: [[../issues/42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]]. Spec: [[../arch.md|Core creation architecture]] Story **Caller, persist, and Poll** persist migrate hop. Related seam map: [[issue-42-persist-seam-map.md]].

## What was added

One fact in [[tests/Server.Tests/Issue42PersistHandlersTests.fs]]: ``EventLog.restore seeds mailbox across Db restart``. Pattern matches [[tests/Server.Tests/DbAgentTests.fs]] / [[tests/Server.Tests/TestBackend.fs]]: `requireDbConnStr`, `resetTestDatabase`, `admittedHostDb (DbAgent.create …)`, `postChange`, dispose, create again, then `eventHistory` asserts the Change Event (`submissionId` + `EventLog.nextId`).

## Command

```text
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~Issue42PersistHandlersTests"
```

## Result

**Pass.** Failed: 0, Passed: 4, Skipped: 0, Duration: ~2s (Gambol.Server.Tests.dll net10.0).
