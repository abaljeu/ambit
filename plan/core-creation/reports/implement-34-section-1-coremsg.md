# Implement 34 section 1 CoreMsg / CoreMailboxBackend

Date: 2026-09-14. No commit.

## 1. What section 1 required

[[plan/core-creation/issues/34-outside-core-lifecycle-proof.md|34 — Outside Core lifecycle proof]] section **1. CoreMsg / CoreMailboxBackend**: mailbox Actor cases on the one CoreMsg loop. Contracts on arch **CoreMsg / CoreMailboxBackend**; seams **CoreMsg union** and **Credentialed Change posts** (Actor live-table admit).

1. **StartActorRequest** — `{ zoomId; focusId; commandId; graphIds }`; one payload for CoreMsg `StartActor`, later CoreMailbox `startActor`, and CoreActorPool.startActor.
2. **StartActor on CoreMsg** — carry that request; validate Authority and secret; hand off to CoreActorPool.startActor.
3. **Admit before Actor PostChange** — same credential fields plus live-row check before PersistHandlers.
4. **ActorStop of ActorResult** — `ActorSucceeded` only; drop live row and secret; request terminate without waiting. History ActorFinished records stay with section 4.
5. **No second mailbox** — Actor cases share the one CoreMsg loop with persist cases.

## 2. What was implemented

1. **Types** ([[src/Server/Core/CoreActorPool.fs]]) — `StartActorRequest`; `ActorResult = ActorSucceeded`. CoreActorPool gained `startActor`, `isLive`, `admit`, `drop`, and `finish`. The synchronized table holds live rows (secret, Focus, cancellation). `launch` also writes a live row so a wired pool admits Actor posts. `startActor` in this slice writes a live row and secret; it does not expand, select `test`, or schedule TestActor (section 3).
2. **CoreMsg** ([[src/Server/Core/CoreMailboxBackend.fs]]) — `StartActor` and `ActorStop` on the existing union. `ActorMailboxHandlers` is the Actor filling next to PersistHandlers (issue 31 predicted this). `noop` is persist-only (live check pass-through). `fromPool` wires the pool. Dispatch validates, then calls those handlers. No `ActorMsg` type.
3. **Doors** ([[src/Server/Core/CoreMailbox.fs]]) — `startFileWithActors` / `startDbWithActors`. Existing `startFile` / `startDb` keep `noop`. Public `startActor` / `actorStop` doors are not added (section 2).
4. **Composition** ([[src/Server/Core/CoreRuntime.fs]]) — one pool is created first and passed into both mailbox starts via `fromPool`.
5. **Tests** ([[tests/Server.Tests/CoreMsgActorCasesTests.fs]]) — CoreMsg union seam: StartActor handoff and refuse; Actor live-table admit and refuse; Browser post without a live row; ActorStop drops without waiting.

## 3. Tests run and results

Filter `FullyQualifiedName~CoreMsgActorCasesTests|FullyQualifiedName~CoreActorPoolTests|FullyQualifiedName~CredentialedChangePostsTests|FullyQualifiedName~CoreRuntimeTests`.

1. **Passed 30 / failed 0 / skipped 0.**
2. Server `bin` was locked by a running fullstack debug session (`netcoredbg` / `.NET Host`). F# compiled to `obj`. Tests ran against that compiled Server assembly. A later clean `dotnet build tests/Server.Tests` should be run when the server is stopped.
3. Client compile gate was not run: Shared and Client were not changed.

## 4. Incomplete or blocked

1. **History ActorFinished** — mailbox `ActorStop` calls `finish` (drop live row and secret, cancel without waiting). It does not append an ActorFinished member on Shared History. That is section 4.
2. **CoreMailbox doors** — no public `startActor` / `actorStop` / History read (section 2). Tests post `CoreMsg` on `MailboxHost.mailbox`.
3. **CoreActorPool.startActor body** — no expand / select `test` / create / ActorStarted / schedule (section 3).
4. **TestActor, CoreRuntime register, outside harness** — sections 5–7.
5. Ticket **Status** stays `ready-for-agent`. Section 1 only is coded. `done` is review approval of the whole ticket.

## 5. Later sections still needed

Yes. Sections 2–7 of [[plan/core-creation/issues/34-outside-core-lifecycle-proof.md|34 — Outside Core lifecycle proof]] remain. Sibling [[plan/core-creation/issues/35-browser-run-hello.md|35 — Browser Run hello]] stays blocked by 34.
