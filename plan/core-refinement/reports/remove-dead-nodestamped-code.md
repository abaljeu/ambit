# Remove dead NodeStamped code

Production posts only `Notice.NodeStamped`. The two posts are [FileAgent.fs](src/Server/Core/FileAgent.fs) when an edit has no file, and [PersistThread.fs](src/Server/Core/PersistThread.fs) after a successful write. No production code posts stamp ops or a message.

Dead code is code that runs only for a non-empty op list or for `Some` message.

## Removed

- `applyStampOps` and `applyStampNotice` in [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs). An empty list did not change the graph. The handler now keeps the graph and calls `noteStamped`.
- `writeStampTimes` and `updateStampTimeSql` in [Database.fs](src/Server/Database.fs). `NodeStamped` writes no database row.
- `projectStampOps` in [SyncLogic.fs](src/Shared/SyncLogic.fs). A logged `NodeStamped` now follows the other notices: the browser advances the event id and does not change the graph. `NodePersisted` still sets `updateTime`.
- The `NodeStamped` arm of `opsForDownload`. A notice has no ops. `Ev.ops` already returns none. An empty stamp list downloaded nothing, so this live post is unchanged.
- The `Some` message arm in [FileAgent.fs](src/Server/Core/FileAgent.fs). That arm consumed the checkpoint and set `persistClean` false. The `None` checkpoint path remains.
- The `Some` message arm in [DbAgent.fs](src/Server/Core/DbAgent.fs). That arm skipped the pending notify ids. The baseline update and the file diff remain.
- The `ops` and `message` fields on `Notice.NodeStamped` in [History.fs](src/Shared/History.fs). `noteStamped` is now `unit -> unit` in [CoreMsg.fs](src/Server/Core/CoreMsg.fs).

## Left

- The `Notice.NodeStamped` case.
- Both posts named above.
- The mailbox log. [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) still calls `CoreEventDispatch.logNotice` before `applyNotice`. That path was not edited.
- FileAgent `noteStamped` for no message. It still writes the checkpoint through `Bookkeeping.writeEventId` when the queue has an event id and `persistClean` is true.
- DbAgent `noteStamped` for no message. It still records the baseline graph and can request a file diff.
- `Notice.NodePersisted` `updateTime` on the server graph (`stampUpdateTime`), on the database row (`writePersistedNode`), and in the browser (`withNodeUpdateTime`).
- The `nodeStamped` decoder in [EventJson.fs](src/Shared/EventJson.fs). A logged row can still contain `ops` and `message`. `Decode.succeed` ignores both fields, so an old row still decodes. New rows omit both fields. The type fields were dropped because the decoder does not need them.
- [PersistStamp.fs](src/Shared/PersistStamp.fs). No file under `src/` calls it. It does not apply, write, or project a `NodeStamped` op list. Tests call it.
- `takeSuffix`, `isStampOp`, and `projectSuffixes` in [SyncLogic.fs](src/Shared/SyncLogic.fs). They accept a `SetUpdateTime` suffix on a change confirmation. That suffix is not the `NodeStamped` op list.
- `planAlignFileStampOps` and the `SetUpdateTime` ops in [DocumentPersistWrite.fs](src/Server/DocumentPersistWrite.fs). Those paths are file alignment and parse, not `NodeStamped`.

`doc/` still says that `NodeStamped` carries stamp ops and a persist message. This run did not edit `doc/`.

## Tests

This run did not edit `tests/`. It did not run `scripts/agent-test.sh`. The other agent owns `tests/`.

`dotnet build src/Server/Gambol.Server.fsproj` succeeded with 0 warnings. `scripts/client.sh build` succeeded.

These files will not compile until they stop passing ops, a message, or the old `noteStamped` arguments:

- [CoreLoopNoticeTests.fs](tests/Server.Tests/CoreLoopNoticeTests.fs)
- [TestBackend.fs](tests/Server.Tests/TestBackend.fs)
- [PersistThreadTests.fs](tests/Server.Tests/PersistThreadTests.fs)
- [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)
- [DbAgentNoticeTests.fs](tests/Server.Tests/DbAgentNoticeTests.fs)
- [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs)
- [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs)

`TestBackend.fs` is in the server test project, so that project does not compile while `noteStamped` still has two arguments.
