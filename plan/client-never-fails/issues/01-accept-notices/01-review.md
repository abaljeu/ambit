# 01 — Accept Notices

Ticket: [[01-accept-notices-ticket.md]]
Spec: [[plan/client-never-fails/spec.md]]
Arch: [[plan/client-never-fails/arch.md]]

## SPEC DEFECTS

A spec defect means the code misses the story Accept Notices or the ticket One job.

- No spec defects.

The story and the One job hold in the diff:

1. **Empty queue.** With an empty pending queue, `applyLoggedNotice` applies each Notice and stores that event id.
2. **Write error.** `applyNoticeGraph` keeps the Graph when `NoticeGraph.writeGraph` returns `Error` of a string, and `applyLoggedNotice` still stores the event id.
3. **Post reply.** `reconcileAck` runs `applyReplyNotices` before confirmation matching. A matching post reply keeps the Notice write. `eventId` becomes the reply `serverEventId`, which `CoreChanges.fromPosted` sets to the last stored event id.
4. **Idle.** The PollDone `DataOutdated` arm leaves Sync `Idle` when `pending` is empty, including `Editing`. A non-empty `pending` still sets `DataOutdated`, which matches `isAutoSyncBlocked` for that queue.

## 1. Standards

1. **reconcileAck length.** **Hard.** `reconcileAck` in `src/Shared/SyncLogic.fs` is 45 lines (556–600). [[.agents/rules/fsharp-source.md]] limits a function to 40 lines. This diff adds the `applyReplyNotices` call inside that function. The function was already past 40 lines before this diff.
2. **update length.** **Hard.** `update` in `src/Client/Update.fs` is 374 lines (170–543). The same 40-line limit applies. This diff adds the empty-queue PollDone arms inside that function. The function was already past 40 lines before this diff.
3. **Dead error arms.** **Judgement.** Overcomplication. `applyNoticeGraph` returns `Ok` for a `writeGraph` error, and `applyLoggedNotice` returns `Ok` for a Notice. The `Error` arm in `applyLoggedNotice` and the `Error` arm in `applyReplyNotices` have no Notice input that reaches them. Source: [[plan/context/SMELLS.md]]. The arms are `| Error _ -> Ok st`, `| Error _ -> Ok({ st with eventId = event.id }, undid)`, and `| Error _ -> state`.
4. **Two normal-node facts.** **Judgement.** Duplicated Code. ``NodePersisted on a normal node is rejected`` and ``writeGraph Error of a string leaves the graph and stores the event id`` both send `Notice.NodePersisted` for a normal Node through `applyTail` and expect the same Graph and the stored event id. The ticket Outputs name both facts.

`python3 scripts/standards-scan.py --diff origin/staging` printed one measured binding, `applyReplyNotices` at 9 lines. That length is inside the limit. The scan printed no long line, no glossary phrase, and no refer-by-name hit.

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

1. **Suite.** `bash scripts/check-test.sh 3558` printed `DONE` and `/tmp/ambit-full-suite-20261029-181629.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 728, skipped 1, failed 0. `tests/Shared.Tests` passed 1908, skipped 3, failed 0.
2. **Rejected normal node.** ``NodePersisted on a normal node is rejected`` expects `Ok`, the same `updateTime`, the same `persistState`, and event id 3.
3. **Write error fact.** ``writeGraph Error of a string leaves the graph and stores the event id`` expects the same Node and the Notice event id.
4. **Post reply fact.** ``post reply applies a Notice then matches the confirmation`` expects `PersistState.Persisted`, the Notice file time, text `after`, `serverEventId`, and an empty pending queue.
5. **PollDone fact.** ``PollDone leaves Sync Idle when the edit box holds new text`` expects `Idle`. The fact sets `Editing ("parent", EditCaret.EndOfText)` on a Node whose text is `parent`, and the event list is empty. The checked edit text matches the Node text.

## 5. Miss

1. **reconcileAck length.** Hard. Standards item 1. `reconcileAck` is 45 lines. The limit is 40.
2. **update length.** Hard. Standards item 2. `update` is 374 lines. The limit is 40.

## 6. Found elsewhere

The Found elsewhere section takes the PR body's Found elsewhere list. That list is empty.

- No findings.
