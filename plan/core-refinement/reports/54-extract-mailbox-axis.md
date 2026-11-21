# 54 — Extract mailbox axis

Branch: `dev`.

## 1. Module

[src/Server/Core/CoreMailboxAxis.fs](../../../src/Server/Core/CoreMailboxAxis.fs) is `internal` `[<RequireQualifiedAccess>]` module `CoreMailboxAxis`. [src/Server/Gambol.Server.fsproj](../../../src/Server/Gambol.Server.fsproj) lists it before `CoreMailboxLoop.fs`.

## 2. Moved

Writers moved from [src/Server/Core/CoreMailboxLoop.fs](../../../src/Server/Core/CoreMailboxLoop.fs), with their doc comments:

- `writeParseState`, `markUnparsedThenPush`, `withPullingFile`
- `writeUnpersisted`, `writePullingFileUnpersisted`, `writeReceived`
- `stampUpdateTime`, `parsedAndUnpersisted`, `pushParsedUnpersisted`
- `applyNodeParsed`, `withPersistedVersion`, `writePersisted`

`applyNotice`, Change, and the pump remain in `CoreMailboxLoop`.

## 3. Callers

- `CoreMailboxLoop.applyNotice` calls the writers on `CoreMailboxAxis`
- `pushReporting` and `makeMailBox` call `CoreMailboxAxis.markUnparsedThenPush`
- `CoreMailboxLoad.dispatchLoad` calls `context.parsePush`, which `makeMailBox` binds to that function
- [src/Shared/SyncLogic.fs](../../../src/Shared/SyncLogic.fs) names `CoreMailboxAxis.writePersisted`

## 4. Verify

`dotnet build src/Server/Gambol.Server.fsproj` succeeded with 0 warnings and 0 errors.

Mailbox, notice, and persist-thread tests passed: Failed 0, Passed 70, Skipped 0.
