# 119 — Add Notice.NodeUnpersisted

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] none

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `Notice`

### Source

[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `applyNotice`, `writeUnpersisted`
[ ] [EventJson.fs](src/Shared/EventJson.fs) `encodeNotice`, `noticeFrom`, `encodeSingleNotice`
[ ] [Database.fs](src/Server/Database.fs) `writeNoticeRow`

### Helper

[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `writeAxis`
[ ] [GraphMutate.fs](src/Shared/GraphMutate.fs) `setPersistState`

### Match sites

[ ] `applyNotice`, `encodeNotice`, `noticeFrom`, and `writeNoticeRow` match every `Notice` case

### Caller

[ ] [CoreLoopNoticeTests.fs](tests/Server.Tests/CoreLoopNoticeTests.fs) `apply` calls `applyNotice`

### Tests this ticket names

[ ] `NodePersisting matches the Unpersisted Set` in `CoreLoopNoticeTests.fs`
[ ] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) `Notice JSON round trip`

### Pattern

[ ] [73 — Add the NodeParsing and NodePersisting Notices](73-add-the-nodeparsing-and-nodepersisting-notices.md)

## 4. Context

### Copy-from

[ ] `Notice` is `[<RequireQualifiedAccess>]`
[ ] `Notice` cases are `NodeParsed`, `NodePersisted`, `MarkUnparsed`, `NodeUploading`, `NodeReceived`, `NodeParsing`, `NodePersisting`, and `PersistFailed`
[ ] The `Notice.NodePersisting nodeId` arm of `applyNotice` is `CoreEventDispatch.writeAxis persist msg (fun live -> writeUnpersisted nodeId live)`
[ ] `writeUnpersisted` calls `setPersistState nodeId PersistState.Unpersisted live`
[ ] `Notice.NodePersisting` encodes with `encodeSingleNotice "nodePersisting" nodeId`
[ ] `"nodePersisting"` decodes to `Notice.NodePersisting nodeId`
[ ] The `Notice.NodePersisting nodeId` arm of `writeNoticeRow` calls `writePersistState connectionString graph nodeId`

## 5. Changes

### Expand

[ ] One expand: add `Notice.NodeUnpersisted of nodeId`

### History.fs

[ ] Add `| NodeUnpersisted of nodeId: NodeId` beside `NodePersisting`

### CoreMailboxLoad.fs

[ ] The `Notice.NodeUnpersisted nodeId` arm is the same `writeAxis` call as `Notice.NodePersisting`

### EventJson.fs

[ ] `encodeNotice` writes `Notice.NodeUnpersisted nodeId` with `encodeSingleNotice "nodeUnpersisted" nodeId`
[ ] `noticeFrom` maps `"nodeUnpersisted"` to `Ok (Notice.NodeUnpersisted nodeId)`

### Database.fs

[ ] The `Notice.NodeUnpersisted nodeId` arm calls `writePersistState connectionString graph nodeId`

## 6. Tests

### applyNotice

[ ] Add `applyNotice marks Unpersisted` in `CoreLoopNoticeTests.fs`
[ ] The test calls `applyNotice` with `Notice.NodeUnpersisted nodeId`
[ ] The node equals `Graph.setPersistState nodeId PersistState.Unpersisted` on the same graph
[ ] Filter: `applyNotice marks Unpersisted`

### JSON

[ ] `Notice JSON round trip` includes `Notice.NodeUnpersisted nodeId`
[ ] Filter: `Notice JSON round trip`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
