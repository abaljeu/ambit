# 114 — Remove CoreMsg.ActorStop

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [118 — Test Actors return their result](118-test-actors-return-their-result.md)

## 3. Look at

### Owner

[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) owns `CoreMsg.ActorStop`

### Source

[ ] [CoreDoor.fs](src/Server/Core/CoreDoor.fs) `actorStop`
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `actorStop`, `postNotice`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `runMsg`, `messageCaller`
[ ] [CoreMailboxEvents.fs](src/Server/Core/CoreMailboxEvents.fs) `operationContext`, `replyFailure`
[ ] [CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) `dispatchActorStop`

### Match sites and constructors

[ ] `CoreMsg.ActorStop` is built only in `CoreMailboxApi.actorStop`
[ ] It is matched in `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`
[ ] `CoreDoor.actorStop` has no caller after 118
[ ] `CoreMailboxApi.actorStop` is called from `CoreDoor.actorStop` and from 4 test files

### Tests this ticket names

[ ] [CoreMsgActorCasesTests.fs](tests/Server.Tests/CoreMsgActorCasesTests.fs) `postActorStop`, every `ActorStop` test
[ ] [Issue41CoreMailboxTests.fs](tests/Server.Tests/Issue41CoreMailboxTests.fs) `mailbox appends ActorStart and ActorStop in lifecycle order`
[ ] [MailboxHistoryDurabilityTests.fs](tests/Server.Tests/MailboxHistoryDurabilityTests.fs) `runMixedHello`
[ ] [CancelByFocusTests.fs](tests/Server.Tests/CancelByFocusTests.fs) `actorStop after cancel does not emit a root Focus ActorStop`

## 4. Context

### Today

[ ] After 117 and 118, no Actor and no other test sends a stop; the pool posts `Notice.ActorStopped` when an Actor ends
[ ] `ActorStop with Test credentials is refused`, `ActorStop with Browser credentials is refused`, and `ActorStop with Parse credentials is refused` check the caller's authority on a sent stop
[ ] A Notice has no caller; with no sent stop there is no credential to check
[ ] `postNotice` returns before the mailbox applies the Notice

### Decisions

[ ] Alan, 2026-10-24: an Actor does not send a stop; it just stops

## 5. Changes

### Change

[ ] One contract: the sent stop leaves. `CoreDoor.actorStop`, `CoreMailboxApi.actorStop`, and `CoreMsg.ActorStop` leave

### Server

[ ] Remove `CoreDoor.actorStop`
[ ] Remove `CoreMailboxApi.actorStop`
[ ] Remove `ActorStop` from `CoreMsg` and its arms from `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`
[ ] Remove `CoreMailboxActors.dispatchActorStop`

### Size

[ ] 6 source files: `CoreMsg.fs`, `CoreDoor.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`, `CoreMailboxEvents.fs`, `CoreMailboxActors.fs`
[ ] 4 test files: `CoreMsgActorCasesTests.fs`, `Issue41CoreMailboxTests.fs`, `MailboxHistoryDurabilityTests.fs`, `CancelByFocusTests.fs`

## 6. Tests

### Callers

[ ] `postActorStop` in `CoreMsgActorCasesTests.fs` posts `Notice.ActorStopped` of the focus with `CoreMailboxApi.postNotice`, then reads the log so the Notice has been applied
[ ] `mailbox appends ActorStart and ActorStop in lifecycle order` posts `Notice.ActorStopped` the same way
[ ] `runMixedHello` posts `Notice.ActorStopped` of the start's focus with `CoreMailboxApi.postNotice`, then reads the log
[ ] `actorStop after cancel does not emit a root Focus ActorStop` posts `Notice.ActorStopped` after the Cancel; the log still holds one stop
[ ] Filter: `CoreMsgActorCasesTests`
[ ] Filter: `mailbox appends ActorStart and ActorStop in lifecycle order`
[ ] Filter: `MailboxHistoryDurabilityTests`
[ ] Filter: `actorStop after cancel does not emit a root Focus ActorStop`

### Removed

[ ] Remove `ActorStop with Test credentials is refused`
[ ] Remove `ActorStop with Browser credentials is refused`
[ ] Remove `ActorStop with Parse credentials is refused`
[ ] No stop is sent, so there is no caller to refuse

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
