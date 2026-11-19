# 20 — Apply own persist notices during a Poll
**Status**: ready-for-agent
One job: The outliner who commits while a Poll is in flight sees that Poll apply `Notice.NodePersisting` and `Notice.NodePersisted` for the earlier change, sees the queued edits stay queued, and sees those edits submit. The story Poll with pending changes in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: bug-fixing
Blockers: none

## Scope

[x] `Update.applyPollDataOutdated` in `src/Client/Update.fs` sets `syncState` to `DataOutdated` when `pending` is non-empty, and returns no effects.
[x] That arm returns before `SyncLogic.applySyncResponse`.
[x] `App.runPollServer` in `src/Client/App.fs`. It calls `SyncLogic.getPollOutcome` with `model.eventId` and dispatches `PollDone`.
[x] `SyncLogic.getPollOutcome` in `src/Shared/SyncLogic.fs`. It returns `Some DataOutdated` when `poll.eventId` is greater than the Browser event id.
[x] `SyncLogic.applySyncResponse` in `src/Shared/SyncLogic.fs`. It applies the event tail on a `SyncResponse`.
[x] `SyncPlanner.tryStartSubmit` in `src/Shared/SyncPlanner.fs` returns the same queue and no `SubmitPendingBatch` when `pending` is non-empty and `syncState` is `Polling` or `DataOutdated`.
[x] With `pending` non-empty and `syncState` open, `tryStartSubmit` sets `Sending` and emits `SubmitPendingBatch`.
[x] `SyncPlanner.enqueuePending` in `src/Shared/SyncPlanner.fs`. It appends one Event and calls `tryStartSubmit`.
[x] `UpdateHelpers.applyAndPost` in `src/Client/UpdateHelpers.fs`. It mints a Change with `ClientHistory.mintChange` and calls `enqueuePending`.
[x] `UpdateHelpers.splitNode` in `src/Client/UpdateHelpers.fs` and `UpdateMove.indentSelection` in `src/Client/UpdateMove.fs`. Enter and Tab reach the pending queue through that post path.
[x] `StatusView.renderSyncStatus` in `src/Client/StatusView.fs`. `DataOutdated` sets class `amb-stale` and the text `Data changed on server — click to reload`.
[x] `Notice.NodePersisting` and `Notice.NodePersisted` in `src/Shared/History.fs`.
[x] ``PollDone leaves Sync Idle when the edit box holds new text`` in `tests/Server.Tests/BrowserResidencyClientTests.fs`. That fact starts with an empty `pending` list.
[x] `enqueuePending` during `Polling`, with `eventId` `EventId.zero` and one Change already on `pending`, grows `pending` and emits no `SubmitPendingBatch`.
[x] `PollDone` of `Some DataOutdated` with that queue leaves `syncState` at `DataOutdated` and leaves the queue unsent.
[x] [10 — Editing returnTo vs Poll/Load under edit+indent Tab CAS](plan/client-start-time/issues/10-edit-indent-old-text-mismatch.md) has status `ready-for-agent` on this tree.
[ ] That client-start-time ticket stays open. This ticket leaves its file as it is.

## Inputs

### Working

[x] The Scope list is the file and symbol set for this ticket.
[x] `VmTestHelpers.emptyModel` in `tests/Shared.Tests/VmTestHelpers.fs`, `EventIdFixtures.storedId` in `tests/Shared.Tests/EventIdFixtures.fs`, and `ClientHistory.mintChange` in `src/Shared/ClientHistory.fs` are linked into `tests/Server.Tests`.
[x] `SyncLogic.changeSuccessToSync` builds the `SyncResponse` that `PollDone` applies.

### Reference

[x] [Stale on type and indent](plan/client-never-fails/reports/stale-on-type-and-indent.md). The client sequence in that report is the fact this ticket adds.
[x] [Size limit](plan/context/size-limit.md) caps a ticket at 8 source files and 4 tests.

## Do not load

[ ] `GraphMutate.setText` in `src/Shared/GraphMutate.fs`, `ChangeAmendment.isRecoverableCas` in `src/Shared/ChangeAmendment.fs`, and `CoreMailboxApi.postEvents` in `src/Server/Core/CoreMailboxApi.fs` stay as they are.
[ ] `getPollOutcome` stays the event-id compare. `runPollServer` stays the caller that passes `model.eventId`. `tryStartSubmit` keeps its busy arm and its blocked arm.
[ ] `enqueuePending`, `applyAndPost`, `splitNode`, and `indentSelection` stay as they are.
[ ] `StatusView.renderSyncStatus` stays as it is. `CodeOutdated` and `ServerRejected` keep their sentences.
[ ] The client-start-time ticket in Scope stays open. Its file stays as it is.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `applyPollDataOutdated` so a non-empty `pending` queue and a Notice tail call `SyncLogic.applySyncResponse`, leave `pending` in place, and call `SyncPlanner.tryStartSubmit` so the effects include `SubmitPendingBatch`.
3. [ ] An `applySyncResponse` result of Error keeps `DataOutdated` and emits no `SubmitPendingBatch`.
4. [ ] Add the fact named in Outputs. On this tree that fact fails until step 2: `PollDone` leaves `syncState` at `DataOutdated`, leaves the queue unsent, and leaves the Browser event id unchanged.

## Outputs

[ ] `src/Client/Update.fs` — `applyPollDataOutdated` applies the Notice tail, keeps `pending`, and the returned effects include `SubmitPendingBatch`.
[ ] `tests/Server.Tests/BrowserResidencyClientTests.fs` — add ``own persist notices during a Poll submit the pending queue``.
[ ] The fact builds a model with `syncState` `Polling`, `eventId` `EventId.zero`, and one `ClientHistory.mintChange` on `pending`.
[ ] `SyncPlanner.enqueuePending` of a second Change leaves `syncState` at `Polling`, `pending` length 2, and no `SubmitPendingBatch`.
[ ] The Poll events are `Notice.NodePersisting` and `Notice.NodePersisted` on a Node in that graph. `NodePersisting` carries the earlier id. `NodePersisted` carries the later id. That later id is ahead of `EventId.zero` and is the Poll tip.
[ ] `SyncLogic.getPollOutcome` on that Poll is `Some DataOutdated`.
[ ] `Update.update` of `PollDone` uses that outcome, `SyncLogic.changeSuccessToSync`, and the Poll event id, the same arguments `runPollServer` dispatches.
[ ] After `PollDone`, `pending` length is 2, `syncState` is the state `tryStartSubmit` sets for an open queue, the effects include `SubmitPendingBatch` for that queue, and `eventId` is the persisted notice id.
[ ] On this tree the `PollDone` asserts fail. After step 2 they pass.

## Stop when

[ ] Rewrite `20-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit needs a file outside Scope.
[ ] Stop when the edit changes `getPollOutcome`, `runPollServer`, `tryStartSubmit`, `enqueuePending`, or a server check named in Do not load.
[ ] Stop when the edit sets `DataOutdated` for a Notice tail that `applySyncResponse` accepts while `pending` is non-empty.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``own persist notices during a Poll submit the pending queue``
[ ] All executed tests pass.
[ ] Rewrite `20-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
