# CoreChangesAccepted: Event-only ack

Date: 2026-09-16

## Type shape after change

```fsharp
type CoreChangesAccepted =
    { revision: Revision
      events: Gambol.Shared.Events.Event list
      externalChanges: bool
      message: string option
      isReady: bool }
```

`changes: Change list` is deleted. `CoreChanges.accepted` / `mergeAccepted` take and merge Events only.

## Behavior

- Mailbox ACK (`acceptedFromPosted`) sets `events = [ stored ]` only.
- Persist confirmations (File/Db) become Events via `Event.ofChange` before filling `CoreChangesAccepted`.
- `CoreEventDispatch.withConfirmedOps` reads confirmed Ops from `accepted.events`.
- HTTP `Api.postEvents` already returned `accepted.events`; unchanged.

`postChange` / `postGraphOnlyChange` doors remain Change-shaped adapters into the Event path; they no longer twin a Change-list ack beside `events`.

## Files touched

- `src/Server/Core/CoreChanges.fs`
- `src/Server/Core/CoreMailbox.fs`
- `src/Server/Core/CoreEventDispatch.fs`
- `src/Server/Core/FileAgent.fs`
- `src/Server/Core/DbAgent.fs`
- `tests/Server.Tests/CoreChangesTests.fs`
- `tests/Server.Tests/CoreCredentialsTests.fs`
- `tests/Server.Tests/CredentialedChangePostsTests.fs`
- `tests/Server.Tests/CoreRuntimeTests.fs`
- `tests/Server.Tests/FileAgentFailureTests.fs`
- `tests/Server.Tests/DatabaseProjectionContractTests.fs`

## Verification

```bash
dotnet build tests/Server.Tests -c Debug
dotnet test tests/Server.Tests -c Debug --no-build --filter \
  "FullyQualifiedName~CoreChangesTests|FullyQualifiedName~CoreCredentialsTests|FullyQualifiedName~CredentialedChangePostsTests|FullyQualifiedName~CoreRuntimeTests|FullyQualifiedName~FileAgentFailureTests"
```

Result: build ok; 28 passed, 0 failed. No commit.
