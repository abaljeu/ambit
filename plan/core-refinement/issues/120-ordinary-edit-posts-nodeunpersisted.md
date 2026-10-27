# 120 — Persist thread posts NodePersisting

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[x] `coding`

## 2. Blockers

[x] [119 — Add Notice.NodeUnpersisted](119-add-notice-nodeunpersisted.md)

## 3. Problem

Per the Notice table in [cqrs-messages](../cqrs-messages.md), `NodePersisting` means the persist thread accepted the node for writing, and it changes no node state. `Notice.NodeUnpersisted` sets Unpersisted and is logged.

## 4. Change

[x] [PersistThread.fs](../../../src/Server/Core/PersistThread.fs) `runRequest`: when it takes a request it will write, post `Notice.NodePersisting request.nodeId` before `persistNode`.
[x] [CoreMailboxLoop.fs](../../../src/Server/Core/CoreMailboxLoop.fs) `applyNotice`: the `NodePersisting` arm changes nothing. The `NodeUnpersisted` arm sets Unpersisted.
[x] [Database.fs](../../../src/Server/Database.fs) `writeNoticeRow`: the `NodePersisting` arm writes nothing. The `NodeUnpersisted` arm writes persist state.
[x] [CoreMailboxFilesAxis.fs](../../../src/Server/Core/CoreMailboxFilesAxis.fs): the comment describes `NodePersisting` as log-only and `NodeUnpersisted` as setting Unpersisted.

## 5. Tests

[x] `PersistThreadTests.fs`: `persist thread posts NodePersisted on success only` and `persist thread posts the version it read` expect `Notice.NodePersisting` then `Notice.NodePersisted`. A failed write expects `Notice.NodePersisting` then `Notice.PersistFailed`.
[x] `CoreLoopNoticeTests.fs`: `NodePersisting changes no node state`. `applyNotice NodeUnpersisted marks Unpersisted` still sets Unpersisted.
[x] `DbAgentNoticeTests.fs`: `NodePersisting writes no persist row`. `NodeUnpersisted writes persist state to the database` still writes the row.
[x] `CoreMailboxApi.postEvent appends an Ev that eventsSince returns` expects Change, then `Notice.NodePersisting`, then `Notice.NodePersisted`.
[x] `new special node's first file write posts NodePersisting` expects `Notice.NodePersisting` for that workspace before `Notice.NodePersisted`.
[x] `soft-fail Edit is not in the graph after restart` checks that no node text is `soft-fail-probe`. It does not assert `eventId`.

## 6. Done

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[x] All executed tests pass.
