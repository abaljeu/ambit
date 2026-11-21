# 55 — Undo, redo, and replay set persistState scope

Server only. `DocumentState.changedVersion : EventId`. `DocumentState.persistedVersion : EventId`. `DocumentState.persistState : PersistState`.

## Signatures

[ ] `DocumentState.persistedVersion` is `EventId` in [Model.fs](../../../src/Shared/Model.fs)
[ ] `DocumentState.changedVersion` is `EventId` in `Model.fs`
[ ] `DocumentState.persistState` is `PersistState`, `Persisted` or `Unpersisted`
[ ] `EventId` is `Zero` or `Int of int` in [EventId.fs](../../../src/Shared/EventId.fs)
[ ] `Node.withUnpersistedAt` writes `persistState` `Unpersisted` and `changedVersion` as `EventId.max`. It leaves `persistedVersion`
[ ] `EventLog.applyRecover` in [EventLog.fs](../../../src/Shared/EventLog.fs) applies an Event through `Ev.apply`, then sets `eventId`

## implement

[ ] Undo and Redo already write `persistState` and `changedVersion` through `ChangeAmendment.applyPosted` and `CoreEventDispatch.markForPersist`
[ ] `Ev.apply` writes parse state for `NodeUploading`, `NodeReceived`, `NodeUnparsed`, and `NodeParsed`
[ ] Replay skips those parse Notices and the persist Notices `NodePersisting`, `NodePersisted`, `PersistFailed`, and `NodeUnpersisted`
