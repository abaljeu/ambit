# 103 — Change reply is the event id

Coded [103 — Change reply is the event id](../issues/103-change-reply-is-the-event-id.md). Status is `coded`. Not committed: desktop git protocol leaves commits to the user.

## What the ticket required

`CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly` reply `Result<EventId, string>`. `CoreMailboxApi` rebuilds `CoreChangesAccepted` from the stored Ev. `message` leaves the ack type. Callers of `postEvents` and `postGraphOnly` keep that type.

## What changed

- `CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly` reply the stored Ev's id.
- `CoreMailboxApi` reads those ids through `Query.EventsSince` and builds `CoreChangesAccepted` with `CoreChanges.fromPosted`. `isReady` comes from `MailboxHost.isReady`.
- `message` left `CoreChangesAccepted`. The HTTP ack writes `None`.
- A failed ActorStart still returns the stored start and stop. A Cancel with no stored Ev is skipped on rebuild.

## How it was proved

- Resubmission id: [103 — Resubmission id](103-resubmission-id.md).
- REPL: `CoreChanges.fromPosted` on a plain Change and an amended Change. The rebuilt type has no `message` field.
- Tests: `postEvents returns the same CoreChangesAccepted from the event id` and `a resubmitted Change replies the stored id`. 136 related mailbox tests, plus parse/persist/load.

## Follow-ups

- `plan/core-refinement/` has no `CONTEXT.md`. `plan/CONTEXT.md` says that file is not backfilled onto existing Projects.
- SageFs session `967c9074` was reused, not created. It was not stopped.
- Not committed.

## Files

- `src/Server/Core/CoreMsg.fs`
- `src/Server/Core/CoreChanges.fs`
- `src/Server/Core/CoreMailboxApi.fs`
- `src/Server/Core/CoreMailboxEvents.fs`
- `src/Server/Core/CoreMailboxActors.fs`
- `src/Server/Core/FileAgent.fs`
- `src/Server/Core/DbAgent.fs`
- `src/Server/Api.fs`
- `tests/Server.Tests/CoreMailboxDoorTests.fs`
- `tests/Server.Tests/TestBackend.fs`
- `tests/Server.Tests/CoreCredentialsTests.fs`
- `tests/Server.Tests/GraphOnlyChangePostTests.fs`
- `tests/Server.Tests/FileAgentFailureTests.fs`
- `tests/Server.Tests/DbAgentFailureTests.fs`
