# Tickets 09 and 10 — done, no code changes

Decided by Alan on 2026-10-29.

Tickets 09 — Poll with pending changes and 10 — Merge in progress are done. The existing code already does what both tickets need, so no code changes were made. Draft PR #485 has no source or test changes compared with staging.

## Ticket 10 rule

However the code handles errors, change nothing. However the code handles incoming changes, change nothing.

## Success-reply path, checked against the code on staging

- A success reply with a list of changes is accepted. It is never rejected just because more changes are pending.
- If the reply only echoes our own changes, `SyncLogic.reconcileAck` accepts it and applies any extra operations the server added. `SyncPlanner.retireSubmittedPrefix` retires the changes that were sent, and the rest of the pending changes post on the next send.
- If the reply also carries other people's changes, `SyncLogic.reconcileExternalAck` accepts it, notes a baseline with the pending edits undone, and retires the changes that were sent. The rest of the pending changes stay queued and post next.
- Rewind and replay of other people's changes happen only after the pending queue drains. `Update.finishAppliedSubmit` then starts a poll, and `SyncLogic.consumeCatchUpPoll` rewinds to the baseline, replays the server events, and re-applies any leftover pending edits.
- A reply that does not line up with what was sent, such as a missing, extra or reordered confirmation, leads to the existing reload message. That is error handling, and it stays as it is.
