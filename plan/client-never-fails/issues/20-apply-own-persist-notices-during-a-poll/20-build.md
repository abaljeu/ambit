# 20 — Apply own persist notices during a Poll — build

`Update.applyPollDataOutdated` in `src/Client/Update.fs` applies a non-empty Poll tail when `pending` is non-empty and every body is `EventBody.Notice`. The apply is `SyncLogic.applySyncResponse` on that Poll `SyncResponse`. The model takes the applied graph and the applied event id. `pending` stays the same Events in the same order. `SyncPlanner.tryStartSubmit` receives the Idle `SyncInfo` from the `PollDone` arm, that `pending`, and the event id the apply stored. The returned state is `Sending 1`. The effects include `SubmitPendingBatch`.

A non-empty `pending` queue with an empty event list stays `DataOutdated`. A non-empty `pending` queue whose tail contains a body other than `EventBody.Notice` stays `DataOutdated`. `pending` stays. The Browser event id stays. `SubmitPendingBatch` stays out of the effects.

An `applySyncResponse` result of `Error` keeps `DataOutdated`. `pending` stays. `SubmitPendingBatch` stays out of the effects.

## Tests

[ ] ``own persist notices during a Poll submit the pending queue`` in `tests/Server.Tests/BrowserResidencyClientTests.fs` — `pending` is the same two Changes in that order, `syncState` is `Sending 1`, the batch base event id is the `Notice.NodePersisted` event id, and the model `eventId` is that id.
[ ] ``a foreign Change during a Poll stays DataOutdated`` in `tests/Server.Tests/BrowserResidencyClientTests.fs` — `pending` length is 2, `syncState` is `DataOutdated`, `SubmitPendingBatch` stays out of the effects, and `eventId` is `EventId.zero`.

## Check

[ ] `scripts/agent-test.sh --filter` for the two named facts in `tests/Server.Tests`: Passed 2, Failed 0
[ ] `scripts/agent-test.sh` with no arguments: Passed 4, Failed 0, Total 4, `tests/Server.Tests` `BrowserResidencyClientTests`
[ ] Full Server suite, `AMBIT_FULL_SUITE=1`: Passed 777, Failed 0, Skipped 1, Total 778
[ ] Full Shared suite, `AMBIT_FULL_SUITE=1`: Passed 1946, Failed 0, Skipped 3, Total 1949
[ ] Server log: `/tmp/server-full.log`
[ ] Shared log: `/tmp/shared-full.log`

Check: pass
