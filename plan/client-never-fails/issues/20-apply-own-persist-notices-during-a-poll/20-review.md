# 20 — Apply own persist notices during a Poll

Ticket: [[20-apply-own-persist-notices-during-a-poll-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

## 1. Standards

- No findings.
- `python3 scripts/standards-scan.py --diff origin/staging` printed no lines and exited 0.

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

- The two facts in Outputs are not in the tree. Status is `ready-for-agent`. Build has not started, so those facts were not executed.
- `PersistThread.runRequest` posts `Notice.NodePersisting` and then `Notice.NodePersisted` after the ack. `CoreEventDispatch.markForPersist` logs `Notice.NodeUnpersisted` before that ack returns.
- `App.runPollServer` calls `SyncLogic.getPollOutcome` with `model.eventId`. A greater Poll event id returns `Some DataOutdated`.
- `Update.applyPollDataOutdated` sets `DataOutdated` and returns no effects while `pending` is non-empty. That is the notice fact's failing `PollDone` asserts. `StatusView.renderSyncStatus` paints class `amb-stale` for that state.
- An empty `pending` list does not take that return. Keystrokes leave the queue empty. Enter is `UpdateHelpers.splitNode`. Tab is `UpdateMove.indentSelection`. Both reach the queue through `UpdateHelpers.applyAndPost`.
- `ServerRejected` and `CodeOutdated` use class `amb-stale` and different sentences. `Overlays.renderSyncRiskAlert` titles `DataOutdated` `View is out of date`. The `#sync-status` click listener reloads the page for `DataOutdated`.
- A pending Change from `ClientHistory.mintChange` has `EventId.zero`. `applySyncResponse` sorts the response events by event id. The ticket keeps `pending` off that list.
- `SyncLogic.changeSuccessToSync` drops `externalChanges`. A tail that contains a body other than `EventBody.Notice` stays `DataOutdated` on this tree. The foreign Change fact is not in the tree, so it was not executed. The current arm matches its asserts.
- The Output facts use empty `nodes` and empty `childMap`. `Api.postPoll` fills both. `applySyncResponse` then rewinds `pending` on the graph and leaves the `pending` list in place. The ticket text says the submit uses that same list.
- ``PollDone leaves Sync Idle when the edit box holds new text`` starts from an empty `pending` list. It does not cover this overlap.
- `plan/client-never-fails/project.md` lists 20 — Apply own persist notices during a Poll. That number is the next number after 19 — Want third child rank and cap. No second ticket 20 sits under this Project.
- Outputs name `src/Client/Update.fs` and two facts in `tests/Server.Tests/BrowserResidencyClientTests.fs`. That is inside the cap of 8 source files and 4 tests.

## 5. Miss

- No findings.

## 6. Found elsewhere

- The PR body has no Found elsewhere list. The three items below sit outside this ticket.
- **Problem.** [09 — Poll with pending changes](plan/client-never-fails/issues/09-poll-with-pending-changes/09-poll-with-pending-changes-ticket.md) is marked done. `SyncPlanner.tryStartPoll` still emits `PollServer` only for `Idle` and an empty `pending` queue. Path 10 in the arch still names that emit.
- **Fix.** Leave that gate. This ticket does not take it.
- **Problem.** `LoadDone` sets `DataOutdated` when `isAutoSyncBlocked` is true. A commit during `Loading` takes that arm and does not apply the reply.
- **Fix.** Leave that arm for [13 — Edit during a Load](plan/client-never-fails/issues/13-edit-during-load/13-edit-during-load-ticket.md).
- **Problem.** The comment on `SyncLogic.getPollOutcome` says callers invoke it only when no local events are pending. `App.runPollServer` passes `model.eventId` on every Poll reply.
- **Fix.** Leave the compare. This ticket reads the result in `applyPollDataOutdated`.
