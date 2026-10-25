# 120 — Ordinary edit posts NodeUnpersisted — write-ticket scope

Scope for [120 — Ordinary edit posts NodeUnpersisted](plan/core-refinement/issues/120-ordinary-edit-posts-nodeunpersisted.md). Blocked by [119 — Add Notice.NodeUnpersisted](plan/core-refinement/issues/119-add-notice-nodeunpersisted.md). One slice: after that expand, an ordinary Edit records today’s Unpersisted mark by posting `Notice.NodeUnpersisted` through `CoreMailboxApi.postNotice` from `CoreEventDispatch.postEvent`. This slice does not remove `Op.SetDocumentState` from the `Op` union, does not add a `SetDocumentState` message case, and does not change how Parsed comes from `Notice.NodeParsed`.

## Slice

[ ] Ordinary Edit means `Order.Change(_, ChangeOrigin.Edit)` placed through `CoreMailboxApi.postEvents`, which runs `CoreMailboxLoad.replyEdit` and then `CoreEventDispatch.postEvent` with `graphOnly = false`.

[ ] Today `CoreEventDispatch.pushChangeSpecials` (in `src/Server/Core/CoreEventDispatch.fs`) marks each owning special node Unpersisted at the stored Change event id and logs `Notice.NodePersisting` synchronously through `CoreEventDispatch.markForPersist` → `CoreEventDispatch.writeAxis` → `PersistHandlers.replaceGraph`, with no `CoreMailboxApi.postNotice` call.

[ ] After this slice, the same Edit still marks Unpersisted at the event id, and the event log also gets `EventBody.Notice (Notice.NodeUnpersisted nodeId)` via the notice queue (`CoreMailboxApi.postNotice` → `CoreQueue.Notice` → `CoreEventDispatch.logNotice` → `CoreMailboxLoad.applyNotice`).

### Implementer switch (Edit only, no `Op.SetDocumentState` work)

[ ] Wire a notice poster into `CoreEventDispatch.Context` the same way `CoreMailbox.hostPublishing` wires `CoreMailboxApi.postNotice` for `persist.bindPersistFinished` in `src/Server/Core/CoreMailbox.fs`.

[ ] At the success exit of `CoreEventDispatch.postEvent` / `persistNew` for an ordinary Edit (`EventBody.Change` from the Edit door, gated by `replyEdit` or by `Ev.commandName` such as `"Edit"`), enumerate owning specials with `DocumentOpImpact.owningSpecials` on the confirmed ops and `completed.id`, then call `postNotice (Notice.NodeUnpersisted nodeId)` for each id.

[ ] For that Edit path only, stop emitting synchronous `Notice.NodePersisting` from `markForPersist` (for example a flag on `pushChangeSpecials` or a branch that skips `markForPersist` when the caller is `replyEdit`), so Undo and Redo keep today’s `pushChangeSpecials` + `Notice.NodePersisting` behavior without double-logging on Edit.

[ ] Rely on [119 — Add Notice.NodeUnpersisted](plan/core-refinement/issues/119-add-notice-nodeunpersisted.md) for the `Notice` case, JSON, and `CoreMailboxLoad.applyNotice` handler that applies `GraphMutate.setUnpersistedAt` (same graph effect as today’s `markForPersist` mutate).

## Signatures

[ ] `type Notice` in `src/Shared/History.fs` — today includes `NodePersisting of nodeId: NodeId`; `NodeUnpersisted of nodeId: NodeId` arrives in [119 — Add Notice.NodeUnpersisted](plan/core-refinement/issues/119-add-notice-nodeunpersisted.md).

[ ] `type Ev = { id: EventId; submissionId: System.Guid; authority: Authority; commandName: string; body: EventBody }` in `src/Shared/History.fs`.

[ ] `type ChangeOrigin = | Edit | Parse` and `Order.Change of ops: Op list * origin: ChangeOrigin` in `src/Shared/MailboxLog.fs`.

