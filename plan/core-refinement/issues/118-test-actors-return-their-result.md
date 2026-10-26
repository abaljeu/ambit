# 118 — Test Actors return their result

One job: A test Actor ends by returning ActorSucceeded, and the two mailbox door tests see the live row drop when Notice.ActorStopped is posted. This comes from item 8 Notice in [2. Target shape](plan/core-refinement/cqrs-messages.md): an Actor sends no stop, and the pool posts Notice.ActorStopped.

Type: `coding`

Blockers: [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md)

**Status:** `defined`

**Trackers:** architecture

## Inputs

### Working

[ ] [[tests/Server.Tests/QueryActorTests.fs]] fact `query start error stores ActorStart and ActorFailed and keeps the edit`: `gate` calls `CoreDoor.actorStop` with `ActorSucceeded`, then returns unit
[ ] [[tests/Server.Tests/OrderedEventStreamTests.fs]] fact `ActorStart body does not run inside the mailbox turn`: `gate` calls `CoreDoor.actorStop` with `ActorSucceeded`, then returns unit
[ ] [[tests/Server.Tests/AgentAskTests.fs]] fact `Ask extract carries Graph.focus from launch`: `probe` calls `CoreDoor.actorStop` with `ActorSucceeded`, then returns unit
[ ] [113 — Actors return their ActorResult](113-actors-return-their-actorresult.md) changes those three bodies to return `ActorResult` and leaves the `actorStop` call
[ ] [[tests/Server.Tests/CoreMailboxDoorTests.fs]] fact `CoreMailboxApi.actorStop with valid credential drops live row`: `trySecretForFocus` returns None, and `getFocusId` returns `Graph.rootId`
[ ] The same file, fact `CoreMailboxApi.actorStop appends ActorStop and drops live row`: `trySecretForFocus` returns None, `getFocusId` returns `sampleRequest.focusId`, and the log match is `EventBody.ActorStop`
[ ] [[src/Server/Core/CoreDoor.fs]] `actorStop` calls `CoreMailboxApi.actorStop` in [[src/Server/Core/CoreMailboxApi.fs]]. This ticket leaves both functions in place
[ ] `CoreMailboxApi.postNotice` is internal, posts `CoreQueue.Notice`, and Server.Tests already calls it
[ ] [[src/Shared/History.fs]] `Notice` has no `ActorStopped` case. [81 — Log Actor and search stops as Notices](81-log-actor-and-search-stops-as-notices.md) adds `Notice.ActorStopped` of the focus and the `ActorResult`
[ ] The blocker handles `Notice.ActorStopped` through `pool.trySecretForFocus`. A None result does not finish the row

### Reference

[ ] `plan/core-refinement/cqrs-messages.md` section 2 Target shape, item 8 Notice

## Do not load

[ ] [[src/Server/GithubTransportActor.fs]]. The blocker removes its `CoreDoor.actorStop` call
[ ] [[src/Server/RunAgentActor.fs]]. The blocker removes its `CoreDoor.actorStop` call
[ ] [[src/Server/SearchActor.fs]]. The blocker removes its `CoreDoor.actorStop` call
[ ] [[src/Server/TestActor.fs]]. The blocker removes its `CoreDoor.actorStop` call
[ ] [[src/Server/Core/CoreActorPool.fs]]. The blocker posts the Notice from the pool
[ ] [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md) removes `CoreDoor.actorStop` and `CoreMailboxApi.actorStop`
[ ] [[tests/Server.Tests/CancelByFocusTests.fs]] calls `actorStop`
[ ] [[tests/Server.Tests/CoreMsgActorCasesTests.fs]] calls `actorStop`
[ ] [[tests/Server.Tests/MailboxHistoryDurabilityTests.fs]] calls `actorStop`
[ ] [[tests/Server.Tests/Issue41CoreMailboxTests.fs]] calls `actorStop`

## Process

One migrate.

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] In `QueryActorTests.fs`, `gate` returns `ActorSucceeded` and does not call `CoreDoor.actorStop`.
3. [ ] In `OrderedEventStreamTests.fs`, `gate` returns `ActorSucceeded` and does not call `CoreDoor.actorStop`.
4. [ ] In `AgentAskTests.fs`, `probe` returns `ActorSucceeded` and does not call `CoreDoor.actorStop`.
5. [ ] In `CoreMailboxDoorTests.fs`, the first test posts `Notice.ActorStopped` of `Graph.rootId` and `ActorSucceeded` with `CoreMailboxApi.postNotice`. Its `trySecretForFocus` returns the live secret for `Graph.rootId`.
6. [ ] In `CoreMailboxDoorTests.fs`, the second test posts `Notice.ActorStopped` of `sampleRequest.focusId` and `ActorSucceeded` with `CoreMailboxApi.postNotice`. Its `trySecretForFocus` returns the live secret for that focus. Its log match is that Notice.

## Outputs

[ ] No source file changes
[ ] `QueryActorTests.fs`, `OrderedEventStreamTests.fs`, `AgentAskTests.fs`, and `CoreMailboxDoorTests.fs` change
[ ] Rename `CoreMailboxApi.actorStop with valid credential drops live row` to `ActorStopped Notice drops live row`
[ ] Rename `CoreMailboxApi.actorStop appends ActorStop and drops live row` to `ActorStopped Notice is logged and drops live row`
[ ] `--filter` `drops live row`
[ ] `--filter` `QueryActorTests`
[ ] `--filter` `OrderedEventStreamTests`
[ ] `--filter` `AgentAskTests`
[ ] Check report: `plan/core-refinement/reports/118-test-actors-return-their-result-check.md`

## Stop when

[ ] The edit needs a file outside Inputs
[ ] The edit contradicts item 8 Notice
[ ] Record the stop at `plan/core-refinement/reports/118-test-actors-return-their-result-stop.md`, then ask.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/118-test-actors-return-their-result-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
