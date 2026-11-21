# Remove file time from Notice.NodeStamped

A successful persist write still posts `Notice.NodePersisted` with the file time. The following stamp is `Notice.NodeStamped([], None)`. That empty stamp still runs the meta checkpoint. New stamps do not carry `Op.SetUpdateTime`.

## Code

[PersistThread.fs](src/Server/Core/PersistThread.fs) no longer builds stamp ops.

After `Ok updateTime`, the thread posts these notices in order:

- `Notice.NodePersisted(nodeId, updateTime)`
- `Notice.NodeStamped([], None)`

`stampOps` is deleted. Nothing else called it.

## Tests

These tests now require an empty op list on a freshly posted `Notice.NodeStamped`:

- [PersistThreadTests.fs](tests/Server.Tests/PersistThreadTests.fs)
- [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)

`scripts/agent-test.sh` ran and passed.

Filter: `FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~PersistThreadTests`

Result: Passed 28, Failed 0, Skipped 0.

`scripts/agent-test.sh --full` did not run. No commit.

## Kept on purpose

The file time that stays:

- `Notice.NodePersisted` and its `updateTime`
- `stampUpdateTime`
- `Database.writePersistedNode`
- the browser `withNodeUpdateTime` path

The meta checkpoint that stays:

- `FileAgent.pendingCheckpoint`
- `noteStamped`
- `Bookkeeping.writeEventId`
- the existing `Notice.NodeStamped([], None)` post when an edit has no file

Replay of an already-logged stamp that still has ops:

- `CoreMailboxLoad.applyStampNotice`
- `Database.writeStampTimes`
- `SyncLogic` apply of `Notice.NodeStamped` ops

Tests that build a `Notice.NodeStamped` with `Op.SetUpdateTime` stay as replay coverage. Tests that check `Notice.NodePersisted` `updateTime`, or that the meta file reads 1, stay as they are.

[arch.md](plan/core-refinement/arch.md) still says `Notice.NodeStamped` carries stamp ops. This run did not edit plan tickets or arch.
