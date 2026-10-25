# 120 — NodePersisted and PersistFailed leave Notice

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [119 — The persist thread posts Notice.PersistStopped](119-the-persist-thread-posts-notice-persiststopped.md)

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `Notice`

### Source

[ ] [EventJson.fs](src/Shared/EventJson.fs) `encodeNodePersisted`, `encodePersistFailed`, `encodeNotice`, `decodeNodePersisted`, `decodePersistFailed`, `decodeNoticeBody`
[ ] [SyncLogic.fs](src/Shared/SyncLogic.fs) `persistFailedMessage`, `applyLoggedNotice`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `applyNotice` and the comment above `withPullingFile`
[ ] [Database.fs](src/Server/Database.fs) `writeNoticeRow`

### Docs

[ ] `doc/current/mailbox.md`, `doc/current/parse-persist.md`, `doc/current/workspace-file-sync.md`, `doc/current/file-agents.md`, `doc/current/db-agents.md`: each line that names `Notice.NodePersisted` or `Notice.PersistFailed`
[ ] `doc/current/op.md` and `doc/current/http-contract.md`: each `[ ]` line that names `Notice.NodeUnpersisted`

### Match sites and constructors

[ ] After 119, nothing in `src/` constructs `Notice.NodePersisted` or `Notice.PersistFailed`
[ ] Tests that still construct an old case, by file:
  - `tests/Shared.Tests/EventJsonTests.fs`
  - `tests/Shared.Tests/AckReconcileTests.fs`
  - `tests/Shared.Tests/SplitOriginTraceTests.fs`
[ ] `tests/Server.Tests/TestBackend.fs` names `Notice.NodePersisted` in a comment

### Tests this ticket names

[ ] `Stored snapshotDone decodes as NodePersisted` and `An old nodePersisted row decodes at version zero` in `EventJsonTests.fs`
[ ] `Notice in the reply keeps the confirmation echo` in `AckReconcileTests.fs`

## 4. Context

### Today

[ ] After 119, `Notice` holds `NodePersisted`, `PersistFailed`, and `PersistStopped`; only `PersistStopped` is posted
[ ] The event log can hold old rows with tag `nodePersisted`, `snapshotDone`, or `persistFailed`
[ ] Each old-case arm in Look at does the same as its `PersistStopped` arm

## 5. Changes

### Contract

[ ] One contract: `Notice.NodePersisted` and `Notice.PersistFailed` leave `Notice`

### History.fs

[ ] Remove `NodePersisted` and `PersistFailed` from `Notice`

### EventJson.fs

[ ] Remove `encodeNodePersisted`, `encodePersistFailed`, and their `encodeNotice` arms
[ ] `decodeNoticeBody` reads an old `nodePersisted` or `snapshotDone` row as `PersistStopped(nodeId, Written(persistedVersion, updateTime))`, with a missing `persistedVersion` as `EventId.zero`
[ ] `decodeNoticeBody` reads an old `persistFailed` row as `PersistStopped(nodeId, Failed message)`

### SyncLogic.fs, CoreMailboxLoad.fs, Database.fs

[ ] Remove the `NodePersisted` and `PersistFailed` arms
[ ] The comment in `CoreMailboxLoad.fs` names `PersistStopped` with `Written` and `Failed`

### Docs

[ ] Each doc line in Look at names `Notice.PersistStopped` with `Written` or `Failed`
[ ] Each `[ ]` line on `Notice.NodeUnpersisted` leaves; [cqrs-messages.md](plan/core-refinement/cqrs-messages.md) decision 12 **PersistStopped** closed that decision

### Size

[ ] 5 source files: `History.fs`, `EventJson.fs`, `SyncLogic.fs`, `CoreMailboxLoad.fs`, `Database.fs`
[ ] 4 test files: `EventJsonTests.fs`, `AckReconcileTests.fs`, `SplitOriginTraceTests.fs`, `TestBackend.fs` (a comment)

## 6. Tests

### Old rows

[ ] Change `Notice JSON round trip`: the list drops `NodePersisted` and `PersistFailed` and keeps both `PersistStopped` values
[ ] Rename `Stored snapshotDone decodes as NodePersisted` to `Stored snapshotDone decodes as PersistStopped Written`
[ ] Change `An old nodePersisted row decodes at version zero`: it expects `PersistStopped(id, Written(EventId.zero, time))`
[ ] Add `An old persistFailed row decodes as PersistStopped Failed` in `EventJsonTests.fs`
[ ] Filter: `EventJsonTests`

### Notice constructions

[ ] `AckReconcileTests.fs` and `SplitOriginTraceTests.fs` construct `PersistStopped(id, Written(EventId.zero, time))`; the asserts stay
[ ] Filter: `AckReconcileTests`
[ ] Filter: `SplitOriginTraceTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
