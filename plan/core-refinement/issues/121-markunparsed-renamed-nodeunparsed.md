# 121 — MarkUnparsed renamed NodeUnparsed

**Status:** `done`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] none

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `Notice`

### Source

[ ] [EventJson.fs](src/Shared/EventJson.fs) `encodeNotice`, `noticeFrom`
[ ] [Database.fs](src/Server/Database.fs) `writeNoticeRow`, `writeDocumentState`
[ ] [CoreMailboxFilesAxis.fs](src/Server/Core/CoreMailboxFilesAxis.fs) `writeParseState`, `markUnparsedThenPush`
[ ] [CoreMailboxLoop.fs](src/Server/Core/CoreMailboxLoop.fs) `applyNotice`, `pushReporting`, `makeMailBox`
[ ] [ParseThread.fs](src/Server/ParseThread.fs) `bootCore`, `afterPost`, `finishParse`
[ ] [DocumentOpImpact.fs](src/Shared/DocumentOpImpact.fs) `noticesBeside`

### Helper

[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `writeAxis`
[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) `PersistHandlers.replaceGraph`

### Match sites

[ ] `Notice` declares `MarkUnparsed of nodeId: NodeId` at `History.fs` line 44
[ ] `documentStateFromNotice` maps `Notice.MarkUnparsed` to Unparsed at `History.fs` lines 448–449, in one arm with `Notice.NodeReceived`
[ ] `encodeNotice` maps `Notice.MarkUnparsed` to the tag `markUnparsed` at `EventJson.fs` lines 178–179
[ ] `noticeFrom` maps the tag `markUnparsed` to `Notice.MarkUnparsed` at `EventJson.fs` line 194
[ ] `writeNoticeRow` shares one arm for `Notice.NodeParsed` and `Notice.MarkUnparsed` at `Database.fs` lines 584–586
[ ] `applyNotice` has the arm `Notice.MarkUnparsed nodeId` at `CoreMailboxLoop.fs` lines 116–118
[ ] `markUnparsedThenPush` builds `Notice.MarkUnparsed nodeId` for `writeParseState` at `CoreMailboxFilesAxis.fs` lines 25–29
[ ] `pushReporting` calls `markUnparsedThenPush` at `CoreMailboxLoop.fs` line 305
[ ] `makeMailBox` binds `parsePush` to `markUnparsedThenPush` at `CoreMailboxLoop.fs` line 324
[ ] `bootCore` builds `Notice.MarkUnparsed nodeId` and posts it at `ParseThread.fs` lines 192–196
[ ] `noticesBeside` builds `Notice.MarkUnparsed id` at `DocumentOpImpact.fs` line 59
[ ] The comment in `CoreMailboxFilesAxis.fs` line 45 is `MarkUnparsed writes Unparsed only.`
[ ] The comment in `DocumentOpImpact.fs` lines 39–40 names `MarkUnparsed` for any Unparsed mark other than PullingFile to Unparsed

### Constructors

[ ] `CoreMailboxFilesAxis.markUnparsedThenPush` builds `Notice.MarkUnparsed nodeId`
[ ] `ParseThread.bootCore` builds `Notice.MarkUnparsed nodeId` in the `markUnparsed` closure
[ ] `DocumentOpImpact.noticesBeside` builds `Notice.MarkUnparsed id` when the new state is Unparsed and the old state is not PullingFile
[ ] `EventJson.noticeFrom` builds `Notice.MarkUnparsed nodeId` for the tag `markUnparsed`

### Tests this ticket names

[ ] [CoreLoopNoticeTests.fs](tests/Server.Tests/CoreLoopNoticeTests.fs) ``MarkUnparsed sets Unparsed and leaves PersistState`` applies `Notice.MarkUnparsed` at line 183 through `CoreMailboxLoop.applyNotice`
[ ] [DbAgentNoticeTests.fs](tests/Server.Tests/DbAgentNoticeTests.fs) ``MarkUnparsed writes parse state to the database`` applies `Notice.MarkUnparsed` at line 95
[ ] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) ``Notice JSON round trip`` lists `Notice.MarkUnparsed` at line 159
[ ] [ParseThreadLoadTests.fs](tests/Server.Tests/ParseThreadLoadTests.fs) builds `Notice.MarkUnparsed` at lines 492, 856, 924, and 1148
[ ] [LoadSaveCommandTests.fs](tests/Server.Tests/LoadSaveCommandTests.fs) `fileLoadHost` builds `Notice.MarkUnparsed` at line 690

### Pattern

[ ] [77 — Rename SnapshotDone to NodePersisted, and Notices start no catch-up](77-rename-snapshotdone-and-notices-start-no-catch-up.md) writes the new JSON tag and still reads the old tag

## 4. Context

### Order

