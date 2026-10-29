# 01 — Accept Notices

Ticket: [[01-accept-notices-ticket.md]]

Spec: [[plan/client-never-fails/spec.md]]

Arch: [[plan/client-never-fails/arch.md]]

Review: [[01-review.md]]

## Stage

Stage is refactor in [01 — Accept Notices](CONTEXT.md).

## 1. Helpers

1. **reconcileAck.** `reconcileAck` in `src/Shared/SyncLogic.fs` is 16 lines. `finishReconcileAck` is 38 lines and matches the confirmation. `stateAfterApprovedSuffix` is 11 lines and builds the approved client state.
2. **PollDone DataOutdated.** `applyPollDataOutdated` in `src/Client/Update.fs` is 32 lines and holds the empty-queue arms. `downloadPolledOps` is 10 lines and is the poll download call for those arms and the other PollDone arms.
3. **update.** `update` is 345 lines. The other PollDone arms stay in `update`.

## 2. Notice errors

1. **applyNoticeGraph.** `applyNoticeGraph` returns the client state. A `writeGraph` string error returns that same state. `applyLoggedNotice` stores the event id on that state.
2. **applyReplyNotices.** `applyReplyNotices` folds `projectLoggedNotice` over the reply Notices. The client event id stays in place until confirmation matching sets the reply event id.

## 3. Facts

1. **Normal node.** The suite keeps ``writeGraph Error of a string leaves the graph and stores the event id``. That fact checks the whole node and the stored event id for `Notice.NodePersisted` on a normal node. ``NodePersisted on a normal node is rejected`` was that same seam.
2. **Edit text.** ``PollDone leaves Sync Idle when the edit box holds new text`` sets edit text `parent new` on the node text `parent` and expects Sync `Idle`.

## 4. Suite

1. **Command.** `bash scripts/check-test.sh 4439` printed `DONE` and `/tmp/ambit-full-suite-20261029-183221.log`. The log ends `full suite finished`.
2. **Server.** `tests/Server.Tests` passed 728, skipped 1, failed 0.
3. **Shared.** `tests/Shared.Tests` passed 1907, skipped 3, failed 0.
