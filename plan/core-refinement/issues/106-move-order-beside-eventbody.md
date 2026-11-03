# 106 — Move Order beside EventBody

**Status:** `done`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] None.

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `Op`, `Authority`, `ActorResult`, `ActorStart`, `LoadSavePath`, `LoadSavePrePick`, `LoadSaveOperation`, `LoadSaveCommandRequest`, `Notice`, `PeerActorName`, `ChangeOrigin`, `Order`, `EventBody`, and `Ev`
[ ] [ChangeValidation.fs](src/Shared/ChangeValidation.fs) owns the `ChangeValidation` module

### Source

[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) keeps `PlacedOrder` and the `MailboxLog` module
[ ] [PathPick.fs](src/Shared/PathPick.fs) keeps `LoadSubject`, `LoadSubjectRejection`, `WorkspaceMappingPresence`, `LoadPathChoice`, and the `LoadSubject` and `PathPick` modules
[ ] [LoadSaveCommand.fs](src/Shared/LoadSaveCommand.fs) keeps `LoadSaveCommandResponse` and the `LoadPathChoice` module
[ ] [Gambol.Shared.fsproj](src/Shared/Gambol.Shared.fsproj) compiles `ChangeValidation.fs` immediately after `History.fs`, and `History.fs` before `PathPick.fs`, `LoadSaveCommand.fs`, and `MailboxLog.fs`

### Tests this ticket names

[ ] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs) `StartPeerActor carries Shared PeerActorName`
[ ] [PathPickTests.fs](tests/Shared.Tests/PathPickTests.fs), every test

## 4. Context

### Compile order

[ ] `History.fs` defines `Op`, `Authority`, `ActorResult`, `ActorStart`, `LoadSavePath`, `LoadSavePrePick`, `LoadSaveOperation`, `LoadSaveCommandRequest`, `Notice`, `PeerActorName`, `ChangeOrigin`, `Order`, `EventBody`, and `Ev`, then `State`, `ApplyResult`, and the `Op` and `Ev` modules
[ ] `ChangeValidation.fs` defines the `ChangeValidation` module
[ ] `Order` sits after `Notice` and before `EventBody`
[ ] `Order.LoadSave` holds `LoadSavePath`, `PeerActorName`, and `LoadSaveCommandRequest`
[ ] `LoadSavePath`, `LoadSavePrePick`, and `LoadSaveOperation` sit after `ActorStart`
[ ] `LoadSaveCommandRequest` sits after those three unions
[ ] `LoadSaveCommandRequest` holds `LoadSaveOperation`, `LoadSavePrePick`, `holdStack`, and `ActorStart`
[ ] All these types are in namespace `Gambol.Shared`

### Why

[ ] [107 — Add EventBody.Order](107-add-eventbody-order.md) adds `EventBody.Order of Order`
[ ] `Order` must compile before `EventBody`
[ ] `History.fs` holds the types through `Ev`. The `ChangeValidation` module is in `ChangeValidation.fs`.

## 5. Changes

### Change

[ ] The type definitions and the `ChangeValidation` module sit in the files below, with no other edit

### History.fs

[ ] `LoadSavePath`, `LoadSavePrePick`, and `LoadSaveOperation` sit after `ActorStart`, with their attributes
[ ] `LoadSaveCommandRequest` sits after them
[ ] `PeerActorName`, `ChangeOrigin`, and `Order` sit after `Notice` and before `EventBody`, with their attributes
[ ] `History.fs` holds `State`, `ApplyResult`, and the `Op` and `Ev` modules

### ChangeValidation.fs

[ ] File `src/Shared/ChangeValidation.fs`, namespace `Gambol.Shared`
[ ] It holds the `ChangeValidation` module, with its attributes and doc comments

### Gambol.Shared.fsproj

[ ] `ChangeValidation.fs` compiles immediately after `History.fs`

### Other files

[ ] `PathPick.fs` holds `LoadSubject`, `LoadSubjectRejection`, `WorkspaceMappingPresence`, `LoadPathChoice`, and the `LoadSubject` and `PathPick` modules
[ ] `LoadSaveCommand.fs` holds `LoadSaveCommandResponse` and the `LoadPathChoice` module
[ ] `MailboxLog.fs` holds `PlacedOrder` and the `MailboxLog` module
[ ] Callers use namespace `Gambol.Shared`, and `ChangeValidation` keeps its name

### Size

[ ] 6 source files: `History.fs`, `ChangeValidation.fs` (new), `Gambol.Shared.fsproj`, `PathPick.fs`, `LoadSaveCommand.fs`, `MailboxLog.fs`
[ ] 0 test files

## 6. Tests

### Kept

[ ] Keep `StartPeerActor carries Shared PeerActorName`
[ ] Keep every test in `PathPickTests.fs`
[ ] Keep `LoadSaveCommandTests`
[ ] Filter: `StartPeerActor carries Shared PeerActorName`
[ ] Filter: `PathPickTests`
[ ] Filter: `LoadSaveCommandTests`
[ ] Keep `HistoryTests`, which use `ChangeValidation`
[ ] Filter: `HistoryTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
