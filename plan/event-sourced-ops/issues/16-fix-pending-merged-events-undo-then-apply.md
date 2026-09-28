# 16 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload

**Status:** defined
**Type:** bug-fixing
**Blocked by:** None — can start immediately

## Context

A person edits in the Browser while another Authority's Changes land on the Server. The Browser has posted Changes (pending in flight) and may still hold leftover queued Changes. The Server merges this Browser's posted Changes with the other Authority's work and returns merged Events on Poll.

Today the Browser does not rewind those posted Changes and adopt the merged Events. It enters DataOutdated and asks for a reload. That path is the defect that [04 — Client consumes merge success without reload](04-client-consumes-merge-success-without-reload.md) already closed for the empty-queue case. Leftover pending still blocks catch-up: `finishAppliedSubmit` starts a catch-up Poll only when pending is empty, and `tryStartPoll` refuses any Poll while pending is non-empty. `isAutoSyncBlocked` then treats leftover pending as a reason to take DataOutdated when Events are present.

Accepted consume in [Client correction — rewind and replay](plan/event-sourced-ops/details/client-consume.md) is: note the catch-up baseline from the Post signal, rewind optimistic posted Changes, replay the Server Event list, keep leftover pending unamended for the next Post. The queue-empty Poll gate is the present blocker; catch-up must run while leftover pending remains.

Do not attach or land this ticket on GitHub PR 145 (actor-as-client). That work is unrelated.

## What to build

When the Browser has posted Changes and leftover queued Changes, and Poll returns the Server's merged Events, the Browser rewinds the posted Changes, adopts those merged Events, and does not reload.

### 1. Start catch-up after merged ack

1. [ ] Merged-ack catch-up — `Update.finishAppliedSubmit` starts the catch-up Poll on a merged ack even when leftover queued Changes remain. Remove the `pending.IsEmpty` gate for that start.

### 2. Allow catch-up Poll with leftover pending

1. [ ] Catch-up Poll while pending — `SyncPlanner.tryStartPoll` allows a Poll when `catchUp.IsSome` and Idle, even when pending is non-empty.
2. [ ] Ordinary Poll stays gated — do not open an ordinary Poll while pending is non-empty.

### 3. PollDone without catch-up baseline (optional)

1. [ ] Build baseline if missing — if `PollDone` has Events and pending but no `catchUp`, build a baseline with `undoPendingGraph` and route to `consumeCatchUpPoll` instead of DataOutdated.

### 4. Soften auto-sync block

1. [ ] Pending is not DataOutdated — soften `isAutoSyncBlocked` so leftover pending alone does not force DataOutdated when Events are present.

### 5. Keep rewind out of the Poll door

1. [ ] No undo in tryStartPoll — do not put rewind (`undoPendingGraph`) inside `tryStartPoll`.

## See also

[146 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](https://github.com/abaljeu/ambit/issues/146), [04 — Client consumes merge success without reload](04-client-consumes-merge-success-without-reload.md), [Client correction — rewind and replay](plan/event-sourced-ops/details/client-consume.md), [tryStartPoll](src/Shared/SyncPlanner.fs), [finishAppliedSubmit and PollDone](src/Client/Update.fs), [undoPendingGraph, consumeCatchUpPoll, and reconcileExternalAck](src/Shared/SyncLogic.fs)

## Comments

- 2026-09-27 — Filed to mirror GitHub [146 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](https://github.com/abaljeu/ambit/issues/146). Status `defined`.
