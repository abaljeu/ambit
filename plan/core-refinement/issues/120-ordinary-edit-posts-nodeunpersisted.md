# 120 — Persist thread posts NodePersisting

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [119 — Add Notice.NodeUnpersisted](119-add-notice-nodeunpersisted.md)

## 3. Problem

Per the Notice table in [cqrs-messages](../cqrs-messages.md), `NodePersisting` means the persist thread accepted the node for writing, and it changes no node state. Nothing in `src` posts it, and its `applyNotice` and `writeNoticeRow` arms still write Unpersisted.

## 4. Change

[ ] [PersistThread.fs](../../../src/Server/Core/PersistThread.fs) `runRequest`: when it takes a request it will write, post `Notice.NodePersisting request.nodeId` before `persistNode`.
[ ] [CoreMailboxLoop.fs](../../../src/Server/Core/CoreMailboxLoop.fs) `applyNotice`: the `NodePersisting` arm changes nothing.
[ ] [Database.fs](../../../src/Server/Database.fs) `writeNoticeRow`: the `NodePersisting` arm writes nothing.
[ ] [CoreMailboxFilesAxis.fs](../../../src/Server/Core/CoreMailboxFilesAxis.fs): the comment describes `NodePersisting` as log-only and `NodeUnpersisted` as setting Unpersisted.

## 5. Tests

[ ] `PersistThreadTests.fs`: add `persist thread posts NodePersisting before NodePersisted`.
[ ] `CoreLoopNoticeTests.fs`: `NodePersisting` leaves the node unchanged.
[ ] `DbAgentNoticeTests.fs`: `NodePersisting` writes no row.

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
