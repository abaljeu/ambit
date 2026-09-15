# Actor CoreChanges mailbox door

Date: 2026-09-15

Ticket: [[../issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]]. Remaining door from [[mailbox-single-door.md]] section 2 item 1 (Second CoreChanges handle). Status stays `coded`. Did not implement remaining doors 2 (pool lifecycle) or 3 (two Histories). No commit.

## 1. Second handle

[[src/Server/Core/CoreMailbox.fs]] `coreChanges` is the public CoreChanges door: reads unwrap persist errors with `failwith`; `isReady` is [[src/Server/Core/MailboxHost.fs]] `isReady`; posts are mailbox `PostChange` / `PostGraphOnlyChange` / `ActorStop`.

Actors did not receive that handle. [[src/Server/Core/CoreMailboxBackend.fs]] `dispatchStartActor` built a private `makeCoreChanges` from the raw `MailboxProcessor<CoreMsg>`:

1. `getRevision` — persist `Error` became `Revision 0`.
2. `getChangesSince` — persist `Error` became `[]`.
3. `isReady` — hardcoded `true`.
4. Posts still `Post`ed CoreMsg, but the handle was a second constructor, not `CoreMailbox.coreChanges`.

`CoreActorPool.schedule` ran the Actor body with that private handle, so Actor reads skipped the public door's error policy.

## 2. Change

1. [[src/Server/Core/CoreMailboxBackend.fs]] — Removed `makeCoreChanges`. `Started` returns the processor plus `bindCoreChanges`. `dispatchStartActor` schedules `make caller` from that bound factory.
2. [[src/Server/Core/CoreMailbox.fs]] `host` — After `MailboxHost.create`, `bindCoreChanges (coreChanges created)`. Actors receive the public door.
3. [[tests/Server.Tests/ActorCoreChangesDoorTests.fs]] — Door tests. Registered in [[tests/Server.Tests/Gambol.Server.Tests.fsproj]].

Did not change Pool start/admit/drop, Actor kind CSS, ActorStop double admit, StartActorRequest.revision, mailboxHistory vs persist undo, or ActorStarted authority `"Actor"`. Left `dispatchStartActor` `getState` persist-error fallback (`Graph.create ()`) for Pool expand; that is not the CoreChanges handle.

## 3. Error policy

Actor `getRevision` / `getChangesSince` now call `CoreMailbox.getRevision` / `getChangesSince`, which unwrap persist `Error` with `failwith`. Failure is not `Revision 0` or `[]`. `isReady` is the host probe. Posts still admit on CoreMsg before PersistHandlers.

## 4. Tests

Seam: Actor-received `CoreChanges` is `CoreMailbox.coreChanges`.

1. `Actor getRevision surfaces persist error instead of Revision 0`
2. `Actor getChangesSince surfaces persist error instead of empty list`
3. `Actor isReady uses mailbox host isReady`
4. `Actor postChange on scheduled handle reaches persist`

Foreground:

```
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~ActorCoreChangesDoorTests"
```

Passed: 4. Failed: 0. First test was red (`None` vs `Some "revision unavailable"`) before the bind.

Then:

```
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~ActorCoreChangesDoorTests|FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~CoreRuntimeTests|FullyQualifiedName~CoreMsgActorCasesTests|FullyQualifiedName~CredentialedChangePostsTests|FullyQualifiedName~TestActorHelloTests|FullyQualifiedName~CoreActorPoolTests|FullyQualifiedName~GraphOnlyChangePostTests|FullyQualifiedName~CoreCredentialsTests"
```

Passed: 64. Failed: 0.

```
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~ActorCoreChangesDoorTests|FullyQualifiedName~FileAgentFailureTests"
```

Passed: 10. Failed: 0.

Client compile gate skipped: Client/Shared were not edited.

## 5. Left for later

1. Pool as second lifecycle door — public CoreActorPool (startActor / admit / drop / isLive / schedule); Actor kind as CSS/`actor-*`; unread `StartActorRequest.revision`. ActorStop double admit is closed in [[actorstop-single-admit.md]].
2. Two Histories — persist undo vs mailbox-owned mailboxHistory; ActorStarted authority string `"Actor"`; graph restart does not restore Actor lifecycle.

## 6. Assumptions

1. Binding `CoreMailbox.coreChanges` after `MailboxHost.create` is the public door. The backend cannot call `CoreMailbox` at compile time (file order).
2. Public read policy stays `failwith` on persist Error. This cut does not change that type to `Result`.
3. Pool `getState` swallow (`Graph.create ()`) is not this door.
