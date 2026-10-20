# 76 — Log Notices on the event log

**Status:** `defined`
**Type:** coding
**Blocked by:** [74 — Add the Notice event body](74-put-notices-on-the-event-log.md)
**Trackers:** architecture

## Context

Change-over step after [74 — Add the Notice event body](74-put-notices-on-the-event-log.md). Today `CoreMailboxLoad.dispatchItem` adds each `Notice` to `context.noticeLog`, and a poll does not return it. `CoreMailboxApi.postNotice` stays the private Notice door (Decision 15 **Notice has its own door** in [Message shape](../cqrs-messages.md)).

## What to build

1. [ ] **Log** — `CoreMailboxLoad.dispatchItem` appends each Notice to the event log as an `EventBody.Notice` event, then applies it as today.
2. [ ] **Remove** — Delete `context.noticeLog` and `MailboxHost.noticeLog`. Tests that read it read the event log.

### Acceptance Criteria

1. [ ] **Test** — `Posted Notice is on poll`. After a Notice is posted, events-since returns it.
2. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete. Undo skips Notice events. The client applies a poll that holds a Notice.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket.

## See also

- [74 — Add the Notice event body](74-put-notices-on-the-event-log.md)
- [75 — A failed write does not fail the edit](75-a-failed-write-does-not-fail-the-edit.md)
- [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md)
