# 64 — Place Edit changes as Order.Change miss

Return to build for the Miss list in [64 — Place Edit changes as Order.Change review](plan/core-refinement/review/64-merge-postevent-and-postgraphonly-into-change.md).

## Outcome

[x] [64 — Place Edit changes as Order.Change](plan/core-refinement/issues/64-merge-postevent-and-postgraphonly-into-change.md) stays `coded`.
[x] Scope-report bare id, Non-zero Change id, and Non-zero id test are fixed.

## Scope-report bare id

[x] [64 — Place Edit changes as Order.Change scope](64-merge-postevent-and-postgraphonly-into-change-scope.md) line 84 names `102 — Place Parse changes as Order.Change`.
[x] Line 96 of that scope report names `102 — Place Parse changes as Order.Change`.

## Non-zero Change id

[x] `postedMailbox` in [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) places `EventBody.Change` at `EventId.zero` as `Mailbox.PlaceOrder` of `Order.Change` with origin `Edit`.
[x] A posted Change whose `id` is `EventId.Int` posts `CoreMsg.PostEvent` of the original Ev.
[x] `persistNew` returns `posted EventId must be zero` for that Ev.
[x] `editEvent` builds a new Edit at `EventId.zero`.
[x] `replyEdit` and `placeOrder` keep their argument lists.

## Non-zero id test

[x] One filter run of ``changes POST rejects non-zero EventId and admits zero`` raised `TimeoutException` at 2 s on a cold host.
[x] The next `scripts/agent-test.sh --filter "FullyQualifiedName~changes POST rejects non-zero EventId and admits zero" server` passed: 1 passed, 0 failed, duration 4 s.

## scripts/agent-test.sh

[x] `scripts/agent-test.sh` with no arguments passed.
[x] Server filter `FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~SessionTests`: 47 passed.
[x] Shared filter `FullyQualifiedName~MailboxLogTests`: 5 passed.

## Owners that stay

[x] ``POST duplicate submissionId with stale revision stays idempotent`` stays with [103 — Change reply is the event id](plan/core-refinement/issues/103-change-reply-is-the-event-id.md).
[x] Ticket section 6 stays as written for the user to confirm.
[x] Origin `Parse` stays with [102 — Place Parse changes as Order.Change](plan/core-refinement/issues/102-place-parse-changes-as-order-change.md).
[x] Undo, Redo, ActorStart, and Cancel in `postEvents` post `Mailbox.CoreMsg` of `CoreMsg.PostEvent`.
