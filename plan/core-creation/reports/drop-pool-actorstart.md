# Drop pool live-row result type ActorStart

Date: 2026-09-15

Scope: pool live-row result only. Did not change History `ActorStarted`. Did not start Event unification. No commit.

## 1. Change

`focusId` is already on `StartActorRequest`. The minted secret is the only new value. A result record was not needed.

1. [[src/Server/Core/CoreActorPool.fs]] — deleted `type ActorStart = { secret; focusId }`. `startActor` is now `StartActorRequest -> (unit -> Graph) -> Result<Credential, string>`. `runStartActor` returns `Ok secret`.
2. [[src/Server/Core/CoreMailboxBackend.fs]] `dispatchStartActor` — matches `Ok secret`. Records History with `request.focusId`. Calls `context.pool.schedule secret (make caller)`.
3. Test fakes that returned `Ok { secret = ...; focusId = ... }` now return `Ok secret`. Unused `request` bindings in those fakes became `_`.

## 2. Test fakes

1. [[tests/Server.Tests/CoreMailboxDoorTests.fs]] — two recording pools (`actorStop` live-row drop; `actorStop` ActorFinished).
2. [[tests/Server.Tests/CoreMsgActorCasesTests.fs]] — `recordingPool` and the `ActorStop` `isLive` fake.

Grep after the edit: no pool `ActorStart` type, no `started.focusId`, no `started.secret`, no `Ok { secret = ...; focusId = ... }`. History still has `ActorStarted`. Tests that match `ActorEvent (_, ActorStarted ...)` were not edited.

## 3. Verify

Foreground (mailbox/pool; not the full suite). Client compile gate skipped: Client and Shared were not edited.

```
dotnet build tests/Server.Tests -c Debug
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~CoreMsgActorCasesTests|FullyQualifiedName~CoreActorPoolTests|FullyQualifiedName~TestActorHelloTests|FullyQualifiedName~ActorCoreChangesDoorTests"
```

Build succeeded, 0 warnings. First run: 51 passed, 1 failed (`CoreActorPool.startActor selects actor from command node text` — live-row gone before `liveFocusIds` assert). Immediate `--no-build` re-run of that Fact passed. Pre-existing flake; same as [[36-review-corrections.md]]. Not caused by this return-type change.

## 4. Leftover

None for this cut. Did not rename History `ActorStarted`. Did not add a domain Event named `ActorStart`.