[ ] `type CoreEventDispatch.Context = { persist: PersistHandlers; eventLog: EventLog ref; restartTip: EventId }` in `src/Server/Core/CoreEventDispatch.fs`.

[ ] `let postEvent (context: Context) (caller: Caller) (event: Ev) (graphOnly: bool) : Result<Ev * CoreChangesAccepted option, string>` in `src/Server/Core/CoreEventDispatch.fs`.

[ ] `let internal postNotice (host: MailboxHost) (msg: Notice) : unit` in `src/Server/Core/CoreMailboxApi.fs`.

[ ] `let logNotice (context: Context) (notice: Gambol.Shared.Notice) : Result<Ev, string>` in `src/Server/Core/CoreEventDispatch.fs`.

[ ] `let internal applyNotice (persist: PersistHandlers) (msg: Notice) : Result<unit, string>` in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] `let owningSpecials (graph: Graph) (ops: Op list) : NodeId list` in `src/Shared/DocumentOpImpact.fs`.

[ ] `let setUnpersistedAt (nodeId: NodeId) (version: EventId) (graph: Graph) : Graph` in `src/Shared/GraphMutate.fs`.

[ ] `replaceGraph: Notice -> Graph -> unit` on `PersistHandlers` in `src/Server/Core/CoreMsg.fs`.

## Match sites

### `Notice.NodePersisting` (today’s Edit mark path)

[ ] Case `NodePersisting of nodeId: NodeId` on `Notice` in `src/Shared/History.fs`.

[ ] `CoreEventDispatch.markForPersist` passes `(Gambol.Shared.Notice.NodePersisting nodeId)` into `CoreEventDispatch.writeAxis` in `src/Server/Core/CoreEventDispatch.fs`.

[ ] `CoreEventDispatch.pushChangeSpecials` matches `EventBody.Change`, `EventBody.Undo`, and `EventBody.Redo` and folds `markForPersist` over `DocumentOpImpact.owningSpecials` in `src/Server/Core/CoreEventDispatch.fs`.

[ ] `CoreMailboxLoad.applyNotice` matches `Notice.NodePersisting nodeId` and calls `CoreEventDispatch.writeAxis` with `writeUnpersisted` in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] `DbAgent.persistHandlers` `replaceGraph` persists the notice row in `src/Server/Core/DbAgent.fs`.

### `Notice.NodeUnpersisted` (after [119 — Add Notice.NodeUnpersisted](plan/core-refinement/issues/119-add-notice-nodeunpersisted.md))

[ ] New `Notice` case and `applyNotice` arm land in ticket 119, not in this slice’s source list alone.

### Edit → `postEvent`

[ ] `CoreMailboxLoad.replyEdit` builds `EventBody.Change` via `editEvent` and calls `CoreEventDispatch.postEvent` with `graphOnly = false` in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] `CoreMailboxLoad.placeOrder` matches `Order.Change (ops, ChangeOrigin.Edit)` to `replyEdit` in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] `CoreMailboxApi.postEvents` routes zero-id `EventBody.Change` to `Mailbox.PlaceOrder` in `src/Server/Core/CoreMailboxApi.fs`.

[ ] `CoreMailboxActors.dispatchPostEvent` calls `CoreEventDispatch.postEvent` with `graphOnly = false` for non-lifecycle bodies in `src/Server/Core/CoreMailboxActors.fs` (Undo, Redo, legacy Change — not the Edit door).

[ ] `CoreMailboxEvents.dispatchPostGraphOnly` calls `CoreEventDispatch.postEvent` with `graphOnly = true` in `src/Server/Core/CoreMailboxEvents.fs`.

### Notice queue

[ ] `CoreMailboxApi.postNotice` enqueues `CoreQueue.Notice msg` in `src/Server/Core/CoreMailboxApi.fs`.

