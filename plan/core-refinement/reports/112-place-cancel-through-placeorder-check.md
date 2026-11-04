pass

## Counts

- Server.Tests: 742 passed, 0 failed, 1 skipped
- Shared.Tests: 1931 passed, 0 failed, 3 skipped

## Commands

- `scripts/agent-test.sh` round: Server.Tests `CancelByFocusTests|MailboxExpandTests`, 16 passed, 0 failed, 0 skipped; Shared.Tests `EventJsonTests|HistoryTests|MailboxLogTests`, 85 passed, 0 failed, 0 skipped
- `scripts/agent-test.sh --filter "FullyQualifiedName~cancelByFocus places Order.Cancel" server` round: 1 passed, 0 failed, 0 skipped
- `scripts/agent-test.sh --filter "FullyQualifiedName~second Cancel for the same run logs no stop" server` round: 1 passed, 0 failed, 0 skipped
- `scripts/agent-test.sh --filter "FullyQualifiedName~CancelByFocusTests" server` round: 9 passed, 0 failed, 0 skipped
- `scripts/agent-test.sh --filter "FullyQualifiedName~Mailbox is CoreMsg, PlaceOrder, and Ask" server` round: 1 passed, 0 failed, 0 skipped
- `scripts/agent-test.sh --full` round: Server.Tests 742 passed, 0 failed, 1 skipped; Shared.Tests 1931 passed, 0 failed, 3 skipped

## Reply

- Every Cancel reply is `Ok EventId.zero`.
- A live run logs `Notice.ActorStopped` of the focus and `ActorCancelled`, then finishes the pool row.
- A second Cancel, and a focus with no live run, reply `Ok EventId.zero` and add no log entry.

## Files

- 8 source files: `CoreMsg.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoop.fs`, `CoreMailboxActors.fs`, `CoreMailboxEvents.fs`, `History.fs`, `EventJson.fs`, `MailboxLog.fs`
- 3 test files: `CancelByFocusTests.fs`, `MailboxExpandTests.fs`, `EventJsonTests.fs`
- Rename files: `History.fs`, `EventJson.fs`, `MailboxLog.fs`, `CoreMailboxLoop.fs`, `EventJsonTests.fs`

## Ticket difference

- Process step 4 of [112 — Place Cancel through PlaceOrder](plan/core-refinement/issues/112-place-cancel-through-placeorder.md) asks for the stored stop's id.
- A focus with no live run has no stored stop.
- Alan's addition requires that reply to match a successful Cancel, so the reply is `EventId.zero`.
- The wire kind stays `cancel`. `EventBody.Cancel` keeps its name. `Order.Cancel` is now `Order.CancelActor`.
