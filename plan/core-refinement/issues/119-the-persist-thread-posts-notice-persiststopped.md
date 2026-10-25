# 119 — The persist thread posts Notice.PersistStopped

**Status:** `defined`
**Trackers:** architecture

> **Size: needs Alan's OK.** This step changes 1 source file and 10 test files. The test edits are mechanical: each wait or match on the posted `Notice.NodePersisted` or `Notice.PersistFailed` matches `Notice.PersistStopped`. The posted case and every test that waits on it change in one step, so a split leaves the suite red between steps.

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [45 — Add Notice.PersistStopped beside NodePersisted and PersistFailed](45-remove-document-state-set-ops.md)

## 3. Look at

### Owner

[ ] [PersistThread.fs](src/Server/Core/PersistThread.fs) owns `runRequest`

### Source

[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `applyNotice`, read only; after 45 it applies `PersistStopped`
[ ] [SyncLogic.fs](src/Shared/SyncLogic.fs) `applyLoggedNotice`, `persistFailedMessage`, read only

### Match sites and constructors

[ ] `PersistThread.runRequest` is the only source poster of `Notice.NodePersisted` and `Notice.PersistFailed`
[ ] Tests that wait on or match the posted Notice, by file:
  - `tests/Server.Tests/PersistThreadTests.fs`
  - `tests/Server.Tests/FileAgentFailureTests.fs`
  - `tests/Server.Tests/DbAgentFailureTests.fs`
  - `tests/Server.Tests/CoreMailboxDoorTests.fs`
  - `tests/Server.Tests/DbAgentTests.fs`
  - `tests/Server.Tests/StateEndpointTests.fs`
  - `tests/Server.Tests/Issue41CoreMailboxTests.fs`
  - `tests/Server.Tests/PersistApplyTests.fs`
  - `tests/Server.Tests/DatabaseProjectionContractTests.fs`
  - `tests/Server.Tests/DbAgentNoticeTests.fs`

### Tests this ticket names

[ ] `persist thread posts NodePersisted on success only`, `NodePersisted carries the written file time`, `persist thread posts the version it read` in `PersistThreadTests.fs`
[ ] `Failed write posts PersistFailed` in `FileAgentFailureTests.fs` and in `DbAgentFailureTests.fs`
[ ] `ACK keeps the Edit events and NodePersisted follows the write` in `FileAgentFailureTests.fs`

## 4. Context

### Today

[ ] `runRequest` reads one `State`, skips a Persisted node, and writes the node from that graph
[ ] A good write posts `Notice.NodePersisted(request.nodeId, state.eventId, updateTime)`
[ ] A bad write, or a write that throws, posts `Notice.PersistFailed(request.nodeId, message)`
[ ] After 45, `applyNotice`, `applyLoggedNotice`, `persistFailedMessage`, `writeNoticeRow`, and the JSON codec handle `Notice.PersistStopped` the same as the two old cases

### Decisions

[ ] Alan, 2026-10-24: persist just stops; the server posts one `Notice.PersistStopped` per request that it writes

## 5. Changes

### Change

[ ] One change: `runRequest` posts `Notice.PersistStopped`, and the tests match it

### PersistThread.fs

[ ] A good write posts `Notice.PersistStopped(request.nodeId, PersistResult.Written(state.eventId, updateTime))`
[ ] A bad write posts `Notice.PersistStopped(request.nodeId, PersistResult.Failed message)`
[ ] A skipped Persisted node posts nothing, as today

### Tests

[ ] Each match `Notice.NodePersisted (id, v, t)` becomes `Notice.PersistStopped (id, PersistResult.Written (v, t))`
[ ] Each match `Notice.NodePersisted _` becomes `Notice.PersistStopped (_, PersistResult.Written _)`
[ ] Each match `Notice.PersistFailed (id, m)` becomes `Notice.PersistStopped (id, PersistResult.Failed m)`
[ ] `eventLabel` in `DatabaseProjectionContractTests.fs` gives `PersistStopped ROOT` for a `Written` stop of ROOT, and its expected list changes to match

### Size

[ ] 1 source file: `PersistThread.fs`
[ ] 10 test files, as listed in Look at

## 6. Tests

### Persist thread

[ ] Rename `persist thread posts NodePersisted on success only` to `persist thread posts PersistStopped for each write`; a good write posts `[ PersistStopped(id, Written _) ]` and a bad write posts `[ PersistStopped(badId, Failed "disk full") ]`
[ ] Rename `NodePersisted carries the written file time` to `PersistStopped Written carries the written file time`
[ ] Keep `persist thread posts the version it read`; it reads the version from `Written`
[ ] Filter: `PersistThreadTests`

### Failed write

[ ] Rename `Failed write posts PersistFailed` to `Failed write posts PersistStopped Failed` in `FileAgentFailureTests.fs` and in `DbAgentFailureTests.fs`; the edit stays in the graph and the message is the write error
[ ] Filter: `Failed write posts PersistStopped Failed`

### Edit ack

[ ] Rename `ACK keeps the Edit events and NodePersisted follows the write` to `ACK keeps the Edit events and PersistStopped follows the write`
[ ] Filter: `FileAgentFailureTests`

### Kept

[ ] The other tests in Look at keep their names and asserts
[ ] Filter: `CoreMailboxDoorTests`
[ ] Filter: `StateEndpointTests`
[ ] Filter: `DbAgentTests`
[ ] Filter: `DatabaseProjectionContractTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
