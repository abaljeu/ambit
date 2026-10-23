# 100 — Core loop gates Persisted on the written version

**Status:** `defined`

> **Decided (Alan, 2026-10-23).** "persistedVersion is the version of the graph that was written to file." Every `NodePersisted` sets `persistedVersion` to the written version. `persistState` becomes `Persisted` only when `persistedVersion >= changedVersion`. Otherwise it stays `Unpersisted` and `changedVersion` does not change.

## 1. Type
bug-fixing

## 2. Blockers
- [98 — Core marks carry the Edit's id](98-core-marks-carry-the-edits-id.md)
- [99 — NodePersisted carries the written version](99-nodepersisted-carries-the-written-version.md)

## 3. Look at
- Files:
  - `src/Server/Core/CoreMailboxLoad.fs`
  - `src/Server/Core/CoreEventDispatch.fs`
  - `src/Server/Core/PersistThread.fs`
  - `tests/Server.Tests/CoreLoopNoticeTests.fs`
- Types:
  - `PersistState`, `DocumentState`, `Notice`, `PersistHandlers`
- Functions:
  - `CoreMailboxLoad.applyNotice` (the `Notice.NodePersisted` arm), `CoreMailboxLoad.writePersisted`, `CoreMailboxLoad.stampUpdateTime`
  - `CoreEventDispatch.pushChangeSpecials`, `CoreEventDispatch.persistedNode`
  - `PersistThread.runRequest` and its `stillPersisted` skip
  - `GraphMutate.setPersistState`

## 4. Context
A persist write can finish after a later graph change:

1. Edit N marks the File Node `Unpersisted` with `changedVersion` N, and `pushChangeSpecials` pushes it.
2. The persist thread takes the request, reads the State at N, and writes it.
3. Edit N+1 sets `changedVersion` to N+1 and pushes the node again.
4. `NodePersisted(node, N, time)` arrives. `CoreMailboxLoad.writePersisted` sets `Persisted` for every such notice.
5. The thread takes the second request. `stillPersisted` is true, so it skips the write. Edit N+1 never reaches the file.

After [99 — NodePersisted carries the written version](99-nodepersisted-carries-the-written-version.md), the notice carries N. The handler does not read it.

## 5. Changes
The `Notice.NodePersisted` arm of `CoreMailboxLoad.applyNotice`, through `writePersisted`:

- sets that node's `persistedVersion` to the written version
- applies `updateTime`
- sets `Persisted` when the node is `Persisted`, or when it is `Unpersisted` and `EventId.value persistedVersion >= EventId.value node.documentState.changedVersion`
- otherwise leaves the node `Unpersisted`, with `changedVersion` unchanged

`persist.noteDiffDone` and `persist.notePersisted` run as today. `PersistThread.runRequest` and `persistedNode` do not change. Because the node stays Unpersisted, the second request in the Context is written.

## 6. Tests
- Add `NodePersisted for an older version leaves the node Unpersisted` in `tests/Server.Tests/CoreLoopNoticeTests.fs`. The handler sets `persistedVersion` to the written version. A written version equal to `changedVersion` sets `Persisted`, including at `EventId.zero`. A written version older than `changedVersion` leaves `Unpersisted`, with `changedVersion` unchanged.
- Keep `NodePersisted sets Persisted and calls the diff hook` passing.
- Filters:
  - `scripts/agent-test.sh --filter "NodePersisted for an older version leaves the node Unpersisted"`
  - `scripts/agent-test.sh --filter "NodePersisted sets Persisted and calls the diff hook"`
  - `scripts/agent-test.sh --filter "PersistThreadTests"`

## 7. Done
- The named tests pass.
- A persist that read an earlier `State` leaves the node `Unpersisted` at the later graph change, and sets `persistedVersion` to the version that was written.
- Upload, import, parse, Load, undo and persist complete.
