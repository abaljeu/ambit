# 34b inject actors and mailbox History

Place: `dev`. Two architectural corrections from [[34b-corrections.md]]. Credentials stay out of this cut.

## 1. Actors passed in

1. CoreRuntime.create takes `(ActorName * ActorFn) list`, registers those entries, then starts the mailbox.
2. TestActor lives in [[tests/Server.Tests/TestActor.fs]]. It is not under src/Server/Core/.
3. RouteRegistration and CoreRuntime tests pass `[]`. Hello tests register TestActor on CoreActorPool.

## 2. Mailbox owns History

1. CoreActorPool.startActor prepares, creates the live row and secret, and returns ActorStart. It does not take History and does not schedule.
2. dispatchStartActor writes ActorStarted on Loop.mailboxHistory, then pool.schedule starts the body.
3. dispatchActorStop writes ActorFinished on mailboxHistory, then pool.finish.
4. History.appendActorStarted and History.appendActorFinished are deleted. getState stays Graph-only.

## 3. Tests

1. Server.Tests filter TestActorHelloTests, CoreMailboxDoorTests, CoreMsgActorCasesTests, CoreRuntimeTests, CoreActorPoolTests, CredentialedChangePostsTests: 46 passed.

