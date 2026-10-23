# 97 — Graph change records changedVersion

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [94 — Document state carries persist versions](94-document-state-carries-persist-versions.md)

## 3. Look at
- Files:
  - `src/Shared/GraphMutate.fs`
  - `src/Shared/GraphOps.fs`
  - `src/Shared/History.fs`
  - `src/Shared/ChangeAmendment.fs`
  - `src/Shared/ResidentProjection.fs`
  - `src/Shared/SyncLogic.fs`
  - `src/Shared/BootCache.fs`
  - `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`
- Types:
  - `PersistState`, `DocumentState`, `EventId`, `Ev`, `ApplyResult`
- The mark this ticket versions: `GraphMutate.markOwningSpecialUnpersisted`, called from `GraphMutate.finishPersist`.
- The helper from [94 — Document state carries persist versions](94-document-state-carries-persist-versions.md): `Node.withUnpersistedAt`.
- `GraphMutate.setPersistState`, the writer the mailbox marks use today.
- Each function that takes `markUnpersisted: bool` today, which takes `changedVersion: EventId option` in its place:
  - `GraphMutate.finishPersist`, `GraphMutate.setText`, `GraphMutate.setClasses`, `GraphMutate.setName`, `GraphMutate.replace`
  - `Op.applyAllowed`, `Op.applyAllowing`, `Op.applyAllAllowing` in `History.fs`
  - `ChangeValidation.applyOpsAllowing` in `History.fs`
  - `ChangeAmendment.applyMarked`
  - `ResidentProjection.applyOpAllowing`, `ResidentProjection.applyOpsAllowing`, `ResidentProjection.applyOpForSyncAllowing`, `ResidentProjection.applyOpsForSyncAllowing`
- Callers that pass the bool today:
  - `Ev.apply` in `History.fs` (`Op.marksOwningPersist event.commandName`)
  - `Op.apply`, `Op.applyAll`, `ChangeValidation.applyOps` in `History.fs`
  - `ResidentProjection.applyOp`, `applyOps`, `applyOpForSync`, `applyOpsForSync`
  - `ChangeAmendment.applyOps`, `ChangeAmendment.applyForCommand`, `ChangeAmendment.applyForGraphOnly`
  - `SyncLogic.foldProjectedEvents`
  - `BootCache` log fold (the `List.fold` over `ordered` that calls `ResidentProjection.applyOpsAllowing guard mark`)
  - `Graph.setText`, `Graph.setClasses`, `Graph.setName`, `Graph.replace` in `GraphOps.fs` (pass `true`)
- Helper that decides the mark today: `Op.marksOwningPersist` (false only for `"Parse"`).

## 4. Context
After [94 — Document state carries persist versions](94-document-state-carries-persist-versions.md), `DocumentState` has `changedVersion` and `persistedVersion`, both `EventId.zero`. `GraphMutate.markOwningSpecialUnpersisted` sets `Unpersisted` on the nearest owning special node. It writes only when that node is not already Unpersisted, and it does not touch `changedVersion`. So a second graph change to a node that is already Unpersisted records nothing. The apply chain carries a `markUnpersisted: bool` and no version.

## 5. Changes
- Replace `markUnpersisted: bool` with `changedVersion: EventId option` on each function in Look at. `None` leaves the persist axis as it is. `Some version` asks `markOwningSpecialUnpersisted` to mark at `version`.
- `markOwningSpecialUnpersisted` drops its `persistState <> Unpersisted` guard and calls `Node.withUnpersistedAt version` on the owning node. It sets `Unpersisted` and sets `changedVersion` to the newer of the old value and `version`. A newer mark overwrites an older `changedVersion`, including when the node is already Unpersisted.
- Add `GraphMutate.setUnpersistedAt (nodeId: NodeId) (version: EventId) (graph: Graph)`, the same write on one node by id. [98 — Core marks carry the Edit's id](98-core-marks-carry-the-edits-id.md) uses it.
- These callers pass the event's own id, `Some event.id`, when `Op.marksOwningPersist event.commandName` is true, and `None` when it is false:
  - `Ev.apply`
  - `SyncLogic.foldProjectedEvents`
  - the `BootCache` log fold
- Callers with no event pass `Some EventId.zero`, which keeps today's mark. They are not the Core's apply path:
  - `Op.apply`, `Op.applyAll`, `ChangeValidation.applyOps`
  - `ResidentProjection.applyOp`, `applyOps`, `applyOpForSync`, `applyOpsForSync`
  - `ChangeAmendment.applyOps`
  - `Graph.setText`, `Graph.setClasses`, `Graph.setName`, `Graph.replace` in `GraphOps.fs`
- `ChangeAmendment.applyForCommand` and `applyForGraphOnly` keep their signatures in this step. They pass `Some EventId.zero` when `Op.marksOwningPersist` is true, and `None` when false. [98 — Core marks carry the Edit's id](98-core-marks-carry-the-edits-id.md) gives them the event's id.
- Do not use the pre-change `state.eventId` as a `changedVersion`. A persist that read that earlier state would then pass the version gate.

## 6. Tests
- Add `graph change sets Unpersisted with the newer version` in `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`. `Ev.apply` of an accepted Edit with id N on a Persisted File Node's child leaves the File Node `Unpersisted` with `changedVersion` N. A second Edit with id N+1 leaves it `Unpersisted` with `changedVersion` N+1. A `"Parse"` event leaves it as it was.
- Filters:
  - `scripts/agent-test.sh --filter "graph change sets Unpersisted with the newer version"`
  - `scripts/agent-test.sh --filter "SpecialNodeStateAxesTests"`
  - `scripts/agent-test.sh --filter "SyncLogicTests"`

## 7. Done
- The named tests pass.
- No function in `src/Shared` takes `markUnpersisted: bool`.
- Upload, import, parse, Load, undo and persist complete.
