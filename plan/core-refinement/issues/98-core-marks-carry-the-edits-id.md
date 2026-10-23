# 98 — Core marks carry the Edit's id

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [97 — Graph change records changedVersion](97-graph-change-records-changedversion.md)

## 3. Look at
- Files:
  - `src/Shared/ChangeAmendment.fs`
  - `src/Server/Core/FileAgent.fs`
  - `src/Server/Core/DbAgent.fs`
  - `src/Server/Core/CoreEventDispatch.fs`
  - `src/Server/Core/CoreMailboxLoad.fs`
  - `tests/Server.Tests/CoreMailboxDoorTests.fs`
  - `tests/Shared.Tests/LazyLoadReconciliationTests.fs`
  - `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`
  - `tests/Shared.Tests/DirectoryReconcileTests.fs`
- Types:
  - `PersistHandlers` in `src/Server/Core/CoreMsg.fs`, `Ev`, `Notice`, `DocumentState`
- Functions:
  - `ChangeAmendment.applyForCommand`, `ChangeAmendment.applyForGraphOnly`
  - `FileAgent.applyOne`, `DbAgent.applyOneEvent` (the `PersistHandlers.applyEvent` bodies)
  - `CoreEventDispatch.persist`, `CoreEventDispatch.pushChangeSpecials`, `CoreEventDispatch.markForPersist`, `CoreEventDispatch.writeAxis`
  - `CoreMailboxLoad.writePullingFileUnpersisted`, and the `Notice.NodePersisting` arm of `CoreMailboxLoad.applyNotice`
  - `CoreMailboxLoad.dispatchItem` (logs a Notice, then applies it)
  - `GraphMutate.setUnpersistedAt` and `Node.withUnpersistedAt`, from [97 — Graph change records changedVersion](97-graph-change-records-changedversion.md) and [94 — Document state carries persist versions](94-document-state-carries-persist-versions.md)
- Callers of `applyForCommand` and `applyForGraphOnly` in tests:
  - `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, 1
  - `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`, 1
  - `tests/Shared.Tests/DirectoryReconcileTests.fs`, 1

## 4. Context
After [97 — Graph change records changedVersion](97-graph-change-records-changedversion.md), the Core marks leave `changedVersion` at `EventId.zero`:

- `FileAgent.applyOne` and `DbAgent.applyOneEvent` call `ChangeAmendment.applyForCommand` or `applyForGraphOnly`, which mark at `EventId.zero`.
- `CoreEventDispatch.pushChangeSpecials`, from [82 — The mailbox pushes an Edit's special nodes](82-the-mailbox-pushes-an-edits-special-nodes.md), calls `markForPersist` for each owning special node of an Edit, then pushes each id. `markForPersist` writes `Unpersisted` through `writeAxis` with `Notice.NodePersisting` only when the node is `Persisted`. It does nothing for a node that is already Unpersisted.
- `CoreMailboxLoad.writePullingFileUnpersisted` (for `Notice.NodeUploading`) and the `Notice.NodePersisting` arm write `Unpersisted` through `GraphMutate.setPersistState` and leave `changedVersion` as it is.
- `dispatchItem` logs a posted Notice before it applies it. So in `applyNotice`, `persist.getState ()` returns the state whose `eventId` is that Notice's own logged id.

## 5. Changes
- `ChangeAmendment.applyForCommand` and `applyForGraphOnly` take `changedVersion: EventId` first. They pass `Some changedVersion` when `Op.marksOwningPersist` is true, and `None` when false.
- `FileAgent.applyOne` and `DbAgent.applyOneEvent` pass `event.id`.
- `CoreEventDispatch.markForPersist` takes the completed Edit's id. It writes through `GraphMutate.setUnpersistedAt nodeId completed.id` when the node is `Persisted` or its `changedVersion` is older than `completed.id`. A newer mark overwrites an older `changedVersion`, including when the node is already Unpersisted. `pushChangeSpecials` passes `completed.id`. It still pushes every owning special id.
- `CoreMailboxLoad.writePullingFileUnpersisted` and the `Notice.NodePersisting` arm write through `GraphMutate.setUnpersistedAt` at the `eventId` from `persist.getState ()`, which is the Notice's own id. A newer mark overwrites an older `changedVersion`.
- The three test callers pass an `EventId`.

## 6. Tests
- Add `An Edit marks its owning special node at the Edit's id` in `tests/Server.Tests/CoreMailboxDoorTests.fs`. Two Edits posted to the same File Node's text leave that File Node `Unpersisted` with `changedVersion` at the second Edit's id.
- Change the three test callers in Look at to pass an `EventId`.
- Filters:
  - `scripts/agent-test.sh --filter "An Edit marks its owning special node at the Edit's id"`
  - `scripts/agent-test.sh --filter "An Edit pushes its owning special node"`
  - `scripts/agent-test.sh --filter "LazyLoadReconciliationTests"`
  - `scripts/agent-test.sh --filter "DirectoryReconcileTests"`

## 7. Done
- The named tests pass.
- No Core writer marks an Edit's node `Unpersisted` without setting `changedVersion` to that Edit's id or newer.
- Upload, import, parse, Load, undo and persist complete.
