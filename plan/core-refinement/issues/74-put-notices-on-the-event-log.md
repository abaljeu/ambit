# 74 — Add the Notice event body

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

Split from [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md). Expand step. Today `CoreMailboxLoad.dispatchItem` adds each `Notice` to `context.noticeLog`, an in-memory list. That list is not the event log, so a poll does not return a Notice. `Notice` is in `src/Shared/MailboxLog.fs`. `EventBody` has no Notice case, and `EventJson` has no Notice encoding. This ticket adds the event form beside the existing ones. Nothing writes it yet. [76 — Log Notices on the event log](76-log-notices-on-the-event-log.md) switches the mailbox to it.

## What to build

No behaviour changes.

1. [ ] **Event body** — Add `EventBody.Notice of Notice`, beside the existing `EventBody` cases. Move `Notice` to where `EventBody` can use it. Every match on `EventBody` treats a Notice as no ops, no target, and no Actor change.
2. [ ] **JSON** — `EventJson` encodes and decodes each `Notice` case, built like the existing body encodings. Server and client both decode it.

### Acceptance Criteria

1. [ ] **Test** — `Notice JSON round trip`. Each `Notice` case encodes and decodes to the same value.
2. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket.

## See also

- [76 — Log Notices on the event log](76-log-notices-on-the-event-log.md)
- [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md)
- [Message shape](../cqrs-messages.md)
