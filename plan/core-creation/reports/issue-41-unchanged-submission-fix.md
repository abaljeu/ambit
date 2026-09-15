# Issue 41 — Unchanged-submission fix

Date: 2026-09-15. Ticket: [[../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]]. Diagnosis: [[issue-41-suite-failures.md]].

## Change

`CoreEventDispatch.persist` no longer drops `Event.ops = Some []` and no longer returns `Ok None` for an empty completed list. Empty-Ops Change Events and `postChange []` go through `persist.postChange`, so File/Db still reject with `Unchanged submission is rejected.` / `changes must not be empty` / closed-persist errors.

## Tests adjusted

Issue-41 and CoreMailbox door helpers that used `EventBody.Change []` only to exercise authority/history now post real Graph Ops, because empty Change is rejected again.
