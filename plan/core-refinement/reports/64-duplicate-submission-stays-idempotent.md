# 64 — Duplicate submission stays idempotent

`POST duplicate submissionId with stale revision stays idempotent` passes.

## Root cause

[x] `postedMailbox` in [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) places a zero-id Change as `Mailbox.PlaceOrder` of `Order.Change` with origin `Edit`.
[x] `replyEdit` in [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) calls `CoreEventDispatch.postEvent`.
[x] `tryStored` in [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) returns the stored Ev for that `submissionId`.
[x] The PlaceOrder reply is that stored id.
[x] `fromPosted` in [CoreChanges.fs](src/Server/Core/CoreChanges.fs) sets `eventId` to that stored id.
[x] `postEvents` in [Api.fs](src/Server/Api.fs) calls `eventsThroughSnapshot` from that id through the snapshot tip.
[x] `acceptedChangeResult` appends that tail, so the ack lists the resubmitted Change and the later Change.

## Change

[x] `cursorAfterDuplicateChange` runs from `acceptedFromIds`.
[x] When the accepted id is stored and the log already holds a later Change, `eventId` becomes the log tip.
[x] The stored Ev list stays the resubmitted Change.
[x] The snapshot tail after that tip is empty, so the ack change list stays the resubmitted `submissionId`.
[x] An idle Cancel keeps `EventId.zero`.

## Tests

[x] The first filter run failed: expected the resubmitted `submissionId`, actual that id plus the later Change.
[x] The filter after the fix passed: 1 passed, 0 failed, duration 3 s.
[x] `scripts/agent-test.sh` passed Server.Tests 52 (`CoreMailboxDoorTests`, `MailboxExpandTests`, `SessionTests`) and Shared.Tests 5 (`MailboxLogTests`).
[x] That mapped set omits `StateEndpointTests`.
[x] `scripts/agent-test.sh --filter "FullyQualifiedName~POST duplicate submissionId with stale revision stays idempotent" server` then passed: 1 passed, 0 failed, duration 3 s.
