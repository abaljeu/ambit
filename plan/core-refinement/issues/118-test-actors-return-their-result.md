# 118 — Test Actors return their result

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md)

## 3. Look at

### Owner

[ ] [CoreDoor.fs](src/Server/Core/CoreDoor.fs) owns `actorStop`, read only

### Source

[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `actorStop`, `postNotice`, read only

### Match sites and constructors

[ ] `CoreDoor.actorStop` is called in `QueryActorTests.fs`, `OrderedEventStreamTests.fs`, and `AgentAskTests.fs`, each from a test Actor
[ ] `CoreMailboxApi.actorStop` is called in two tests of `CoreMailboxDoorTests.fs`

### Tests this ticket names

[ ] [QueryActorTests.fs](tests/Server.Tests/QueryActorTests.fs) the `gate` Actor
[ ] [OrderedEventStreamTests.fs](tests/Server.Tests/OrderedEventStreamTests.fs) the `gate` Actor
[ ] [AgentAskTests.fs](tests/Server.Tests/AgentAskTests.fs) `probe`
[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `CoreMailboxApi.actorStop with valid credential drops live row`, `CoreMailboxApi.actorStop appends ActorStop and drops live row`

## 4. Context

### Today

[ ] After 117, an Actor that returns its result is stopped by the pool, and no source file calls `CoreDoor.actorStop`
[ ] The three test Actors still call `CoreDoor.actorStop` with `ActorSucceeded`, then return it
[ ] The two `CoreMailboxDoorTests` tests stop a run with `CoreMailboxApi.actorStop` and the Actor's caller
[ ] Those tests use a recording pool; `dispatchItem` finds the run for a `Notice.ActorStopped` with `trySecretForFocus`
[ ] Tests see internal `CoreMailboxApi.postNotice`

## 5. Changes

### Change

[ ] One migrate: tests stop calling `CoreDoor.actorStop` and `CoreMailboxApi.actorStop`. Source does not change

### Tests

[ ] The three test Actors return `ActorSucceeded` and do not call `CoreDoor.actorStop`
[ ] The two `CoreMailboxDoorTests` tests post `Notice.ActorStopped` of the focus with `CoreMailboxApi.postNotice`
[ ] Their recording pool's `trySecretForFocus` gives the live secret for that focus

### Size

[ ] 0 source files
[ ] 4 test files: `QueryActorTests.fs`, `OrderedEventStreamTests.fs`, `AgentAskTests.fs`, `CoreMailboxDoorTests.fs`

## 6. Tests

### Callers

[ ] Rename `CoreMailboxApi.actorStop with valid credential drops live row` to `ActorStopped Notice drops live row`
[ ] Rename `CoreMailboxApi.actorStop appends ActorStop and drops live row` to `ActorStopped Notice is logged and drops live row`
[ ] Filter: `drops live row`
[ ] Filter: `QueryActorTests`
[ ] Filter: `OrderedEventStreamTests`
[ ] Filter: `AgentAskTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
