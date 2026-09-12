# 28 — Drain Actor lifecycle on host stop

**Type:** task
**Status:** blocked
**Blocked by:** [[plan/core-creation/issues/27-prove-core-actor-lifecycle-with-testactor.md]]
Actual: 10m

## Context

Host shutdown is Actor lifecycle work, not Database availability policy. It follows the terminal and recovery model in [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Implement it after the current mailbox, cancellation, and shared-drop redo.

## What to build

On host StopAsync, refuse new Posts, drain the mailbox through [[18-finish-and-drop.md]], request cancellation without waiting, and exit when idle or the host default timeout fires. Restart Interrupted is [[18-finish-and-drop.md]].

- [ ] New Posts are refused after host stop begins.
- [ ] Already-enqueued Change and terminal messages drain in order.
- [ ] Running Actors receive non-blocking termination requests after durable terminal handling.
- [ ] Interrupted restart after an interrupted stop follows [[18-finish-and-drop.md]].
- [ ] Core does not define a separate shutdown timeout or crash-isolation design.

## See also

[[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]], [[plan/core-creation/issues/18-finish-and-drop.md]], [[plan/core-creation/issues/19-database-down-and-host-stop.md]]

## Time

- 2026-09-11 5m — split host-stop drain from Database-down persistence work (from chat)
- 2026-09-11 5m — drop restated finish path; keep unique host-stop drain (from chat)
