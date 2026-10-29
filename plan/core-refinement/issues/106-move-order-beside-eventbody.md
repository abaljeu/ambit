# 106 — Move Order beside EventBody, in Ev.fs

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] None.

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `Op`, `Authority`, `ActorResult`, `ActorStart`, `Notice`, `EventBody`, and `Ev`

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

[ ] `History.fs` defines `Op`, `Authority`, `ActorResult`, `ActorStart`, `Notice`, `EventBody`, and `Ev`, then `State`, `ApplyResult`, and the `Op`, `Ev`, and `ChangeValidation` modules
[ ] `History.fs` is overloaded
[ ] `Order` is in `MailboxLog.fs`, after `History.fs`
[ ] `Order.LoadSave` holds `LoadSavePath` and `LoadSaveCommandRequest`
[ ] `LoadSavePath` is in `PathPick.fs`, after `History.fs`
[ ] `LoadSaveCommandRequest` is in `LoadSaveCommand.fs`, after `History.fs`
[ ] `LoadSaveCommandRequest` holds `LoadSaveOperation`, `LoadSavePrePick`, and `ActorStart`
[ ] All these types are in namespace `Gambol.Shared`

### Why

[ ] [107 — Add EventBody.Order](107-add-eventbody-order.md) adds `EventBody.Order of Order`
[ ] `Order` must compile before `EventBody`
[ ] Alan, 2026-10-29: the types go in a new file, `Ev.fs`, not in `History.fs`. `Ev.fs` holds all the types up to and including `Ev`.

## 5. Changes

### Change

[ ] One refactor: move type definitions into a new `Ev.fs`, with no other edit

### Ev.fs

[ ] New file `src/Shared/Ev.fs`, namespace `Gambol.Shared`
[ ] It holds, in this order, with their attributes and doc comments: `Op`, `Authority`, `ActorResult`, `ActorStart`, `LoadSavePath`, `LoadSavePrePick`, `LoadSaveOperation`, `LoadSaveCommandRequest`, `Notice`, `PeerActorName`, `ChangeOrigin`, `Order`, `EventBody`, `Ev`

### Gambol.Shared.fsproj

[ ] Compile `Ev.fs` immediately before `History.fs`

### Old homes

[ ] Remove `Op`, `Authority`, `ActorResult`, `ActorStart`, `Notice`, `EventBody`, and `Ev` from `History.fs`; `State`, `ApplyResult`, and the modules stay
[ ] Remove the three LoadSave unions from `PathPick.fs`
[ ] Remove `LoadSaveCommandRequest` from `LoadSaveCommand.fs`
[ ] Remove `PeerActorName`, `ChangeOrigin`, and `Order` from `MailboxLog.fs`
[ ] The modules and other types in `History.fs`, `PathPick.fs`, `LoadSaveCommand.fs`, and `MailboxLog.fs` stay
[ ] No caller changes, because the namespace stays `Gambol.Shared`

### Size

[ ] 6 source files: `Ev.fs` (new), `Gambol.Shared.fsproj`, `History.fs`, `PathPick.fs`, `LoadSaveCommand.fs`, `MailboxLog.fs`
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
