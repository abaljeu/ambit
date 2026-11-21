# 20 — Apply own persist notices during a Poll
**Status**: coded
One job: The outliner who commits while a Poll is in flight sees that Poll apply `Notice.NodePersisting` and `Notice.NodePersisted` for the earlier change, sees the queued edits stay queued, and sees those edits submit. The story Poll with pending changes in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: bug-fixing
Blockers: none

## Scope

[x] `Update.applyPollDataOutdated` in `src/Client/Update.fs` calls `SyncLogic.applySyncResponse` when `pending` is non-empty, the event list is non-empty, and every body is `EventBody.Notice`.
[x] That path keeps `pending` in the same order and calls `SyncPlanner.tryStartSubmit`. The effects include `SubmitPendingBatch`.
[x] A non-empty `pending` queue with an empty event list, or with any body other than `EventBody.Notice`, sets `syncState` to `DataOutdated` and returns before `SyncLogic.applySyncResponse`.
[x] `App.runPollServer` in `src/Client/App.fs`. It calls `SyncLogic.getPollOutcome` with `model.eventId` and dispatches `PollDone`.
[x] `SyncLogic.getPollOutcome` in `src/Shared/SyncLogic.fs`. It returns `Some DataOutdated` when `poll.eventId` is greater than the Browser event id.
[x] `SyncLogic.applySyncResponse` in `src/Shared/SyncLogic.fs`. It applies the event tail on a `SyncResponse`.
[x] `SyncPlanner.tryStartSubmit` in `src/Shared/SyncPlanner.fs` returns the same queue and no `SubmitPendingBatch` when `pending` is non-empty and `syncState` is `Polling` or `DataOutdated`.
[x] With `pending` non-empty and `syncState` `Idle`, `tryStartSubmit` sets `Sending 1` and emits `SubmitPendingBatch`.
[x] `ClientHistory.mintChange` in `src/Shared/ClientHistory.fs` sets the new Change's `id` to `EventId.zero`.
[x] `SyncLogic.applySyncResponse` sorts the response events by event id before it applies them. The `pending` list is not part of that sort.
[x] `PersistThread.runRequest` in `src/Server/Core/PersistThread.fs` posts `Notice.NodePersisting` and then `Notice.NodePersisted` after the change is stored.
[x] `CoreEventDispatch.markForPersist` in `src/Server/Core/CoreEventDispatch.fs` logs `Notice.NodeUnpersisted` before that ack returns.
[x] `Api.postPoll` in `src/Server/Api.fs` sets `externalChanges` with `countsAsExternal`. A Notice is not external. `SyncLogic.changeSuccessToSync` omits `externalChanges`, so `PollDone` sees the event list.
[x] `Overlays.renderSyncRiskAlert` in `src/Client/Overlays.fs` titles `DataOutdated` `View is out of date`.
[x] The `#sync-status` click listener in `src/Client/App.fs` reloads the page when `syncState` is `DataOutdated`.
[x] An empty `pending` list does not take the first return in `applyPollDataOutdated`. A non-empty event list in that arm calls `applySyncResponse`.
[x] `SyncPlanner.enqueuePending` in `src/Shared/SyncPlanner.fs`. It appends one Event and calls `tryStartSubmit`.
[x] `UpdateHelpers.applyAndPost` in `src/Client/UpdateHelpers.fs`. It mints a Change with `ClientHistory.mintChange` and calls `enqueuePending`.
[x] `UpdateHelpers.splitNode` in `src/Client/UpdateHelpers.fs` and `UpdateMove.indentSelection` in `src/Client/UpdateMove.fs`. Enter and Tab reach the pending queue through that post path.
[x] `StatusView.renderSyncStatus` in `src/Client/StatusView.fs`. `DataOutdated` sets class `amb-stale` and the text `Data changed on server — click to reload`.
[x] `Notice.NodePersisting` and `Notice.NodePersisted` in `src/Shared/History.fs`.
[x] ``PollDone leaves Sync Idle when the edit box holds new text`` in `tests/Server.Tests/BrowserResidencyClientTests.fs`. That fact starts with an empty `pending` list.
[x] `enqueuePending` during `Polling`, with `eventId` `EventId.zero` and one Change already on `pending`, grows `pending` and emits no `SubmitPendingBatch`.
[x] `PollDone` of `Some DataOutdated` with that queue stays `DataOutdated` and leaves the queue unsent when the event list is empty or contains a body other than `EventBody.Notice`.
[x] [10 — Editing returnTo vs Poll/Load under edit+indent Tab CAS](plan/client-start-time/issues/10-edit-indent-old-text-mismatch.md) has status `ready-for-agent` on this tree.
[x] That client-start-time ticket stays open. This ticket leaves its file as it is.

## Inputs

### Working

[x] The Scope list is the file and symbol set for this ticket.
[x] `VmTestHelpers.emptyModel` in `tests/Shared.Tests/VmTestHelpers.fs`, `EventIdFixtures.storedId` in `tests/Shared.Tests/EventIdFixtures.fs`, and `ClientHistory.mintChange` in `src/Shared/ClientHistory.fs` are linked into `tests/Server.Tests`.
[x] `SyncLogic.changeSuccessToSync` builds the `SyncResponse` that `PollDone` applies.

### Reference

