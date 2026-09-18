# ActorStop admits once

Date: 2026-09-15

Ticket: [[../issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]]. Status stays `coded`. No commit.

## 1. Extra admits

[[src/Server/Core/CoreMailboxBackend.fs]] `dispatchActorStop` admitted the same secret more than once:

1. `admitCaller` — for Authority `"Actor"` this already uses `CoreAuth.admit (pool.isLive secret)`.
2. A second `CoreAuth.admit (context.pool.isLive caller.secret)` after that success.
3. [[src/Server/Core/CoreActorPool.fs]] `finish` called `runAdmit` again, then dropped the live row.

## 2. Kept vs removed

Kept:

1. `admitCaller` as the one ActorStop admission (live Actor via `pool.isLive`; unauthenticated / non-live still refuse).
2. Authority gate after that success: only `"Actor"` may stop. Browser, Parse, and Test still refuse. The live row is not dropped.
3. `recordActorFinished` then `pool.finish` (drop the live row and cancel; reply that result).
4. Public `CoreActorPool` type, `isLive`, `admit`, `drop`, and `finish`. The pool is not hidden.

Removed:

1. The second `CoreAuth.admit (pool.isLive ...)` in `dispatchActorStop`.
2. `runAdmit` inside `runFinish`. Finish is a lifecycle drop, not a door.

## 3. pool.finish

`finish` still exists and still drops (`runDrop` / cancel and remove the live row). It no longer calls `CoreAuth.admit`. Mailbox is the only caller of `finish`. Direct `pool.finish` on a dead secret now returns `Ok ()` and is a no-op drop, because that path is not the Core door.

Did not start pool-as-door, two Histories, CSS `actor-*`, or unread `StartActorRequest.revision`.

## 4. Tests

Seam: [[tests/Server.Tests/CoreMsgActorCasesTests.fs]] CoreMailbox `actorStop`.

1. `ActorStop consults isLive once for a live Actor` — red first (2, then 1).
2. `live Actor stop succeeds and already-finished cannot` — real `CoreActorPool.create`.
3. `ActorStop of an already-finished Actor is refused`.
4. `ActorStop with Test credentials is refused`.
5. `ActorStop with Browser credentials is refused`.
6. `ActorStop with Parse credentials is refused`.

Foreground:

```
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~CoreMsgActorCasesTests|FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~CoreActorPoolTests|FullyQualifiedName~TestActorHelloTests|FullyQualifiedName~ActorCoreChangesDoorTests"
```

Passed: 50. Failed: 0.

Client compile gate skipped: Client/Shared were not edited.

## 5. Blockers

None.
