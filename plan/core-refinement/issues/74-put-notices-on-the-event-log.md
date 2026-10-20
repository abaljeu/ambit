# 74 — Put Notices on the event log

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

Split from [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md). Today `CoreMailboxLoad.dispatchItem` adds each `Notice` to `context.noticeLog`, an in-memory list. That list is not the event log. A client poll does not return a Notice. `Notice` is in `src/Shared/MailboxLog.fs`. `EventBody` has no Notice case, and `EventJson` has no Notice encoding.

## What to build

No behaviour changes. Only where a Notice is recorded changes.

1. [ ] **Event body** — Add `EventBody.Notice of Notice`. Move `Notice` to where `EventBody` can use it. Every match on `EventBody` treats a Notice as no ops, no target, and no Actor change.
2. [ ] **JSON** — `EventJson` encodes and decodes each `Notice` case.
3. [ ] **Log** — `CoreMailboxLoad.dispatchItem` appends each Notice to the event log as an `EventBody.Notice` event, then applies it as today. `context.noticeLog` is deleted.

### Acceptance Criteria

1. [ ] **Test** — `Notice JSON round trip`. Each `Notice` case encodes and decodes to the same value.
2. [ ] **Test** — `Posted Notice is on poll`. After a Notice is posted, events-since returns it.
3. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete. Undo skips Notice events.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket.

## See also

- [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md)
- [75 — A failed write does not fail the edit](75-a-failed-write-does-not-fail-the-edit.md)
- [Message shape](../cqrs-messages.md)