[ ] `CoreMailboxLoad.dispatchItem` matches `CoreQueue.Notice msg`, runs `CoreEventDispatch.logNotice`, then `applyNotice` in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] `CoreMailbox.hostPublishing` sets `persist.bindPersistFinished (CoreMailboxApi.postNotice created)` in `src/Server/Core/CoreMailbox.fs`.

[ ] `CoreMailboxEvents.eventDispatchContext` builds `CoreEventDispatch.Context` from `MailboxContext` in `src/Server/Core/CoreMailboxEvents.fs` (today without a notice poster).

## Existing tests

[ ] ``An Edit pushes its owning special node`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` — `CoreMailboxApi.postEvents` with `editFileText`; asserts `pushPersist` received the file `NodeId`.

[ ] ``An Edit marks its owning special node at the Edit's id`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` — two Edits; asserts `PersistState.Unpersisted` and `changedVersion` equals the second ack `eventId`.

[ ] ``A graph-only change pushes nothing`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` — `CoreMailboxApi.postGraphOnly` with `editFileText`; asserts empty persist push (contrast with Edit door).

[ ] ``postEvents places Order.Change with origin Edit`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` — stored `EventBody.Change` matches `Order.Change(ops, ChangeOrigin.Edit)` in the mailbox log.

[ ] ``NodePersisting leaves the Edit's changedVersion`` in `tests/Server.Tests/CoreLoopNoticeTests.fs` — direct `applyNotice` with `Notice.NodePersisting`; asserts Unpersisted persist state and unchanged `changedVersion` (handler contract for today’s notice, not the Edit door).

[ ] ``NodePersisting matches the Unpersisted Set`` in `tests/Server.Tests/CoreLoopNoticeTests.fs` — `Notice.NodePersisting` graph matches `Graph.setPersistState` Unpersisted.

## Copy-from

[ ] `Api.postParseFile` in `src/Server/Api.fs` — after a successful graph-only Change, calls `CoreMailboxApi.postNotice handle.host (Notice.NodeParsed fileId)` (post Change, then post Notice through the mailbox door).

[ ] `Api.postChange` in `src/Server/Api.fs` — after `CoreDoor.postEvents`, calls `List.iter (CoreMailboxApi.postNotice handle.host)` on inferred node notices (batch post through the same helper).

[ ] `CoreEventDispatch.pushChangeSpecials` + `markForPersist` in `src/Server/Core/CoreEventDispatch.fs` — already computes owning specials and applies `GraphMutate.setUnpersistedAt` at the event id; reuse the same `owningSpecials` list and version when posting `Notice.NodeUnpersisted`.

## write-ticket

### Source files this slice would change

[ ] `src/Server/Core/CoreEventDispatch.fs` — post `Notice.NodeUnpersisted` after successful ordinary Edit in `postEvent`; optional Edit-only skip of synchronous `Notice.NodePersisting` in `pushChangeSpecials` / `markForPersist`; extend `Context` with a notice poster.

[ ] `src/Server/Core/CoreMailboxEvents.fs` — pass the notice poster into `eventDispatchContext` (or equivalent wiring from `MailboxContext`).

[ ] `src/Server/Core/CoreMailbox.fs` — bind `CoreMailboxApi.postNotice` into dispatch context after `MailboxHost.create`, alongside `bindPersistFinished`.

[ ] `src/Server/Core/CoreMailboxLoad.fs` — if needed, supply the poster when building context or distinguish Edit in the `replyEdit` → `postEvent` call chain.

### Tests this slice would add or change

[ ] Add ``ordinary edit posts NodeUnpersisted`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` — `CoreMailboxApi.postEvents` with `editFileText`; event log from `CoreMailboxApi.getEventsSince` or `eventHistory` contains `EventBody.Notice (Notice.NodeUnpersisted fileId)` after the Change; live graph shows Unpersisted at the Edit event id.

[ ] Extend ``An Edit marks its owning special node at the Edit's id`` or related door tests only if synchronous `Notice.NodePersisting` is removed from the Edit path and assertions must exclude duplicate notice rows.
