# Undo and Redo skip the version write

Ticket: [98 — Core marks carry the Edit's id](../issues/98-core-marks-carry-the-edits-id.md)
Spec: [spec §2.6 Undo, Redo, and stored replay](../spec.md)
Related: [47 — Undo and redo set Unpersisted](../issues/47-undo-and-redo-set-unpersisted.md)
Review: [98 — Core marks carry the Edit's id](../review/98-core-marks-carry-the-edits-id.md)

## 1. What changed

`ChangeAmendment.applyPosted` is the one apply both agents call. `FileAgent.applyOne` and `DbAgent.applyOneEvent` pass `graphOnly`, `event.id`, the event, the ops, and the state.

The version choice reads `EventBody`, not `commandName`.

- `EventBody.Undo` and `EventBody.Redo` do not call `Op.persistMarkVersion`. Apply runs with no version. After a real graph change, the owning special nodes are set `Unpersisted` through `GraphMutate.setPersistState`. `changedVersion` stays as it was.
- Every other body still calls `Op.persistMarkVersion`. A Change named `Edit` still stamps `event.id`. A Change named `Parse` still writes no persist state.

`DocumentOpImpact.fs` now compiles before `ChangeAmendment.fs` so `applyPosted` can call `DocumentOpImpact.owningSpecials`.

## 2. Files

- `src/Shared/ChangeAmendment.fs` — `applyPosted`
- `src/Shared/Gambol.Shared.fsproj` — `DocumentOpImpact.fs` before `ChangeAmendment.fs`
- `src/Server/Core/FileAgent.fs` — `applyOne` calls `applyPosted`
- `src/Server/Core/DbAgent.fs` — `applyOneEvent` calls `applyPosted`
- `tests/Server.Tests/PersistApplyTests.fs` — `Undo and Redo leave Unpersisted and keep changedVersion`

## 3. Tests

Red, before the source change: `Undo and Redo leave Unpersisted and keep changedVersion` failed. Expected `changedVersion` Int 4. Actual Int 9, the Undo event id. Text had already changed and persist state was already `Unpersisted`. `commandName` on that event is `Edit`.

Green, `scripts/agent-test.sh --filter` on Server.Tests. Passed 7, failed 0.

- `Undo and Redo leave Unpersisted and keep changedVersion`
- `FileAgent applyEvent marks a File Node at the later Edit id`
- `FileAgent applyEvent applies Ev Ops without leftover Change`
- `DbAgent applyEvent applies Ev Ops without leftover Change`
- `CoreEventDispatch persist apply does not copy Ev to leftover Change`
- `An Edit marks its owning special node at the Edit's id`
- `An Edit pushes its owning special node`

The new test runs Undo and Redo on FileAgent and DbAgent, on the command path and the graph-only path. Each event's `commandName` is `Edit`.

## 4. SageFs

Daemon `0.6.919` and session core `0.6.919.0` match. The repo has no newer pin.

Session `e6f1f6a2` (Server.Tests, this worktree). Reused. Not created here. Not stopped.

`scripts/sagefs-hard-reset.sh e6f1f6a2 true` rebuilt it so the REPL matched disk. Proof on a File node shaped like the agents' input:

- Undo and Redo versions are `None`
- a Change named `Edit` is `Some` event id 9
- a Change named `Parse` is `None`
- `GraphMutate.setText` with no version, then `GraphMutate.setPersistState` `Unpersisted`, left `changedVersion` 4 and text `hello`

## 5. Remaining

These callers still call `Op.persistMarkVersion` with `commandName`. An Undo or Redo through them still stamps `changedVersion` at that event id.

- `Ev.apply` in `src/Shared/History.fs`
- `SyncLogic` projected apply in `src/Shared/SyncLogic.fs`
- `BootCache.foldLog` in `src/Shared/BootCache.fs`

`markUnpersistedKeepingVersion` keeps the graph when `GraphMutate.setPersistState` returns an error. A File node that carries the axes succeeds.

Undo and Redo leave `changedVersion` where it was. A later `NodePersisted` whose written version equals that `changedVersion` still counts as current under [100 — Core loop gates Persisted on the written version](../issues/100-core-loop-gates-persisted-on-the-written-version.md). Spec §2.6 **Undo, Redo, and stored replay** says an older written version leaves the node `Unpersisted`.

No commit. Branch `dev`.
