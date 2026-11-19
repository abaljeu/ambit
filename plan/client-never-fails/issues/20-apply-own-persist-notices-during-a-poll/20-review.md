# 20 — Apply own persist notices during a Poll

Ticket: [[20-apply-own-persist-notices-during-a-poll-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

## 1. Standards

- `python3 scripts/standards-scan.py --diff origin/staging` exits 0.
- The scan prints three bindings inside the 40-line limit: `eventsAreNotices` 7, `keepDataOutdated` 5, `submitPendingAfterNotices` 21.
- The scan prints no long line, no glossary phrase, and no refer-by-name hit.
- No findings.

## 2. Spec

- The One job holds in `Update.applyPollDataOutdated`.
- A non-empty Notice tail, with `pending` non-empty, goes through `SyncLogic.applySyncResponse`. The model takes the applied graph and the applied event id. `pending` stays the same Events in the same order. `SyncPlanner.tryStartSubmit` receives the Idle `SyncInfo` from the `PollDone` arm and that event id. The state is `Sending 1`. The effects include `SubmitPendingBatch`.
- An empty event list with that queue stays `DataOutdated`. A tail that contains a body other than `EventBody.Notice` stays `DataOutdated`. An `applySyncResponse` result of `Error` stays `DataOutdated`. `pending` stays. `SubmitPendingBatch` stays out of the effects.
- No findings.

## 3. Arch

- This slice sits in `Update.applyPollDataOutdated` in `src/Client/Update.fs`. That file is the PollDone site in the module map.
- Path 10 still has `SyncPlanner.tryStartPoll` emit `PollServer` only for `Idle` and an empty `pending` queue. That gate is the Found elsewhere item for 09 — Poll with pending changes.
- No Blocker.

## 4. Tests

- ``own persist notices during a Poll submit the pending queue`` in `tests/Server.Tests/BrowserResidencyClientTests.fs` builds `Polling`, `EventId.zero`, and two `ClientHistory.mintChange` Events. The Poll is `Notice.NodePersisting` then `Notice.NodePersisted`. `nodes` is empty and `childMap` is empty. After `PollDone`, `pending` is those two Changes in that order, `syncState` is `Sending 1`, the batch base event id is the `Notice.NodePersisted` id, and the model `eventId` is that id.
- ``a foreign Change during a Poll stays DataOutdated`` uses that same queue. The Poll event is one `EventBody.Change` whose `Op.SetText` old text is the Node's current text. After `PollDone`, `pending` length is 2, `syncState` is `DataOutdated`, `SubmitPendingBatch` stays out of the effects, and `eventId` is `EventId.zero`.
- The notice fact fails when the pending arm always stays `DataOutdated`. Expected state is `Sending 1`. Actual state is `DataOutdated`. The foreign fact passes on that arm.
- The foreign fact fails when the pending arm applies every non-empty tail. Expected state is `DataOutdated`. Actual state is `Sending 1`. `SyncLogic.applySyncResponse` accepts that Change.
- `20-build.md` states this behavior and ends `Check: pass`.
- `bash scripts/check-test.sh 3827` prints `DONE` and `/tmp/ambit-full-suite-20261119-032547.log`. The log ends `full suite finished`. `AMBIT_FULL_SUITE=1`. `tests/Server.Tests` passed 777, skipped 1, failed 0, total 778. `tests/Shared.Tests` passed 1946, skipped 3, failed 0, total 1949.

## 5. Miss

- No findings.

## 6. Found elsewhere

The following issues were found beyond the ticket scope.  The fixes are suggested.

- **Problem.** [09 — Poll with pending changes](../09-poll-with-pending-changes/09-poll-with-pending-changes-ticket.md) is marked done. `SyncPlanner.tryStartPoll` still emits `PollServer` only for `Idle` and an empty `pending` queue.
- **Fix.** Leave that gate. This ticket does not take it.
- Accepted.
- **Problem.** `LoadDone` in `src/Client/Update.fs` sets `DataOutdated` when `isAutoSyncBlocked` is true. A commit during `Loading` takes that arm and leaves the reply unapplied.
- **Fix.** Leave that arm for [13 — Edit during a Load](../13-edit-during-load/13-edit-during-load-ticket.md).
- Accepted.
- **Problem.** The comment on `SyncLogic.getPollOutcome` says callers invoke it only when no local events are pending. `App.runPollServer` passes `model.eventId` on every Poll reply.
- **Fix.** Leave the compare. This ticket reads the result in `applyPollDataOutdated`.
- Accepted.
