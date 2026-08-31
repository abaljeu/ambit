# 02 — Append accepted Change to the log on retire

**Context:** Accepted edits leave `gambol-pending-v1` after submit retire. First paint on the next F5 must include those edits, so each accepted Change with `id` greater than snapshot Revision must go into the IndexedDB Change log. Do not rewrite the snapshot per edit. Do not persist ClientHistory.

**What to build:** After `retireSubmittedPrefix` / submit ack, append the accepted Change records to store `changes`. Pending queue still clears through `SavePendingQueue` as today. Shared helpers select Changes with `id > R` and keep log order.

**Blocked by:** [[01-persist-bootstrap-snapshot-after-state.md]]

**See also:** [[plan/client-start-time/reports/cache-first-boot-via-poll.md]], [[src/Shared/SyncPlanner.fs]], [[src/Shared/SyncLogic.fs]], [[plan/event-sourced-ops/overview.md]]

**Status:** ready-for-agent

- [x] On successful submit retire, Browser appends the accepted Change (server `id` / `changeId`) to IndexedDB store `changes` for the current `file`.
- [x] Pending queue still clears (`gambol-pending-v1`); retire path is otherwise unchanged.
- [x] Snapshot is not rewritten on each edit.
- [x] ClientHistory / Undo is not persisted.
- [x] Shared tests cover `changesAfter` (keep `id > R`, drop `id <= R`, sort by `id`) and that append input is the accepted Change list.
