# 01 — Accept Notices
**Status**: coded
One job: The outliner keeps working while the Browser applies each Notice and stores its event id when the pending queue is empty, including a Notice on a post reply, and Sync stays Idle when the edit box holds new text. The story Accept Notices in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: none

## Inputs

### Working

[x] ``let writeGraph (notice: Notice) (graph: Graph) : Result<Graph, string>`` in `src/Shared/NoticeGraph.fs`.
[x] ``let private applyNoticeGraph`` and ``let private applyLoggedNotice`` and ``let reconcileAck`` and ``let applyServerTail`` in `src/Shared/SyncLogic.fs`.
[x] ``let update (msg: Msg) (model: VM) : VM * Effect list`` in `src/Client/Update.fs`. The PollDone arm is the site.
[x] ``let isAutoSyncBlocked (model: VM) : bool`` in `src/Client/UpdateHelpers.fs`. Read only, so a dirty edit box still allows Idle after an empty queue.
[x] `SyncLogic.applyLoggedNotice` matches `EventBody.Notice` and calls `applyNoticeGraph`.
[x] `SyncLogic.reconcileAck` calls `dropNotices` before confirmation matching.
[x] `Update.update` matches `SysMsg (PollDone ...)` and can set `DataOutdated` while the queue is empty.
[x] `Update.applySubmitResponse` calls `reconcileAck` on a post reply.
[x] ``PersistFailed leaves the graph`` in [[tests/Shared.Tests/SyncLogicTests.fs]] checks `PersistFailed` leaves the Graph unchanged.
[x] ``NodePersisted on a normal node is rejected`` in `tests/Shared.Tests/SyncLogicTests.fs` expects `Error` with `normal nodes do not have parse or persist state`.
[x] ``Notice in the reply keeps the confirmation echo`` in [[tests/Shared.Tests/AckReconcileTests.fs]] checks a trailing Notice on `reconcileAck`.
[x] ``Poll answer chains the next Included residency Want`` in [[tests/Server.Tests/BrowserResidencyClientTests.fs]] calls `Update.update` with PollDone.
[x] `applyLoggedNotice` in `src/Shared/SyncLogic.fs` already stores the event id when `writeGraph` returns `Ok`. This ticket uses that path when `writeGraph` returns `Error` of a string.
[x] [[src/Shared/SyncLogic.fs]] — `applyNoticeGraph`, `applyLoggedNotice`, and `reconcileAck`.
[x] [[src/Client/Update.fs]] — PollDone leaves Sync `Idle` when the pending queue is empty, including when the edit box holds new text.

### Reference

[x] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: Server crash ordering, placeholder rows, `ChildrenStatus`, Absent fetch, the typing pause, and the reload prompt.
[ ] `src/Shared/NoticeGraph.fs` stays as it is. `writeGraph` already returns `Error` of a string for a non-artifact Node.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change `applyNoticeGraph` and `applyLoggedNotice` in `src/Shared/SyncLogic.fs` so a `writeGraph` `Error` of a string leaves the Graph unchanged and still stores the event id.
3. [x] Change `reconcileAck` in `src/Shared/SyncLogic.fs` so a post reply applies its Notices, then matches confirmations.
4. [x] Change the PollDone arm in `src/Client/Update.fs` so an empty pending queue leaves Sync `Idle`, including when `isAutoSyncBlocked` is true because the edit box holds new text.
5. [x] Change ``NodePersisted on a normal node is rejected`` and add the three facts named in Outputs.

## Outputs

[x] `src/Shared/SyncLogic.fs`
[x] `src/Client/Update.fs`
[x] `tests/Shared.Tests/SyncLogicTests.fs` — change ``NodePersisted on a normal node is rejected``; add the `writeGraph` `Error` fact
[x] `tests/Shared.Tests/AckReconcileTests.fs` — add the post-reply Notice fact
[x] `tests/Server.Tests/BrowserResidencyClientTests.fs` — add the PollDone Idle fact

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/01-accept-notices-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``NodePersisted on a normal node is rejected``
  - `tests/Shared.Tests`: the new `writeGraph` `Error` fact name written in the test file
  - `tests/Shared.Tests`: the new post-reply Notice fact name written in the test file
  - `tests/Server.Tests`: the new PollDone Idle fact name written in the test file
[x] All executed tests pass.
[x] Write the verdict to `plan/client-never-fails/reports/01-accept-notices-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
