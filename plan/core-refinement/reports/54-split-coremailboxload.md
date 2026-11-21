# Split CoreMailboxLoad

## Boundary

Load ends at `dispatchStartLoadSaveCommand` and `replyLoad`.

`CoreMailboxLoad` keeps

- the Load subject check (`loadSubjectError`, `isLoadSubject`)
- `dispatchLoad`
- `dispatchStartLoadSaveCommand`
- `replyLoad`

The `Order.Load` arm in `placeOrder` calls `CoreMailboxLoad.replyLoad`.

The `StartLoadSaveCommand` arm in `runMsg` calls `CoreMailboxLoad.dispatchStartLoadSaveCommand`.

## New file

[src/Server/Core/CoreMailboxLoop.fs](src/Server/Core/CoreMailboxLoop.fs) is module `CoreMailboxLoop`.

That half is the mailbox loop. The loop arms are

- Change (`replyEdit`, `placeOrder` for `ChangeOrigin.Edit`)
- Notice apply for Parse and Persisted (`applyNotice` and the persist writers)
- Ask (`runQuery`)
- seed, pump, and `start`

`makeMailBox` sets `parsePush` on `MailboxContext`. `replyEdit` takes the same parameters as before.

`importFocusId` and `pushImportFocus` are absent from the source. Both modules leave them absent.

## Callers

- `CoreMailbox.hostPublishing` calls `CoreMailboxLoop.makeMailBox` and `CoreMailboxLoop.start`
- `CoreLoopNoticeTests` and `PersistThreadTests` call `CoreMailboxLoop.applyNotice`
- the `SyncLogic.nodeAfterPersisted` comment names `CoreMailboxLoop.writePersisted`
- `src/Server/Gambol.Server.fsproj` lists `Core\CoreMailboxLoop.fs` after `CoreMailboxLoad.fs`

The notice tests stay in their current files. No test file is a mirror of `CoreMailboxLoad.fs`.

## Result

Branch `dev`.

`dotnet build src/Server/Gambol.Server.fsproj` succeeded with 0 warnings and 0 errors.

`scripts/agent-test.sh --filter FullyQualifiedName~MailboxExpandTests|FullyQualifiedName~MailboxAskTests|FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~Issue41CoreMailboxTests|FullyQualifiedName~CoreLoopNoticeTests|FullyQualifiedName~PersistThreadTests server` passed 70 tests. Failed 0.

The repo has no saved mailbox filter. The filter above is those mailbox modules plus the two test files that call `applyNotice`.
