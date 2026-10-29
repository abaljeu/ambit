# 105 — One Ev.withOps in History

**Status:** `coded`
**Note:** 2026-10-25 — Optional. Not needed to remove a type. Out of the type chain; no ticket waits on it.
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [102 — Place Parse changes as Order.Change](102-place-parse-changes-as-order-change.md)

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `Ev.ops`, `Ev.target`, and `Ev.isAction`

### Source

[ ] [PersistStamp.fs](src/Shared/PersistStamp.fs) `appendToEvent`
[ ] [CoreMailboxEvents.fs](src/Server/Core/CoreMailboxEvents.fs) `withAppliedOps`
[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `withConfirmedOps`

### Callers

[ ] `PersistStamp.appendToLastEvent` calls `appendToEvent`
[ ] `FileAgent` and `DbAgent` call `CoreMailboxEvents.withAppliedOps`
[ ] `CoreEventDispatch.pushChangeSpecials` and `CoreEventDispatch.postEvent` call `withConfirmedOps`

### Tests this ticket names

[ ] [EventTests.fs](tests/Shared.Tests/EventTests.fs) `Undo Redo carried Ops`

## 4. Context

### Three copies

[ ] `appendToEvent`, `withAppliedOps`, and `withConfirmedOps` each match `EventBody.Change`, `EventBody.Undo`, and `EventBody.Redo`
[ ] Each one puts a new Op list into the same body kind
[ ] Each one keeps the Undo or Redo target
[ ] Each one returns the Ev unchanged for the other bodies
[ ] `Ev.ops` is the one reader of the Op list

### Why

[ ] [108 — Log Change as EventBody.Order](108-log-change-as-eventbody-order.md) logs a Change as `EventBody.Order` of `Order.Change(ops, origin)`
[ ] A rebuild must keep `origin`
[ ] One rebuild function means the later tickets change one match

## 5. Changes

### Change

[x] One refactor: add `Ev.withOps`, and the three copies call it

### History.fs

[x] Add `Ev.withOps (event: Ev) (ops: Op list) : Ev` after `Ev.ops`
[x] `EventBody.Change _` gives `EventBody.Change ops`
[x] `EventBody.Undo(target, _)` gives `EventBody.Undo(target, ops)`
[x] `EventBody.Redo(target, _)` gives `EventBody.Redo(target, ops)`
[x] Every other body returns the Ev unchanged

### Callers

[x] `PersistStamp.appendToEvent` returns the Ev unchanged when `stampOps` is empty. Otherwise it calls `Ev.withOps event (appendToOps ops stampOps)` when `Ev.ops event` is `Some ops`, and returns the Ev unchanged when `Ev.ops event` is `None`
[x] `CoreMailboxEvents.withAppliedOps` is `Ev.withOps`
[x] `CoreEventDispatch.withConfirmedOps` calls `Ev.withOps event ops` when the confirmed ops are `Some ops`, and returns the Ev unchanged when they are `None`
[x] Behavior does not change

### Size

[x] 4 source files: `History.fs`, `PersistStamp.fs`, `CoreMailboxEvents.fs`, `CoreEventDispatch.fs`
[x] 1 test file: `EventTests.fs`

## 6. Tests

### withOps

[x] Add `Ev.withOps replaces the ops and keeps the body kind` in `EventTests.fs`
[x] A Change gets the new ops
[x] An Undo and a Redo keep their target and get the new ops
[x] An ActorStart and a Notice are unchanged
[x] Filter: `Ev.withOps replaces the ops and keeps the body kind`

### Kept

[x] Keep `Undo Redo carried Ops`
[x] Filter: `Undo Redo carried Ops`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
