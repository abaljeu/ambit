# 121 — MarkUnparsed leaves Notice

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [56 — Remove ParseThread markUnparsed](56-remove-parsethread-markunparsed.md)

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `Notice`

### Source

[ ] [EventJson.fs](src/Shared/EventJson.fs) `encodeNotice`, `noticeFrom`
[ ] [Database.fs](src/Server/Database.fs) `writeNoticeRow`, `writeDocumentState`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `applyNotice`, `writeParseState`, `markUnparsedThenPush`, `pushReporting`, `makeMailBox`

### Helper

[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `writeAxis`
[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) `PersistHandlers.replaceGraph`

### Match sites

[ ] `Notice.MarkUnparsed` is declared on `Notice`
[ ] `encodeNotice` maps `Notice.MarkUnparsed` to the tag `markUnparsed`
[ ] `noticeFrom` maps the tag `markUnparsed` to `Notice.MarkUnparsed`
[ ] `writeNoticeRow` shares one arm for `Notice.NodeParsed` and `Notice.MarkUnparsed`
[ ] `applyNotice` has the arm `Notice.MarkUnparsed nodeId`
[ ] `markUnparsedThenPush` builds `Notice.MarkUnparsed nodeId` for `writeParseState`
[ ] `pushReporting` and `makeMailBox` call `markUnparsedThenPush`
[ ] The comment above `applyNotice` includes the line `MarkUnparsed writes Unparsed only.`

### Tests this ticket names

[ ] [CoreLoopNoticeTests.fs](tests/Server.Tests/CoreLoopNoticeTests.fs) ``MarkUnparsed sets Unparsed and leaves PersistState``
[ ] [DbAgentNoticeTests.fs](tests/Server.Tests/DbAgentNoticeTests.fs) ``MarkUnparsed writes parse state to the database``
[ ] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) ``Notice JSON round trip``, ``An old nodeStamped row is skipped on read``
[ ] [ParseThreadLoadTests.fs](tests/Server.Tests/ParseThreadLoadTests.fs)
[ ] [LoadSaveCommandTests.fs](tests/Server.Tests/LoadSaveCommandTests.fs)

### Pattern

[ ] [84 — Remove the NodeStamped case](84-remove-the-nodestamped-case.md)

## 4. Context

### Direct write

[ ] The `Notice.MarkUnparsed` arm calls `writeParseState` with that notice, the node id, and `ParseState.Unparsed`
[ ] `writeParseState` calls `writeAxis`, then `replaceGraph`
[ ] `markUnparsedThenPush` calls `rawPush` when that write returns `Ok`
[ ] After 56 — Remove ParseThread markUnparsed, `markUnparsedThenPush` is the remaining constructor of `Notice.MarkUnparsed`

### Rows and legacy JSON

[ ] `writeNoticeRow` calls `writeDocumentState` for `Notice.NodeParsed` and for `Notice.MarkUnparsed`
[ ] `encodeNotice` writes `Notice.MarkUnparsed` with `encodeSingleNotice` and the tag `markUnparsed`
[ ] `noticeFrom` returns `Error` for an unknown tag
[ ] `EventLogFile.readAllEvents` and `DbAgent.loadReconciled` drop a row when `EventJson.decode` returns `Error`

## 5. Changes

### Contract

[ ] One contract: remove `Notice.MarkUnparsed` and the `applyNotice` arm

### History.fs

[ ] `Notice` cases are `NodeParsed`, `NodePersisted`, `NodeUploading`, `NodeReceived`, `NodeParsing`, `NodePersisting`, and `PersistFailed`

### EventJson.fs

[ ] `encodeNotice` matches each remaining `Notice` case
[ ] The tag `markUnparsed` falls through to the unknown-tag `Error` in `noticeFrom`

### Database.fs

[ ] `writeNoticeRow` calls `writeDocumentState` for `Notice.NodeParsed`

### CoreMailboxLoad.fs

[ ] `applyNotice` matches each remaining `Notice` case
[ ] Delete the comment line `MarkUnparsed writes Unparsed only.`
[ ] `markUnparsedThenPush` passes `Notice.NodeParsed nodeId` and `ParseState.Unparsed` to `writeParseState`

## 6. Tests

### Apply

[ ] Remove ``MarkUnparsed sets Unparsed and leaves PersistState`` from `CoreLoopNoticeTests.fs`
[ ] Add ``MarkUnparsed leaves Notice`` in `CoreLoopNoticeTests.fs`
[ ] The test asserts the remaining `Notice` cases from History.fs
[ ] Filter: `MarkUnparsed leaves Notice`
[ ] Filter: `CoreLoopNoticeTests`

### Database

[ ] Remove ``MarkUnparsed writes parse state to the database`` from `DbAgentNoticeTests.fs`
[ ] Filter: `DbAgentNoticeTests`

### JSON

[ ] ``Notice JSON round trip`` omits `Notice.MarkUnparsed` from its notice list
[ ] Add ``An old markUnparsed row is skipped on read`` in `EventJsonTests.fs`
[ ] The legacy body uses the same event fields as ``An old nodeStamped row is skipped on read``, with `"notice":"markUnparsed"`
[ ] `EventJson.decode` of that legacy body returns `Error`
[ ] Filter: `Notice JSON round trip`
[ ] Filter: `An old markUnparsed row is skipped on read`

### Stubs

[ ] Remove each `Notice.MarkUnparsed` construction in `ParseThreadLoadTests.fs`
[ ] Remove each `Notice.MarkUnparsed` construction in `LoadSaveCommandTests.fs`
[ ] Where `ParseThreadDeps` still has `markUnparsed`, the test value is `ignore`
[ ] Filter: `ParseThreadLoadTests`
[ ] Filter: `LoadSaveCommandTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
