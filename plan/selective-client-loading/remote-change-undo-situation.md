# Remote Change and first-client Undo

**Situation:** Client 2's Change reaches client 1 as a Poll ChangeLog tail, not as an ACK. A non-empty semantic tail **clears** Browser History, then projects the remote Change. History does not record or invert that Change. Client 1 cannot Undo client 2's edit, and cannot Undo its own earlier local edit after that Poll apply.

**Spec rule (item 12):** A non-empty semantic remote Poll or Load Change tail clears Browser History before projected application; an empty tail preserves History; do not match or rebase remote tails against Browser History. ([[undo-spec.md]])

The Server keeps no Undo state ([[server-history-after-undo.md]], spec item 1). ACK of client 1's own submits is History-neutral ([[undo-spec.md]] items 5–7). Those are different paths from Poll of someone else's Change.

## How client 1 receives client 2's Change

Idle Poll: `GET /ambit/poll?rev=` → `Api.getPoll` → `GetChangesSince` (ChangeLog after client Revision) → `PollDone` → `SyncLogic.applyServerTail` → `SyncLogic.applySyncResponse`.

`applySyncResponse` clears `ClientHistory` when `response.changes` is non-empty, then folds the tail through `ResidentProjection.applyChange`. It never calls `ClientHistory.record`. Empty `changes` leaves History as-is. Package-only Load (empty `changes`, non-empty packages) is the other preserve path (`applyLoadResponse`).

ACK of own work uses `SyncLogic.reconcileAck`: it projects `SetUpdateTime` suffixes and advances Revision; it does not clear or amend History.

## Cases

1. **Client 1 idle, no pending, empty History.** Non-empty Poll tail applies. History is already empty; clear is a no-op. Graph gets the remote Change. Undo after this: nothing (`ClientHistory.undo` / `SyncLogic.applyLocalUndo` return `None`).

2. **Client 1 idle, local History from its own already-ACKed Changes.** Same Poll. History **clears**. Own records are discarded. The remote Change is projected, not recorded. Undo after this: same as (1). Client 1 cannot Undo its earlier local Change. Client 1 cannot Undo client 2's Change.

3. **Client 1 has unacknowledged pending Changes (in flight or queued).** `SyncPlanner.tryStartPoll` does not Poll unless `Idle` and the pending queue is empty. `UpdateHelpers.isAutoSyncBlocked` also refuses Poll apply when pending is non-empty; `PollDone` then sets `DataOutdated` and does **not** apply the tail, so History **stays**. Spec item 12 is silent on this blocked-Poll case; it only describes a tail that is applied. If both clients POST, the second hits Server revision mismatch and spec item 11 (reject, reload). `StateLoaded` clears History. If client 1's ACK lands first, History stays through `reconcileAck`; the next idle Poll of client 2's Change is case 2 and then clears.

4. **Undo after (1) or (2).** History is empty. Undo does not invert the remote Change. Undo does not restore client 1's earlier local Change.

**Silent in spec:** rebasing client 1 History over a remote Change; treating a remote Poll Change as Undo-able; preserving own History across a non-empty applied tail. Conflict policy stays deferred ([[undo-spec.md]] Deferrals).
