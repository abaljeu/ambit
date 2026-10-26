# 117 — The pool posts Notice.ActorStopped when an Actor ends

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [113 — Actors return their ActorResult](113-actors-return-their-actorresult.md)
[ ] [81 — Log Actor and search stops as Notices](81-log-actor-and-search-stops-as-notices.md)

## 3. Look at

### Owner

[ ] [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) owns `runSchedule`, `trySecretForFocus`, and `finish`

### Source

[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `postNotice`, read only
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `dispatchItem`, `applyNotice`
[ ] [GithubTransportActor.fs](src/Server/GithubTransportActor.fs), [RunAgentActor.fs](src/Server/RunAgentActor.fs), [SearchActor.fs](src/Server/SearchActor.fs), [TestActor.fs](src/Server/TestActor.fs): each call to `CoreDoor.actorStop`

### Docs

[ ] [cqrs-messages.md](../cqrs-messages.md) §2 item 8 **Notice**, read only; it already says the pool posts `Notice.ActorStopped` when an Actor ends

### Match sites and constructors

[ ] `runSchedule` is the only place that starts Actor work
[ ] `CoreDoor.actorStop` is called from the four Actors and from tests; the test calls stay until [118 — Test Actors return their result](118-test-actors-return-their-result.md)
[ ] `dispatchItem` has one `CoreQueue.Notice` arm for every Notice

### Tests this ticket names

[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs), the two tests added here
[ ] [CancelByFocusTests.fs](tests/Server.Tests/CancelByFocusTests.fs) `actorStop after cancel does not emit a root Focus ActorStop`

## 4. Context

### Today

[ ] After 113, `runSchedule` starts `work |> Async.Ignore` and does not read the result
[ ] Each Actor calls `CoreDoor.actorStop`, which posts `CoreMsg.ActorStop` with the Actor's caller; `dispatchActorStop` checks the secret, appends the stop, and calls `pool.finish`
[ ] After 81, the stop is logged as `Notice.ActorStopped` of the focus and the result
[ ] `pool.finish` cancels the row's token; work started with that token does not reach its end
[ ] A Cancel order runs `runCancel`, which appends `Notice.ActorStopped(focusId, ActorCancelled)` and calls `pool.finish`
[ ] `dispatchItem` logs a Notice, then calls `applyNotice`; a Notice gets no reply and has no caller
[ ] `CoreActorPool.fs` compiles after `CoreMailboxApi.fs`; the `CoreChangesDoor` that `runSchedule` gets holds the `host`

### Decisions

[ ] Alan, 2026-10-24: an Actor does not send a stop; it just stops
[ ] The pool sees the Actor end and posts `Notice.ActorStopped` on the notice line
[ ] The Notice tells the client it can drop its hold on the Actor; it does not tell the pool
[ ] The stop has no caller, so no secret check is needed for it
[ ] Exactly one stop is logged per run: the first of the Actor's end and a Cancel
[ ] Alan, 2026-10-24: a search just stops too; [111 — Place search start as an order, and post SearchStopped](111-place-search-start-as-an-order-and-post-searchstopped.md) posts `Notice.SearchStopped` when a search ends

### Known limit

[ ] Alan, 2026-10-24: accepted for now. `dispatchItem` finds the run by focus. If a new run starts on the same focus after an Actor ends and before its `Notice.ActorStopped` is handled, that Notice finishes the new run

## 5. Changes

### Change

[ ] One change: the pool posts `Notice.ActorStopped` when an Actor's work returns, and the four Actors stop calling `CoreDoor.actorStop`

### CoreActorPool.fs

[ ] `runSchedule` runs the work, then posts `Notice.ActorStopped(row.focusId, result)` with `CoreMailboxApi.postNotice coreChanges.host`
[ ] Work that is cancelled posts nothing

### CoreMailboxLoad.fs

[ ] For `Notice.ActorStopped`, `dispatchItem` finds the run with `pool.trySecretForFocus`
[ ] With a run, `dispatchItem` logs the Notice, then calls `pool.finish` with that secret and the result
[ ] With no run, `dispatchItem` logs nothing; a Cancel already logged the stop
[ ] Every other Notice is logged and applied as today

### Actors

[ ] Each of the four Actors returns its result and does not call `CoreDoor.actorStop`
[ ] `RunAgentActor.stop` leaves

### Size

[ ] 6 source files: `CoreActorPool.fs`, `CoreMailboxLoad.fs`, `GithubTransportActor.fs`, `RunAgentActor.fs`, `SearchActor.fs`, `TestActor.fs`
[ ] 1 test file: `CoreMailboxDoorTests.fs`

## 6. Tests

### Done

[ ] Add `an Actor that returns logs Notice.ActorStopped` in `CoreMailboxDoorTests.fs`
[ ] A `root` Actor returns `ActorSucceeded` without calling `CoreDoor.actorStop`
[ ] The log holds the start, then `Notice.ActorStopped` of the focus and `ActorSucceeded`, and the focus is not in `pool.liveFocusIds`
[ ] Filter: `an Actor that returns logs Notice.ActorStopped`
[ ] Add `ActorStopped for an idle focus logs nothing`
[ ] The test posts `Notice.ActorStopped` with `CoreMailboxApi.postNotice` for a focus with no run; the log gains no entry
[ ] Filter: `ActorStopped for an idle focus logs nothing`

### Cancel

[ ] Keep `actorStop after cancel does not emit a root Focus ActorStop`; the log holds one stop
[ ] Filter: `actorStop after cancel does not emit a root Focus ActorStop`

### Kept

[ ] Keep every test that starts one of the four Actors
[ ] Filter: `TestActorHelloTests`
[ ] Filter: `QueryActorTests`
[ ] Filter: `LoadSaveCommandTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
