# 01 — Accept Notices build

## Stage

[ ] Stage is build in [01 — Accept Notices](CONTEXT.md).

## Edits

[ ] `applyNoticeGraph` and `applyLoggedNotice` in `src/Shared/SyncLogic.fs` keep the Graph on a `writeGraph` string error and store that Notice event id.
[ ] `applyReplyNotices` runs from `reconcileAck` before confirmation matching.
[ ] The PollDone `DataOutdated` arm in `src/Client/Update.fs` leaves Sync `Idle` when `pending` is empty.
[ ] ``NodePersisted on a normal node is rejected`` expects `Ok`, the same Graph, and the Notice event id.
[ ] ``writeGraph Error of a string leaves the graph and stores the event id`` covers that same seam.
[ ] ``post reply applies a Notice then matches the confirmation`` expects `PersistState.Persisted`, the Notice file time, and an empty pending queue.
[ ] ``PollDone leaves Sync Idle when the edit box holds new text`` expects `Idle` for empty events, empty `pending`, and `Editing`.

## Check

[ ] Verdict is `pass` in [01 — Accept Notices check](plan/client-never-fails/reports/01-accept-notices-check.md).

## Status

[ ] Ticket status is `coded`.
