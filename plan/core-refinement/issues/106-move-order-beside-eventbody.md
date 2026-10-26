# 106 — Move Order beside EventBody

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] None.

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `Notice`, `EventBody`, and `Ev`

### Source

[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) `PeerActorName`, `ChangeOrigin`, `Order`
[ ] [PathPick.fs](src/Shared/PathPick.fs) `LoadSavePath`, `LoadSavePrePick`, `LoadSaveOperation`
[ ] [LoadSaveCommand.fs](src/Shared/LoadSaveCommand.fs) `LoadSaveCommandRequest`
[ ] [Gambol.Shared.fsproj](src/Shared/Gambol.Shared.fsproj) compiles `History.fs` before `PathPick.fs`, `LoadSaveCommand.fs`, and `MailboxLog.fs`

### Tests this ticket names

[ ] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs) `StartPeerActor carries Shared PeerActorName`
[ ] [PathPickTests.fs](tests/Shared.Tests/PathPickTests.fs), every test

## 4. Context

### Compile order

[ ] `History.fs` defines `Op`, `ActorStart`, `Notice`, `EventBody`, and `Ev`
[ ] `Order` is in `MailboxLog.fs`, after `History.fs`
[ ] `Order.LoadSave` holds `LoadSavePath` and `LoadSaveCommandRequest`
[ ] `LoadSavePath` is in `PathPick.fs`, after `History.fs`
[ ] `LoadSaveCommandRequest` is in `LoadSaveCommand.fs`, after `History.fs`
[ ] `LoadSaveCommandRequest` holds `LoadSaveOperation`, `LoadSavePrePick`, and `ActorStart`
[ ] All these types are in namespace `Gambol.Shared`

### Why

[ ] [107 — Add EventBody.Order](107-add-eventbody-order.md) adds `EventBody.Order of Order`
[ ] `Order` must compile before `EventBody`

## 5. Changes

### Change

[ ] One refactor: move type definitions, with no other edit

### History.fs

[ ] Add `LoadSavePath`, `LoadSavePrePick`, and `LoadSaveOperation` after `ActorStart`, with their attributes
[ ] Add `LoadSaveCommandRequest` after them
[ ] Add `PeerActorName`, `ChangeOrigin`, and `Order` after `Notice` and before `EventBody`, with their attributes

### Old homes

[ ] Remove those three unions from `PathPick.fs`
[ ] Remove `LoadSaveCommandRequest` from `LoadSaveCommand.fs`
[ ] Remove `PeerActorName`, `ChangeOrigin`, and `Order` from `MailboxLog.fs`
[ ] The modules in `PathPick.fs`, `LoadSaveCommand.fs`, and `MailboxLog.fs` stay
[ ] No caller changes, because the namespace stays `Gambol.Shared`

### Size

[ ] 4 source files: `History.fs`, `PathPick.fs`, `LoadSaveCommand.fs`, `MailboxLog.fs`
[ ] 0 test files

## 6. Tests

### Kept

[ ] Keep `StartPeerActor carries Shared PeerActorName`
[ ] Keep every test in `PathPickTests.fs`
[ ] Keep `LoadSaveCommandTests`
[ ] Filter: `StartPeerActor carries Shared PeerActorName`
[ ] Filter: `PathPickTests`
[ ] Filter: `LoadSaveCommandTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
