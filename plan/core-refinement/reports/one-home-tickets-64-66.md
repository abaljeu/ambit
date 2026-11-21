# One home for tickets 64, 65, and 66

Branch: `dev`.

## Files

- [64 — Merge PostEvent and PostGraphOnly into Change](../issues/64-merge-postevent-and-postgraphonly-into-change.md)
- [65 — Log Undo and Redo as the target](../issues/65-log-undo-and-redo-as-the-target.md)
- [66 — Move Actor orders onto Order and Notice](../issues/66-move-actor-orders-onto-order-and-notice.md)

## Homes

### 64 — Merge PostEvent and PostGraphOnly into Change

- Context keeps the four body names: Undo, Redo, ActorStart, and Cancel.
- What to build item 1 Door keeps the decision that `CoreMsg.PostEvent` stays for those bodies.
- Acceptance Criteria item 4 points at the four bodies Context names. The assertion is that a post of each body still uses `CoreMsg.PostEvent`.

### 65 — Log Undo and Redo as the target

- Context: `EventBody.Undo` and `EventBody.Redo` store an `EventId` and an Op list. `dispatchPostEvent` sends them through `postEvent`. `ClientHistory.invertAs` and `CoreEventDispatch` fill that list. 64 leaves those bodies on `CoreMsg.PostEvent`.
- What to build item 1 Order: place `Order.Undo` or `Order.Redo` of the target `EventId`, and the `PlaceOrder` reply.
- What to build item 2 Log: Decision 1 Logged as placed. The stored body has no Op list. Storage until 68, Accept, `EventJson`, the old decoder, and `MailboxLog`.
- What to build item 3 Resolve: inverse, forward Op list, the same client and server resolution, and `tryPeekUndoName`.
- What to build item 4 Old events: a stored Undo or Redo that still carries an Op list applies that list.
- Acceptance Criteria: the stored target and the graph for a new Undo and a new Redo, one shared Op list, an old Undo applies its list, and the still-working flows.

### 66 — Move Actor orders onto Order and Notice

- Context: the seven `CoreMsg` cases, the pool start, the search start, `searchDoor`, the stored search stop, client `PostEvent` of `ActorStart` and `Cancel`, and 60 moving `PeerActorName` onto Shared.
- What to build: StartActor, StartPeerActor with Decision 13 PeerActorName, LoadSave, Cancel, ActorStop, and Remove.
- Acceptance Criteria: `pool.startActor` and the log, the peer and load-save orders, the cancel reply, `Notice.ActorStop` and the removed cases, and the still-working flows.
