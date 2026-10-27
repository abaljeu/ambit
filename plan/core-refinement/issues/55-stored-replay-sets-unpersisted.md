# 55 — Undo, redo, and replay set persistState

**Status:** `coded`
One job: Undo, Redo, and replay from load leave `DocumentState.persistedVersion : EventId` and `DocumentState.parseState` unchanged, and set `DocumentState.persistState : PersistState` to `Unpersisted` when the Graph changes. They set `DocumentState.changedVersion : EventId` to the applied Event's `EventId`, including a replayed `Parse` command. Replay ignores persist Notices and parse Notices. The job comes from [Document-state events](../spec.md).

Type: bug-fixing
Blockers: none

## Inputs

### Working

[ ] [EventLog.fs](../../../src/Shared/EventLog.fs) `EventLog.recover` and `applyRecover` apply each Event ahead of the Graph through `Ev.apply`
[ ] [History.fs](../../../src/Shared/History.fs) `Ev.apply` passes `Op.persistMarkVersion` into the Op apply. `Ev.documentStateFromNotice` writes parse state for `NodeUploading`, `NodeReceived`, `NodeUnparsed`, and `NodeParsed`
[ ] [Model.fs](../../../src/Shared/Model.fs) `DocumentState.persistedVersion : EventId`, `DocumentState.changedVersion : EventId`, `DocumentState.persistState : PersistState`. `Node.withPersistState` writes `persistState` only. `Node.withUnpersistedAt` also sets `changedVersion` to `EventId.max` of the stored value and the argument. Both leave `persistedVersion`
[ ] [GraphMutate.fs](../../../src/Shared/GraphMutate.fs) `finishPersist` calls `setUnpersistedAt` when the `EventId` is present
[ ] [ChangeAmendment.fs](../../../src/Shared/ChangeAmendment.fs) `applyPosted` applies Undo and Redo Ops with `Op.persistMarkVersion`
[ ] [CoreEventDispatch.fs](../../../src/Server/Core/CoreEventDispatch.fs) `markForPersist` calls `GraphMutate.setUnpersistedAt` for Undo and Redo
[ ] [PersistApplyTests.fs](../../../tests/Server.Tests/PersistApplyTests.fs) `editThenAction` and `applyAction` post an `Ev` through `PersistHandlers.applyEvent`
[ ] [EventTests.fs](../../../tests/Shared.Tests/EventTests.fs) `EventLog.recover` fixtures

### Reference

[ ] [Document-state events](../spec.md)
[ ] [47 — Undo and redo set Unpersisted](47-undo-and-redo-set-unpersisted.md)
[ ] [fsharp-source](../../../.agents/rules/fsharp-source.md)

## Do not load

[ ] [BootCache.fs](../../../src/Shared/BootCache.fs)
[ ] [SyncLogic.fs](../../../src/Shared/SyncLogic.fs)
[ ] src/Client

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `EventLog.applyRecover`. A persist Notice or a parse Notice leaves the Graph. `eventId` still becomes that Event's `EventId`.
3. [ ] Persist Notices are `NodePersisting`, `NodePersisted`, `PersistFailed`, and `NodeUnpersisted`.
4. [ ] Parse Notices are `NodeParsing`, `NodeParsed`, `NodeUnparsed`, `NodeReceived`, and `NodeUploading`.
5. [ ] Undo, Redo, and a replayed Change keep the `Op.persistMarkVersion` apply. That apply writes `persistState` and `changedVersion` through `Node.withUnpersistedAt` and leaves `persistedVersion`.
6. [ ] A replayed `Parse` command that changes the Graph calls `GraphMutate.setUnpersistedAt` with that Event's `EventId`. That is `Node.withUnpersistedAt`. It sets `persistState` to `Unpersisted` and `changedVersion` to that `EventId`. It leaves `persistedVersion` and `parseState`. Live `Parse` stays on `Op.persistMarkVersion`, which gives `Parse` no `EventId`.

## Outputs

[ ] `EventLog.fs`
[ ] `PersistApplyTests.fs` ``Undo sets persistState Unpersisted and leaves persistedVersion``
[ ] `PersistApplyTests.fs` ``Redo sets persistState Unpersisted and leaves persistedVersion``
[ ] `EventTests.fs` ``Replay sets persistState Unpersisted and ignores persist Notices``
[ ] `EventTests.fs` ``Replay of Parse sets persistState Unpersisted and leaves persistedVersion``

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/55-stored-replay-sets-unpersisted-stop.md`, then ask.
[ ] The fix needs a file outside Inputs.
[ ] The fix contradicts this spec.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
[ ] `Server.Tests`: ``Undo sets persistState Unpersisted and leaves persistedVersion``
[ ] `Server.Tests`: ``Redo sets persistState Unpersisted and leaves persistedVersion``
[ ] `Shared.Tests`: ``Replay sets persistState Unpersisted and ignores persist Notices``
[ ] `Shared.Tests`: ``Replay of Parse sets persistState Unpersisted and leaves persistedVersion``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/55-stored-replay-sets-unpersisted-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
