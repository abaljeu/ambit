# undo-spec

## History and Status

On 2026-08-04, implementation of [[.scratch/selective-client-loading/issues/15-introduce-history-action-messaging.md]] began from commit ec17f938731437cfb8ace4dd83e711ed1bc77c23. The intended functional change was narrow: the client would continue to apply Undo and Redo optimistically with the existing History algorithm, but would submit an explicit Undo or Redo instead of an inverted or reissued Change; the server would apply the same History algorithm against canonical state, persist the explicit action, and return it unchanged through synchronization and replay.

The attempt interpreted the ticket and [[.scratch/selective-client-loading/spec.md]] as a broader synchronization redesign. It modified 28 source and test files across Shared, Client, Server, file persistence, PostgreSQL persistence, bootstrap, Poll revision validation, retry handling, and compatibility paths. Some of that cross-layer work is required because Change was the queue, wire, server-command, and durable-record unit, but bootstrap action journals, retry-policy changes, terminology churn, and adjacent synchronization repairs exceeded the intended functional scope.

At the last stable checkpoint, 129 focused Shared tests and 7 focused Server tests passed, the solution built, and the client compiled through Fable. Later review fixes and partial reversions made all of those results stale. The final full suite was not run.

Current status: stopped, blocked, uncommitted, and not known to compile. All 28 ticket files are stashed. No commit or push was made. History action application naming and the Serialization and ChangeLog codecs were partially repaired after an interrupted reversal and remain unverified. DB legacy bare-Change restart coverage is still missing.

Do not continue from the stashed tree as though it were a green implementation. First choose between discarding the ticket-specific edits and restarting with the narrow behavior above, or auditing and repairing the broad attempt. A restart is the lower-risk option: widen existing change queues, payloads, durable entries, Poll tails, and server application to carry HistoryAction while preserving existing route, field, helper, bootstrap, revision, and retry behavior.

## Corrective Plan

The history above records the stopped attempt and its then-current interpretation. This section supersedes that interpretation. Discard the ticket-specific source edits and restart [[.scratch/selective-client-loading/issues/15-introduce-history-action-messaging.md]] from the last stable checkpoint.

- `HistoryAction` is the client pending-queue, submit-payload, and server-command unit only. It has `Change`, `Undo`, and `Redo` cases; every case carries the existing revision id and `changeId`, and only `Change` carries operations.
- Keep the existing mixed `/changes` batch and atomic application path. The client applies Undo and Redo immediately with its local `History`, then submits the explicit action without waiting for the server.
- The server applies each action to canonical process-local `History`, materializes its graph effect as an ordinary `Change` carrying the action's same revision id and `changeId`, and persists that Change through the existing path.
- `ChangeLog`, Poll, bootstrap, and Load catch-up remain ordered `Change` paths. They do not store or return Undo or Redo actions.
- Applying a non-empty upstream Change tail clears local `History` and applies the tail without recording those upstream Changes in local `History`. Empty Polls and acknowledgements of the client's own submissions preserve local `History`.
- Server `History` starts empty on every process start. [[src/Server/Database.fs]] and [[src/Server/FileAgent.fs]] load authoritative graph state without replaying `ChangeLog`; the server start time becomes the Poll mismatch that puts an existing page into the stale-client refresh flow.
- Do not add a compatibility decoder or migration. Existing HistoryAction-specific durable codecs, bootstrap journals, catch-up paths, and replay work from the stopped attempt are not part of the restart.
- Preserve existing routes, response shapes, ChangeLog format, retry identity, revision chaining, optimistic conflict behavior, and single-flight synchronization except where the explicit decisions above require a narrow change.
