# 14 — Directory reconcile matches disk members

**Status:** `coded`
Actual: 1h 45m
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

A person reconciles a Directory Node. The reconcile examines that directory on disk. An earlier one-level reconcile marked each Current child Directory Node Unparsed on every parent reconcile. Alan replaced that mark on 2026-10-12. The Directory reconcile home is Parse thread architecture §2 Module map, item 1 Directory reconcile.

When a Directory parse finishes, the Browser never learns that the Directory became Current. `Notice.ParseFinished` sets the parse axis through `FileAgent.replaceGraph`, which posts no Event. The client poll applies Events only, so the row stays Unparsed.

## What to build

Directory reconcile examines the directory on disk. It adds a missing member node. It deletes a member node that has no file. It does not mark an existing member Unparsed. A new File Node starts Unparsed. A new child Directory Node starts Unparsed. When the Directory Node or Workspace Node is Unparsed, the ops start with SetDocumentState from Unparsed to Current.

1. [x] **Add a missing member** — A file on disk with no File Node gains a File Node. A child directory on disk with no Directory Node gains a Directory Node. The new File Node starts Unparsed. The new Directory Node starts Unparsed.
2. [x] **Delete a member with no file** — A member node that has no file is deleted.
3. [x] **Leave an existing member** — The reconcile does not mark an existing File Node Unparsed. The reconcile does not mark an existing child Directory Node Unparsed. A disk-newer member is an existing member.
4. [x] **Post Current** — When the Directory Node or Workspace Node is Unparsed, `DirectoryReconcile.planDirectoryReconcile` prepends `Op.SetDocumentState(id, Unparsed, Current)` to the ops `ParseThread.parseDirectory` posts. That op is present when there are no member ops.

### Acceptance Criteria

1. [x] **New File Node** — A file the Graph lacks gets a File Node. That File Node starts Unparsed.
2. [x] **New child Directory Node** — A child directory the Graph lacks gets a Directory Node. That Directory Node starts Unparsed.
3. [x] **No file** — A member node that has no file is gone after the reconcile.
4. [x] **Existing File Node** — An existing File Node is not marked Unparsed.
5. [x] **Existing child Directory Node** — An existing child Directory Node is not marked Unparsed.
6. [x] **Load of a newly Unparsed child Directory reconciles one level** — A parent Load adds the child Unparsed. A child Load adds its member. Replaying Events leaves the child Current.

## Finish-up

1. [x] **Piece answers** — The push list from Directory reconcile is empty. A missing member is removed with the existing drop-owned delete. `Notice.ParseFinished` still sets the parse axis with no Event.
2. [x] **Basis and reference docs** — [Parse thread architecture](../arch.md) §2 Module map, item 1 **Directory reconcile**, [Parse thread spec](../spec.md), and the Directory reconcile sentences in [core-refinement architecture](../../core-refinement/arch.md) match this ticket.
3. [x] **Current and user docs** — [Parse and persist](../../../doc/current/parse-persist.md) and [Mailbox](../../../doc/current/mailbox.md) match this ticket. doc/user does not state this reconcile.

## Implementation

1. **Members** — [Directory reconcile](../../../src/Shared/dotnet/DirectoryReconcile.fs) `planDirectoryReconcile` adds a missing File Node and a missing child Directory Node with `Op.NewSpecialNode`. Both start Unparsed. A member whose name is absent from that directory is deleted. The push list is empty, so an existing member is not marked Unparsed.
2. **Current** — When that Directory Node or Workspace Node is Unparsed, the same plan prepends `Op.SetDocumentState(id, Unparsed, Current)`, including when there are no member ops. [Parse thread](../../../src/Server/ParseThread.fs) `parseDirectory` posts those ops.
3. **Proof** — [Directory reconcile tests](../../../tests/Shared.Tests/DirectoryReconcileTests.fs) and [Parse thread load tests](../../../tests/Server.Tests/ParseThreadLoadTests.fs) `Load of a newly Unparsed child Directory reconciles one level`.

## Side effects

1. **Update time** — `Op.SetDocumentState` touches `updateTime` on the Directory Node or Workspace Node. This ticket does not change that op.
2. **Parsed axis** — `Notice.ParseFinished` still sets Parsed through `FileAgent.replaceGraph` and posts no Event. The Browser learns Current from the new op. It does not learn Parsed from an Event.
3. **Disk-newer text** — An existing disk-newer File Node is not parsed by this reconcile. Its text stays as it was.
4. **Delete path** — A member with no file is removed by `ViewModelDeleteOps.planDeleteDroppedOwnedMany`, which moves that node to trash.

## Time

- 2026-10-12 1h — directory reconcile members, Current event, and tests (from chat)
- 2026-10-12 45m — Load posts the Unparsed Event (from chat)

## See also

1. [Parse thread architecture — §2 Module map, Directory reconcile](../arch.md#2-module-map)
