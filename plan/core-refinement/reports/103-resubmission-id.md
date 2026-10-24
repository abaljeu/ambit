# 103 — Resubmission id

A post whose `submissionId` is already stored replies with that stored Ev's id. No amended flag is needed beside the id.

## What was checked

`CoreEventDispatch.postEvent` looks up `submissionId` on the mailbox EventLog.

- A hit returns `Ok(existing, None)`. The existing Ev is the stored record, including amended ops from the first apply.
- `CoreMailboxActors.dispatchPostEvent` replies that stored Ev. The id on the reply is `existing.id`.
- `FileAgent.applyOne` and `DbAgent.applyOneEvent` take the same stored-Ev path and do not set `externalChanges`.

## Conclusion

The resubmission reply is the stored Ev's id. `CoreMailboxApi` can rebuild the ack from that id. Item 2 of [103 — Change reply is the event id](../issues/103-change-reply-is-the-event-id.md) holds.
