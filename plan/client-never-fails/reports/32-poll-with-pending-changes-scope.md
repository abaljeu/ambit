# 32 — Poll with pending changes scope

Slice: A post reply undoes the posted changes and replays the server Events. A Poll has an empty undo list, so the server Events apply. tryStartPoll emits PollServer while pending changes wait. Those pending changes post after the Poll applies.

## Signatures

[ ] `SyncPlanner.tryStartPoll` in `src/Shared/SyncPlanner.fs` emits `PollServer` only when the pending queue is empty and the state is `Idle`.
[ ] `SyncLogic.applySyncResponse` replays trailing pending after a precondition undo.

## Match sites

[ ] ``tryStartPoll returns no effects when queue is non-empty`` is the current gate.
[ ] A Poll response carries an empty undo list relative to a post of our own changes.

## Existing tests

[ ] ``tryStartPoll returns no effects when queue is non-empty`` in [[tests/Shared.Tests/SyncPlannerTests.fs]].
[ ] ``applySyncResponse re-applies trailing pending after precondition undo`` in [[tests/Shared.Tests/SyncLogicTests.fs]].

## Copy-from

[ ] `SyncLogic.reapplyLeftoverPending` is the replay helper `applySyncResponse` already uses.

## write-ticket

### Source files

[ ] [[src/Shared/SyncPlanner.fs]] — `tryStartPoll`.
[ ] [[src/Shared/SyncLogic.fs]] — post-reply undo and replay. A Poll passes an empty undo list.

### Tests

[ ] Change ``tryStartPoll returns no effects when queue is non-empty`` so the result includes `PollServer` while pending changes wait.
[ ] Add ``pending changes post after Poll applies`` in `tests/Shared.Tests/SyncLogicTests.fs`.

## Size

[ ] Source files: 2. Under the limit of 8.
[ ] Tests added or changed: 2. Under the limit of 4.
