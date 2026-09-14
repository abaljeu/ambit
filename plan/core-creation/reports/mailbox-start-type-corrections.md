# Mailbox start type corrections

Date: 2026-09-14. No commit.

## 1. Must-fix status

1. **dispatch too long** — `dispatch` is now a try/with over `runMsg`. Under the 40-line cap.
2. **Parameter explosion** — leftover Authority+secret is `Caller`. Dispatch helpers take one private `Loop` (credentials, persist filling, pool, error hooks) plus the message. `ActorMailboxHandlers` is gone. `PersistHandlers` stays (see below).
3. **Duplicated test setup** — [[tests/Server.Tests/CoreMsgActorCasesTests.fs]] uses `withHost` for temp-dir / host / dispose.

## 2. StartActor handoff

After `admitCaller` succeeds, the mailbox replies `Ok ()` and `Async.Start`s `pool.startActor`. The loop is not held. `RunSynchronously` is not used on this path. A later `startActor` post back to the mailbox cannot deadlock the StartActor reply.

## 3. Types

`StartActorRequest`:

```
type StartActorRequest =
    { zoomId: NodeId
      focusId: NodeId
      commandId: NodeId
      graphIds: NodeId list
      revision: Revision }
```

`LaunchRequest` is deleted. `runLaunch` / `launch` / `query` are deleted. Start keeps only mint-secret and `PutLive` on `runStartActor`. Name lookup, span plan, overlap, `PublicNumber`, and `Async.Start` of the actor body stay out (section 3).

`Caller`:

```
type Caller =
    { authority: Authority
      secret: Credential }
```

Used on CoreMsg `PostChange` / `StartActor` / `ActorStop`, CoreMailbox `postChange` / `coreChanges`, `asCaller`, and `bindHandle`.

`ActorMailboxHandlers` is deleted. The mailbox calls `CoreActorPool` (`startActor`, `isLive`, `finish`).

`PersistHandlers` is kept. FileAgent and DbAgent build persist closures and start the mailbox; there is no FileAgent/DbAgent persist object to pass. Issue 31 named this the persist parameter. Deleting it would invert those agents or duplicate the loop.

## 4. runLaunch vs runStartActor

Kept on start: mint secret, `credentials.add`, `PutLive` at `focusId` (already in `runStartActor`).
Deleted with launch: `getState`, name lookup, span extract/overlap, `PublicNumber` / `query`, `bindHandle` + `Async.Start` of the actor body (section 3).

## 5. Tests

Filter `FullyQualifiedName~CoreMsgActorCasesTests|FullyQualifiedName~CoreActorPoolTests|FullyQualifiedName~CredentialedChangePostsTests|FullyQualifiedName~CoreRuntimeTests`. **22 passed / 0 failed / 0 skipped.** Server `bin` was locked by a debug session; tests used `BaseOutputPath=artifacts/mailbox-test/`.
