# 01 — Accept Notices
**Status**: 
One job: The outliner keeps working while the Browser applies each Notice and stores its event id when the pending queue is empty, including a Notice on a post reply, and Sync stays Idle when the edit box holds new text. The story Accept Notices in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: none

## Inputs

### Working

[ ] ``let writeGraph (notice: Notice) (graph: Graph) : Result<Graph, string>`` in `src/Shared/NoticeGraph.fs`.
[ ] ``let private applyNoticeGraph`` and ``let private applyLoggedNotice`` and ``let reconcileAck`` and ``let applyServerTail`` in `src/Shared/SyncLogic.fs`.
[ ] ``let update (msg: Msg) (model: VM) : VM * Effect list`` in `src/Client/Update.fs`. The PollDone arm is the site.
[ ] ``let isAutoSyncBlocked (model: VM) : bool`` in `src/Client/UpdateHelpers.fs`. Read only, so a dirty edit box still allows Idle after an empty queue.
[ ] `SyncLogic.applyLoggedNotice` matches `EventBody.Notice` and calls `applyNoticeGraph`.
[ ] `SyncLogic.reconcileAck` calls `dropNotices` before confirmation matching.
[ ] `Update.update` matches `SysMsg (PollDone ...)` and can set `DataOutdated` while the queue is empty.
[ ] `Update.applySubmitResponse` calls `reconcileAck` on a post reply.
[ ] ``PersistFailed leaves the graph`` in [[tests/Shared.Tests/SyncLogicTests.fs]] checks `PersistFailed` leaves the Graph unchanged.
[ ] ``NodePersisted on a normal node is rejected`` in `tests/Shared.Tests/SyncLogicTests.fs` expects `Error` with `normal nodes do not have parse or persist state`.
[ ] ``Notice in the reply keeps the confirmation echo`` in [[tests/Shared.Tests/AckReconcileTests.fs]] checks a trailing Notice on `reconcileAck`.
[ ] ``Poll answer chains the next Included residency Want`` in [[tests/Server.Tests/BrowserResidencyClientTests.fs]] calls `Update.update` with PollDone.
[ ] `applyLoggedNotice` in `src/Shared/SyncLogic.fs` already stores the event id when `writeGraph` returns `Ok`. This ticket uses that path when `writeGraph` returns `Error` of a string.
[ ] [[src/Shared/SyncLogic.fs]] — `applyNoticeGraph`, `applyLoggedNotice`, and `reconcileAck`.
[ ] [[src/Client/Update.fs]] — PollDone leaves Sync `Idle` when the pending queue is empty, including when the edit box holds new text.

### Reference

[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: Server crash ordering, placeholder rows, `ChildrenStatus`, Absent fetch, the typing pause, and the reload prompt.
[ ] `src/Shared/NoticeGraph.fs` stays as it is. `writeGraph` already returns `Error` of a string for a non-artifact Node.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `applyNoticeGraph` and `applyLoggedNotice` in `src/Shared/SyncLogic.fs` so a `writeGraph` `Error` of a string leaves the Graph unchanged and still stores the event id.
3. [ ] Change `reconcileAck` in `src/Shared/SyncLogic.fs` so a post reply applies its Notices, then matches confirmations.
4. [ ] Change the PollDone arm in `src/Client/Update.fs` so an empty pending queue leaves Sync `Idle`, including when `isAutoSyncBlocked` is true because the edit box holds new text.
5. [ ] Change ``NodePersisted on a normal node is rejected`` and add the three facts named in Outputs.

## Outputs

[ ] `src/Shared/SyncLogic.fs`
[ ] `src/Client/Update.fs`
[ ] `tests/Shared.Tests/SyncLogicTests.fs` — change ``NodePersisted on a normal node is rejected``; add the `writeGraph` `Error` fact
[ ] `tests/Shared.Tests/AckReconcileTests.fs` — add the post-reply Notice fact
[ ] `tests/Server.Tests/BrowserResidencyClientTests.fs` — add the PollDone Idle fact

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/01-accept-notices-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``NodePersisted on a normal node is rejected``
  - `tests/Shared.Tests`: the new `writeGraph` `Error` fact name written in the test file
  - `tests/Shared.Tests`: the new post-reply Notice fact name written in the test file
  - `tests/Server.Tests`: the new PollDone Idle fact name written in the test file
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/01-accept-notices-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
