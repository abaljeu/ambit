# History.fs line loss — simplification, not a move

**Most of the missing lines were deleted unused/legacy code (Slice 5), not relocated.** Browser Undo was written as a new module in Slice 2; it is not a cut-paste of the old History stacks.

Baseline: `selective-client-sync` (cut-from in [[git.md]]). Current: working tree on `w/selective-client-loading-undo`.

## Counts

| Point | `History.fs` | `ClientHistory.fs` |
| --- | ---: | ---: |
| Cut-from `selective-client-sync` | 795 | absent |
| HEAD (Slice 2 committed) | 813 | 85 |
| Working tree | 677 | 95 |

Working vs HEAD: `History.fs` **−137 / +1** (net −136). `ClientHistory.fs` **+10** (`tryPeekUndoName` / `tryPeekRedoName`, Slice 6).

Working vs baseline: `History.fs` **−147 / +29** (net −118). The +29 is `Change.inverse` from Slice 2, still in `History.fs`. ClientHistory is a **new 95-line file**, not a destination for those 147 deletions.

Deleted History.fs lines with **no new home: ~137** (working vs HEAD). ClientHistory does not absorb them.

## Slice 2 — new module, History.fs grew

Committed as “improving Undo” then “simplify slice 2” ([[implement-undo-slice-2.md]]).

- `ClientHistory.fs` was **added** (242 lines, then simplified to 85). It did not come out of `History.fs`.
- `History.fs` **gained** `Change.inverse` (HEAD vs baseline: +38 / −10). It did **not** lose confirmation-amendment or pending-lineage.
- Pending lineage / `confirm` lived only in the first ClientHistory draft and were **deleted there** (165 lines in “simplify slice 2”: `PendingTransition`, `appendPending`, `amendRecord`, `validateConfirmation`, `confirm`, …). Not moved into History, SyncLogic, or SyncPlanner.

Browser undo/redo in ClientHistory is a **new** stack over ordinary inverse Changes. It is not a relocation of `History.undo` / `History.redo`, which still applied graph mutations and recorded Server History.

## Slice 5 — deleted from History.fs, not moved

Working-tree diff vs HEAD ([[implement-undo-slice-5.md]]). Removed symbols, all gone from `src` with no counterpart file:

- `ChangeRequest` type and module (~31 lines)
- `History.addChange` (Emacs fold-future-into-past Server stack)
- `applyChange` no longer records Server History (now `Change.apply` only)
- `History.undo` / `History.redo` (Server undo/redo stacks)
- `applyAction` / `changedResult`
- `PersistStamp.applyToGraph`

`PersistStamp.opsBetween`, `appendToChange`, and `appendToLast` remain in `History.fs` and are still used by FileAgent/DbAgent.

`History` `{ past; future; nextId }` still exists as an empty field on `State`. The stack operations that filled it are gone.

## Not in SyncLogic / SyncPlanner

Those files changed in Slice 5, but they did **not** receive the deleted History.fs bodies.

- [[src/Shared/SyncLogic.fs]] **added** `AckReconcile` / `reconcileAck` (new ACK validation; ~+259). Undo/Redo callers use `ClientHistory.undo` / `ClientHistory.redo` already present from Slice 3b.
- [[src/Shared/SyncPlanner.fs]] **removed** `ackBatch`, `ackRequiresReload`, `applyAndEnqueueLocalAction`; added `retireSubmittedPrefix`.
- [[src/Shared/Serialization.fs]] / [[src/Shared/ViewModelSync.fs]] dropped `ChangeRequest` codecs/`fromRequest`/`toChangeRequest` — deleted, not moved.

Replacement for stamp ACK is suffix projection through ResidentProjection + `AckReconcile`, not `PersistStamp.applyToGraph` relocated.
