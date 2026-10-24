# 113 — Actors post Notice.ActorStopped

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [112 — Place Cancel through PlaceOrder](112-place-cancel-through-placeorder.md)

## 3. Look at

### Owner

[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) owns `actorStop` and `postNotice`

### Source

[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `dispatchItem`
[ ] [CoreDoor.fs](src/Server/Core/CoreDoor.fs) `actorStop`
[ ] [GithubTransportActor.fs](src/Server/GithubTransportActor.fs), [RunAgentActor.fs](src/Server/RunAgentActor.fs), [SearchActor.fs](src/Server/SearchActor.fs), [TestActor.fs](src/Server/TestActor.fs): each call to `CoreDoor.actorStop`
[ ] [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) `trySecretForFocus`, `finish`

### Match sites and constructors

[ ] `CoreDoor.actorStop` is called from the four Actors and from tests
[ ] The tests that call it: `OrderedEventStreamTests.fs`, `QueryActorTests.fs`, `AgentAskTests.fs`

### Tests this ticket names

[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `CoreMailboxApi.actorStop with valid credential drops live row`, `CoreMailboxApi.actorStop appends ActorStop and drops live row`
[ ] [OrderedEventStreamTests.fs](tests/Server.Tests/OrderedEventStreamTests.fs), [QueryActorTests.fs](tests/Server.Tests/QueryActorTests.fs), [AgentAskTests.fs](tests/Server.Tests/AgentAskTests.fs): each call to `CoreDoor.actorStop`

## 4. Context

### Today

[ ] `CoreMailboxApi.actorStop` posts `CoreMsg.ActorStop` with the caller and the result
[ ] `dispatchActorStop` finds the focus from the caller's secret, appends the stop, and finishes the pool entry
[ ] `dispatchItem` logs a Notice, then calls `applyNotice`; a Notice gets no reply
[ ] A Notice carries no caller, so the mailbox cannot find the secret from it

### Decisions

[ ] When the Actor is done it posts `Notice.ActorStopped` of its focus and its result
[ ] A focus with no running Actor is reported, and the log gains no entry

## 5. Changes

### Change

[ ] One change: Actors post `Notice.ActorStopped` through `postNotice`
[ ] `CoreMailboxApi.actorStop` and `CoreMsg.ActorStop` stay until 114

### Server

[ ] Add `CoreMailboxApi.actorStopped host focusId result`, which posts `Notice.ActorStopped(focusId, result)` with `postNotice` and returns `Ok ()`
[ ] `CoreDoor.actorStop door focusId result` calls `actorStopped`
[ ] For `Notice.ActorStopped`, `dispatchItem` finds the run with `pool.trySecretForFocus`
[ ] With a run, `dispatchItem` logs the Notice, then calls `pool.finish` with the secret and the result
[ ] With no run, `dispatchItem` reports "no running actor" and logs nothing

### Actors

[ ] Each of the four Actors passes the focus it started on to `CoreDoor.actorStop`

### Size

[ ] 7 source files: `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`, `CoreDoor.fs`, `GithubTransportActor.fs`, `RunAgentActor.fs`, `SearchActor.fs`, `TestActor.fs`
[ ] 4 test files: `CoreMailboxDoorTests.fs`, `OrderedEventStreamTests.fs`, `QueryActorTests.fs`, `AgentAskTests.fs`

## 6. Tests

### Done

[ ] Add `actorStop posts Notice.ActorStopped` in `CoreMailboxDoorTests.fs`
[ ] The log holds `Notice.ActorStopped` of the focus and the result, and the pool entry is finished
[ ] Filter: `actorStop posts Notice.ActorStopped`
[ ] Add `ActorStopped for an idle focus logs nothing`
[ ] Filter: `ActorStopped for an idle focus logs nothing`

### Callers

[ ] Change `CoreMailboxApi.actorStop with valid credential drops live row` and `CoreMailboxApi.actorStop appends ActorStop and drops live row` to call `actorStopped` with the focus
[ ] Filter: `drops live row`
[ ] Change each `CoreDoor.actorStop` call in `OrderedEventStreamTests.fs`, `QueryActorTests.fs`, and `AgentAskTests.fs` to pass the focus
[ ] Filter: `OrderedEventStreamTests`
[ ] Filter: `QueryActorTests`
[ ] Filter: `AgentAskTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
