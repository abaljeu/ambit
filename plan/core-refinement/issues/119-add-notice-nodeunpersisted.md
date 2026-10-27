# 119 — Add Notice.NodeUnpersisted

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] none

## 3. Problem

When the core applier stores a Change, Undo or Redo, `CoreEventDispatch.markForPersist` marks each owning Special Unpersisted and logs that as `Notice.NodePersisting`. Per the Notice table in [cqrs-messages](../cqrs-messages.md), a node change logs `Notice.NodeUnpersisted`, and `NodePersisting` belongs to the persist thread.

## 4. Change

[ ] [History.fs](../../../src/Shared/History.fs): add `| NodeUnpersisted of nodeId: NodeId` to `Notice`.
[ ] [CoreEventDispatch.fs](../../../src/Server/Core/CoreEventDispatch.fs) `markForPersist`: log `Notice.NodeUnpersisted nodeId` instead of `Notice.NodePersisting nodeId`. The axis write (`setUnpersistedAt` at the change's id) and the `pushPersist` that follows are unchanged.
[ ] [CoreMailboxLoop.fs](../../../src/Server/Core/CoreMailboxLoop.fs) `applyNotice`: the `NodeUnpersisted` arm sets Unpersisted, the same `writeAxis` call the `NodePersisting` arm makes.
[ ] [EventJson.fs](../../../src/Shared/EventJson.fs): encode and decode `NodeUnpersisted` as `"nodeUnpersisted"`.
[ ] [Database.fs](../../../src/Server/Database.fs) `writeNoticeRow`: the `NodeUnpersisted` arm writes the persist state, as the `NodePersisting` arm does.
[ ] Every other `match` on `Notice` gets a `NodeUnpersisted` arm with the same handling as `NodePersisting`.
[ ] `NodePersisting` keeps its current handling. Nothing in `src` posts it after this ticket.
[ ] A graph-only change logs no `NodeUnpersisted`.

## 5. Tests

[ ] `CoreLoopNoticeTests.fs`: add `applyNotice NodeUnpersisted marks Unpersisted`.
[ ] `CoreMailboxDoorTests.fs`: add `an edit logs NodeUnpersisted for its owning special`. Post an ordinary edit through `postEvents`. `getEventsSince` returns that Change. `replaceGraph` receives `Notice.NodeUnpersisted fileId`. Neither list has `Notice.NodePersisting`.
[ ] `CoreMailboxDoorTests.fs`: add `a graph-only change logs no NodeUnpersisted`. `postGraphOnly` of that same edit records no notice.
[ ] `EventJsonTests.fs` `Notice JSON round trip` includes `Notice.NodeUnpersisted`.
[ ] `DbAgentNoticeTests.fs`: add `NodeUnpersisted writes persist state to the database`.
[ ] Any existing test that expects the core applier to log `Notice.NodePersisting` for a change expects `Notice.NodeUnpersisted` instead.

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
