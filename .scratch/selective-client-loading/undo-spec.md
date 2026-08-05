# undo-spec

## History and Status

On 2026-08-04, implementation of [[.scratch/selective-client-loading/issues/15-introduce-history-action-messaging.md]] began from commit ec17f938731437cfb8ace4dd83e711ed1bc77c23. An early attempt interpreted the ticket and [[.scratch/selective-client-loading/spec.md]] as a broader synchronization redesign and was abandoned as `stash@{0}` (`WIP history action attempt 2026-08-04`). Do not apply that stash.

The corrective restart landed in commit `4255c48` (“implementing new undo plan ready for partially downloaded graph”). Ticket 15 is **agent-done**: `HistoryAction` is the pending-queue / submit / server-command unit; ChangeLog, Poll, bootstrap, and Load remain Change-only.

## Corrective Plan

This section remains the authoritative behavior for HistoryAction submission. It was implemented by `4255c48`; keep it as the decision record for later tickets (especially 18).

- `HistoryAction` is the client pending-queue, submit-payload, and server-command unit only. It has `Change`, `Undo`, and `Redo` cases; every case carries the existing revision id and `changeId`, and only `Change` carries operations.
- Keep the existing mixed `/changes` batch and atomic application path. The client applies Undo and Redo immediately with its local `History`, then submits the explicit action without waiting for the server.
- The server applies each action to canonical process-local `History`, materializes its graph effect as an ordinary `Change` carrying the action's same revision id and `changeId`, and persists that Change through the existing path.
- `ChangeLog`, Poll, bootstrap, and Load catch-up remain ordered `Change` paths. They do not store or return Undo or Redo actions.
- Applying a non-empty upstream Change tail clears local `History` and applies the tail without recording those upstream Changes in local `History`. Empty Polls and acknowledgements of the client's own submissions preserve local `History`.
- Server `History` starts empty on every process start. [[src/Server/Database.fs]] and [[src/Server/FileAgent.fs]] load authoritative graph state without replaying `ChangeLog`; the server start time becomes the Poll mismatch that puts an existing page into the stale-client refresh flow.
- Do not add a compatibility decoder or migration. Existing HistoryAction-specific durable codecs, bootstrap journals, catch-up paths, and replay work from the stopped attempt are not part of the delivered design.
- Preserve existing routes, response shapes, ChangeLog format, retry identity, revision chaining, optimistic conflict behavior, and single-flight synchronization except where the explicit decisions above require a narrow change.