[x] [Stale on type and indent](plan/client-never-fails/reports/stale-on-type-and-indent.md) records the overlap. Outputs name the facts this ticket adds.
[x] [Size limit](plan/context/size-limit.md) caps a ticket at 8 source files and 4 tests. Outputs name one source file and two facts in one test file.

## Do not load

[x] `GraphMutate.setText` in `src/Shared/GraphMutate.fs`, `ChangeAmendment.isRecoverableCas` in `src/Shared/ChangeAmendment.fs`, and `CoreMailboxApi.postEvents` in `src/Server/Core/CoreMailboxApi.fs` stay as they are.
[x] `getPollOutcome` stays the event-id compare. `runPollServer` stays the caller that passes `model.eventId`. `tryStartSubmit` keeps its busy arm and its blocked arm.
[x] `enqueuePending`, `applyAndPost`, `splitNode`, and `indentSelection` stay as they are.
[x] `StatusView.renderSyncStatus` stays as it is. `CodeOutdated` and `ServerRejected` keep their sentences.
[x] `Overlays.renderSyncRiskAlert` and the `#sync-status` click listener stay as they are.
[x] `Api.postPoll`, `countsAsExternal`, `PersistThread.runRequest`, and `CoreEventDispatch.markForPersist` stay as they are.
[x] The client-start-time ticket in Scope stays open. Its file stays as it is.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change `applyPollDataOutdated` for a non-empty `pending` queue whose event list is non-empty and whose every body is `EventBody.Notice`. Call `SyncLogic.applySyncResponse` with that Poll `SyncResponse`. Keep `pending` as the same Events in the same order. The event list passed to `applySyncResponse` stays the Poll events. Copy the applied graph and the applied event id onto the model. Call `SyncPlanner.tryStartSubmit` with the Idle `SyncInfo` from the `PollDone` arm, that same `pending`, and the event id the apply stored. The effects include `SubmitPendingBatch`.
3. [x] A non-empty `pending` queue whose event list is empty, or whose list contains a body other than `EventBody.Notice`, stays `DataOutdated`. `pending` stays. The Browser event id stays. The effects include no `SubmitPendingBatch`.
4. [x] An `applySyncResponse` result of Error keeps `DataOutdated` and emits no `SubmitPendingBatch`. `pending` stays.
5. [x] The two facts named in Outputs pass.

## Outputs

[x] `src/Client/Update.fs` — `applyPollDataOutdated` applies the Notice tail, keeps `pending`, and the returned effects include `SubmitPendingBatch`.
[x] `tests/Server.Tests/BrowserResidencyClientTests.fs` — add ``own persist notices during a Poll submit the pending queue``.
[x] The fact builds a model with `syncState` `Polling`, `eventId` `EventId.zero`, and one `ClientHistory.mintChange` on `pending`.
[x] `SyncPlanner.enqueuePending` of a second Change leaves `syncState` at `Polling`, `pending` length 2, and no `SubmitPendingBatch`.
[x] The Poll events are `Notice.NodePersisting` and `Notice.NodePersisted` on a Node in that graph. `nodes` is empty and `childMap` is empty. The `NodePersisting` event id is the earlier id. The `NodePersisted` event id is the later id. That later id is ahead of `EventId.zero` and is the Poll tip.
[x] `SyncLogic.getPollOutcome` on that Poll is `Some DataOutdated`.
[x] `Update.update` of `PollDone` uses that outcome, `SyncLogic.changeSuccessToSync`, and the Poll event id, the same arguments `runPollServer` dispatches.
[x] After `PollDone`, `pending` is those same two Changes in that order, `syncState` is `Sending 1`, the effects include `SubmitPendingBatch` for that queue, the batch base event id is the `NodePersisted` event id, and the model `eventId` is that same id.
[x] Those `PollDone` asserts pass.
[x] `tests/Server.Tests/BrowserResidencyClientTests.fs` — add ``a foreign Change during a Poll stays DataOutdated``.
[x] That fact uses the same queue of two Changes. The Poll event list is one `EventBody.Change` whose event id is ahead of `EventId.zero`. Its op is `Op.SetText` whose old text is that Node's current text, so `applySyncResponse` would accept it. `nodes` is empty and `childMap` is empty.
[x] `SyncLogic.getPollOutcome` on that Poll is `Some DataOutdated`.
[x] After `PollDone`, `pending` length is 2, `syncState` is `DataOutdated`, the effects include no `SubmitPendingBatch`, and `eventId` is `EventId.zero`.
[x] That fact passes.

## Stop when

[ ] Rewrite `20-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit touches a file other than `src/Client/Update.fs` or `tests/Server.Tests/BrowserResidencyClientTests.fs`.
[ ] Stop when the edit changes `getPollOutcome`, `runPollServer`, `tryStartSubmit`, `enqueuePending`, or a server check named in Do not load.
[ ] Stop when the edit sets `DataOutdated` for a Notice tail that `applySyncResponse` accepts while `pending` is non-empty.
[ ] Stop when the edit clears `DataOutdated` for a tail that contains a body other than `EventBody.Notice`.
[ ] Stop when the edit appends `pending` to the response event list or replaces `pending` with a list sorted by event id.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``own persist notices during a Poll submit the pending queue``
  - `tests/Server.Tests`: ``a foreign Change during a Poll stays DataOutdated``
[x] All executed tests pass.
[x] Rewrite `20-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
