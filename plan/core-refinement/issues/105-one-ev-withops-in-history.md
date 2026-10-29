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

[ ] [History.fs](src/Shared/History.fs) owns `Ev.ops`, `Ev.withOps`, `Ev.target`, and `Ev.isAction`

### Source

[ ] [PersistStamp.fs](src/Shared/PersistStamp.fs) `appendToEvent`
[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `withConfirmedOps`
[ ] [FileAgent.fs](src/Server/Core/FileAgent.fs) `applyOne`
[ ] [DbAgent.fs](src/Server/Core/DbAgent.fs) `applyOneEvent`

### Callers

[ ] `PersistStamp.appendToLastEvent` calls `appendToEvent`
[ ] `FileAgent.applyOne` and `DbAgent.applyOneEvent` call `Ev.withOps`
[ ] `CoreEventDispatch.pushChangeSpecials` calls `withConfirmedOps`
[ ] `CoreEventDispatch.store` calls `withConfirmedOps`
[ ] `CoreEventDispatch.postEvent` calls `persistNew`, which calls `store`

### Tests this ticket names

[ ] [EventTests.fs](tests/Shared.Tests/EventTests.fs) `Ev.withOps replaces the ops and keeps the body kind`
[ ] `Undo Redo carried Ops` in that same file

## 4. Context

### One rebuild

[ ] `Ev.withOps` writes a new Op list onto `EventBody.Change`, `EventBody.Undo`, and `EventBody.Redo`
[ ] `Ev.withOps` keeps the Undo or Redo target
[ ] `Ev.withOps` returns the same Ev for `EventBody.ActorStart`, `EventBody.Cancel`, `EventBody.Load`, `EventBody.ActorStop`, and `EventBody.Notice`
[ ] `Ev.ops` reads that Op list
[ ] `Ev.ops` and `Ev.withOps` each match those bodies

### Why

[ ] [108 — Log Change as EventBody.Order](108-log-change-as-eventbody-order.md) logs a Change as `EventBody.Order` of `Order.Change(ops, origin)`
[ ] A rebuild must keep `origin`
[ ] One rebuild function means the later tickets change one match

## 5. Changes

### History.fs

[x] `Ev.withOps (event: Ev) (ops: Op list) : Ev` sits after `Ev.ops`
[x] `EventBody.Change _` gives `EventBody.Change ops`
[x] `EventBody.Undo(target, _)` gives `EventBody.Undo(target, ops)`
[x] `EventBody.Redo(target, _)` gives `EventBody.Redo(target, ops)`
[x] Every other body returns the same Ev

### Callers

[x] `PersistStamp.appendToEvent` returns the same Ev when `stampOps` is empty
[x] Otherwise `appendToEvent` calls `Ev.withOps event (appendToOps ops stampOps)` when `Ev.ops event` is `Some ops`
[x] Otherwise `appendToEvent` returns the same Ev when `Ev.ops event` is `None`
[x] `FileAgent.applyOne` calls `Ev.withOps event appliedOps` when apply returns `ApplyResult.Changed`
[x] `DbAgent.applyOneEvent` calls `Ev.withOps event appliedOps` when apply returns `ApplyResult.Changed`
[x] `CoreEventDispatch.withConfirmedOps` calls `Ev.withOps event ops` when the confirmed ops are `Some ops`
[x] `withConfirmedOps` returns the same Ev when the confirmed ops are `None`

### Size

[x] 6 source files: `History.fs`, `PersistStamp.fs`, `CoreMailboxEvents.fs`, `CoreEventDispatch.fs`, `FileAgent.fs`, `DbAgent.fs`
[x] 1 test file: `EventTests.fs`

## 6. Tests

### withOps

[x] `Ev.withOps replaces the ops and keeps the body kind` is in `EventTests.fs`
[x] A Change gets the new ops
[x] An Undo and a Redo keep their target and get the new ops
[x] An ActorStart and a Notice stay the same Ev
[x] Filter: `Ev.withOps replaces the ops and keeps the body kind`

### Kept

[x] `Undo Redo carried Ops` stays
[x] Filter: `Undo Redo carried Ops`

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
