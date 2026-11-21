# 20 — Apply own persist notices during a Poll

Ticket: [20 — Apply own persist notices during a Poll](20-apply-own-persist-notices-during-a-poll-ticket.md)

Spec: [Client never fails on server data](../../spec.md)

Arch: [Client never fails on server data architecture](../../arch.md)

Review: [20 — Apply own persist notices during a Poll](20-review.md)

## 1. keepDataOutdated

[x] `keepDataOutdated` in `src/Client/Update.fs` sets `syncState` to `DataOutdated` and returns an empty effect list.
[x] The empty-`pending` `Error` arm of `applyPollDataOutdated` calls `keepDataOutdated`.
[x] That arm returns the same model and the same empty effect list as the pending arms that call `keepDataOutdated`.
[x] ``own persist notices during a Poll submit the pending queue`` and ``a foreign Change during a Poll stays DataOutdated`` in `tests/Server.Tests/BrowserResidencyClientTests.fs` stay as written.
