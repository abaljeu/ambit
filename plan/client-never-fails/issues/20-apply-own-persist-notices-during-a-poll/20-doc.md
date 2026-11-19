# 20 — Apply own persist notices during a Poll

## 1. Current

[[../../../../doc/current/sync-mvp.md]]

[x] The Protocol claim names `Update.applyPollDataOutdated` in `src/Client/Update.fs`. A non-empty Notice-only tail with `pending` non-empty is applied, that queue stays, and `SyncPlanner.tryStartSubmit` submits it. A tail with any other body, or an empty tail, with that queue stays `DataOutdated`.

[[../../../../doc/current/http-contract.md]]

[x] Client flow Poll names that same apply rule for the `c` tail.

## 2. User

[x] `doc/user` is unchanged for this Poll apply.

## 3. Found elsewhere

[[../../map.md]]

[x] Problem item 3 — Pending changes during a Poll states that a tail with any other body stays `DataOutdated`, and that an empty tail with that queue stays `DataOutdated`.
[x] Solution item 3 — Protocol states that a Notice-only Poll tail while edits are pending is applied and the queue is submitted.

[[../../arch.md]]

[x] Path 10 Poll with pending changes states `SyncPlanner.tryStartPoll` emits `PollServer` only when `syncState` is `Idle` and `pending` is empty. That gate is [09 — Poll with pending changes](../09-poll-with-pending-changes/09-poll-with-pending-changes-ticket.md).
[x] Path 10 places this ticket in `Update.applyPollDataOutdated`.

[[../../project.md]]

[x] The index line for [20 — Apply own persist notices during a Poll](20-apply-own-persist-notices-during-a-poll-ticket.md) has Status `coded`.

[[../../../../src/Shared/SyncLogic.fs]]

[x] The comment on `SyncLogic.getPollOutcome` states `App.runPollServer` calls it with the Browser event id on a decoded Poll reply, and `App.runLoadServer` calls it on a decoded Load reply.

## 4. Stage

[x] Stage is doc in [20 — Apply own persist notices during a Poll](CONTEXT.md).