[ ] `Notice.NodeUnparsed` is posted before the parse, when a node is marked Unparsed and pushed for parsing
[ ] `Notice.NodeParsed` is posted after the nodes are done parsing
[ ] `markUnparsedThenPush` calls `writeParseState` with `Notice.MarkUnparsed`, then calls `rawPush` when that write returns `Ok`
[ ] `writeParseState` calls `writeAxis`, then `replaceGraph`
[ ] `ParseThread.afterPost` calls `deps.markUnparsed` on the push list, and that closure posts `Notice.MarkUnparsed`
[ ] `ParseThread.finishParse` posts `Notice.NodeParsed`

### Rows and legacy JSON

[ ] `writeNoticeRow` calls `writeDocumentState` for `Notice.NodeParsed` and for `Notice.MarkUnparsed`
[ ] `encodeNotice` writes `Notice.MarkUnparsed` with `encodeSingleNotice` and the tag `markUnparsed`
[ ] `noticeFrom` maps the tag `markUnparsed` to `Notice.MarkUnparsed`
[ ] `EventLogFile.readAllEvents` and `DbAgent.loadReconciled` load a row when `EventJson.decode` returns `Ok`

## 5. Changes

### Contract

[ ] Rename `Notice.MarkUnparsed` to `Notice.NodeUnparsed`
[ ] `Notice.NodeUnparsed` is posted before the parse
[ ] `Notice.NodeParsed` is posted after the nodes are done parsing

### History.fs

[ ] The case is `NodeUnparsed of nodeId: NodeId`
[ ] `documentStateFromNotice` maps `Notice.NodeUnparsed` to Unparsed

### EventJson.fs

[ ] `encodeNotice` writes `Notice.NodeUnparsed` with the tag `nodeUnparsed`
[ ] `noticeFrom` maps the tag `nodeUnparsed` to `Notice.NodeUnparsed`
[ ] `noticeFrom` maps the tag `markUnparsed` to `Notice.NodeUnparsed`
[ ] Existing logs and database rows that store `markUnparsed` load as `Notice.NodeUnparsed`

### Database.fs

[ ] `writeNoticeRow` shares one arm for `Notice.NodeParsed` and `Notice.NodeUnparsed`
[ ] That arm calls `writeDocumentState`

### CoreMailboxLoop.fs

[ ] `applyNotice` has the arm `Notice.NodeUnparsed nodeId`
[ ] That arm calls `writeParseState` with the notice, the node id, and `ParseState.Unparsed`

### CoreMailboxFilesAxis.fs

[ ] `markUnparsedThenPush` builds `Notice.NodeUnparsed nodeId` for `writeParseState`, then pushes
[ ] The comment line is `NodeUnparsed writes Unparsed only.`

### ParseThread.fs

[ ] The `bootCore` `markUnparsed` closure posts `Notice.NodeUnparsed nodeId`
[ ] `finishParse` posts `Notice.NodeParsed nodeId`

### DocumentOpImpact.fs

[ ] `noticesBeside` builds `Notice.NodeUnparsed id` for that Unparsed arm
[ ] The comment names `NodeUnparsed`

## 6. Tests

### Apply

[ ] Rename ``MarkUnparsed sets Unparsed and leaves PersistState`` to ``NodeUnparsed sets Unparsed and leaves PersistState`` in `CoreLoopNoticeTests.fs`
[ ] The test applies `Notice.NodeUnparsed`
[ ] Filter: `NodeUnparsed sets Unparsed and leaves PersistState`
[ ] Filter: `CoreLoopNoticeTests`

### Database

[ ] Rename ``MarkUnparsed writes parse state to the database`` to ``NodeUnparsed writes parse state to the database`` in `DbAgentNoticeTests.fs`
[ ] The test applies `Notice.NodeUnparsed`
[ ] Filter: `DbAgentNoticeTests`

### JSON

[ ] ``Notice JSON round trip`` lists `Notice.NodeUnparsed`
[ ] Add ``An old markUnparsed row decodes as NodeUnparsed`` in `EventJsonTests.fs`
[ ] The legacy body uses the same event fields as ``Stored snapshotDone decodes as NodePersisted``, with `"notice":"markUnparsed"`
[ ] `EventJson.decode` of that legacy body returns `Notice.NodeUnparsed`
[ ] Filter: `Notice JSON round trip`
[ ] Filter: `An old markUnparsed row decodes as NodeUnparsed`

### Stubs

[ ] `ParseThreadLoadTests.fs` lines 492, 856, 924, and 1148 build `Notice.NodeUnparsed`
[ ] `LoadSaveCommandTests.fs` `fileLoadHost` builds `Notice.NodeUnparsed`
[ ] `markUnparsed = ignore` stays `ignore` in `ParseThreadLoadTests.fs`, `MailboxExpandTests.fs`, and `NodeEventExpandTests.fs`
[ ] Filter: `ParseThreadLoadTests`
[ ] Filter: `LoadSaveCommandTests`

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.

See [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md): `Notice.NodeUnparsed` covers `Current → Unparsed` (P6, P9).
