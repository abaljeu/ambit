# 114 — Remove CoreMsg.ActorStop

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [113 — Actors post Notice.ActorStopped](113-actors-post-notice-actorstopped.md)

## 3. Look at

### Owner

[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) owns `CoreMsg.ActorStop`

### Source

[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `actorStop`, `actorStopped`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `runMsg`, `messageCaller`
[ ] [CoreMailboxEvents.fs](src/Server/Core/CoreMailboxEvents.fs) `operationContext`, `replyFailure`
[ ] [CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) `dispatchActorStop`

### Match sites and constructors

[ ] `CoreMsg.ActorStop` is built only in `CoreMailboxApi.actorStop`
[ ] It is matched in `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`
[ ] `CoreMailboxApi.actorStop` is called from 4 test files and no source file

### Tests this ticket names

[ ] [CoreMsgActorCasesTests.fs](tests/Server.Tests/CoreMsgActorCasesTests.fs) `postActorStop`, every `ActorStop` test
[ ] [Issue41CoreMailboxTests.fs](tests/Server.Tests/Issue41CoreMailboxTests.fs) `mailbox appends ActorStart and ActorStop in lifecycle order`
[ ] [MailboxHistoryDurabilityTests.fs](tests/Server.Tests/MailboxHistoryDurabilityTests.fs) `runMixedHello`
[ ] [CancelByFocusTests.fs](tests/Server.Tests/CancelByFocusTests.fs) `actorStop after cancel does not emit a root Focus ActorStop`

## 4. Context

### Today

[ ] After 113, no Actor calls `CoreMailboxApi.actorStop`
[ ] `ActorStop with Test credentials is refused`, `ActorStop with Browser credentials is refused`, and `ActorStop with Parse credentials is refused` check the caller's authority
[ ] A Notice carries no caller, so `actorStopped` has no authority check
[ ] `actorStopped` returns before the mailbox applies the Notice

## 5. Changes

### Change

[ ] One contract: `CoreMailboxApi.actorStop` and `CoreMsg.ActorStop` leave

### Server

[ ] Remove `CoreMailboxApi.actorStop`
[ ] Remove `ActorStop` from `CoreMsg` and its arms from `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`
[ ] Remove `CoreMailboxActors.dispatchActorStop`

### Size

[ ] 5 source files: `CoreMsg.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`, `CoreMailboxEvents.fs`, `CoreMailboxActors.fs`
[ ] 4 test files: `CoreMsgActorCasesTests.fs`, `Issue41CoreMailboxTests.fs`, `MailboxHistoryDurabilityTests.fs`, `CancelByFocusTests.fs`

## 6. Tests

### Callers

[ ] `postActorStop` in `CoreMsgActorCasesTests.fs` calls `actorStopped` with the focus, then reads the log so the Notice has been applied
[ ] `mailbox appends ActorStart and ActorStop in lifecycle order` calls `actorStopped`
[ ] `runMixedHello` calls `actorStopped`
[ ] `actorStop after cancel does not emit a root Focus ActorStop` calls `actorStopped`
[ ] Filter: `CoreMsgActorCasesTests`
[ ] Filter: `mailbox appends ActorStart and ActorStop in lifecycle order`
[ ] Filter: `MailboxHistoryDurabilityTests`
[ ] Filter: `actorStop after cancel does not emit a root Focus ActorStop`

### Removed

[ ] Remove `ActorStop with Test credentials is refused`
[ ] Remove `ActorStop with Browser credentials is refused`
[ ] Remove `ActorStop with Parse credentials is refused`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
