# 10 — Migrate Browser Poll, post-Event, and Boot

**Type:** coding
**Status:** coded
Actual: 10m
**Blocked by:** None — can start immediately

## Context

The Browser already Polls and posts Events, and boot already hits `/state` or boot Poll. Those Sync calls do not send the strict current-version Want requests. Shared owns codecs and apply; the Server owns projection and answers. This batch owns Browser transport: restore the ViewModel, compute Want at each send, POST Poll, and consume the resulting answers. Auto wants need no click and no Load command.

## What to build

Each Poll and each post-Event computes and carries the current Want, including `[]`. Boot consumes the small Zoom-scoped Graph, restores Fold locally, then computes the next Want from the ViewModel. Load Fetch uses the same edges-plus-Nodes answer. Find remains residence-only and postponed.

### 1. Browser HTTP

Module [Browser HTTP](../arch.md). Story paths 13, 16, 17, 19, 20, 27.

- [x] 7.2.1 Poll carries Want — `runPollServer` POSTs current-version `PollRequest`
- [x] 7.2.2 post-Event carries Want — POST `/{file}/changes` sends the current Want beside Events
- [x] 7.3.1 Compute at send time — every Poll and post-Event runs `Want.compose` from current Graph, SiteMap, and Zoom
- [x] 27.2 Empty and repeats — always send `want`, including `[]`; repeated Wants need no client acknowledgement state
- [x] 16.2 and 17.2 Silent growth — Want attaches from Included, not from a Bullet click, `loadOp`, or `tryStartLoad`

### 2. Boot Poll

Module [Boot Poll](../arch.md). Story paths 1, 2, 3.

- [x] 8.2.3 First paint scoped Graph — consume the Server-produced visible-closure only
- [x] 2.3 Local restore — restore Zoom and Fold in the Browser after State loads
- [x] 8.2.4 Boot Poll Want — after restore, compute Fold-aware Included and carry the next Want

### 3. SyncPlanner and Load command

Modules [SyncPlanner](../arch.md), [Load command](../arch.md). Story paths 18, 30, 31.

- [x] 10.2.2 Want is payload — Want rides Poll and submit; no new flight state
- [x] 13.2.2 Load uses current answer — `runLoadServer` receives `nodes` plus `childMap`
- [x] 18.2 Hollow-click Load may remain — do not un-wire hollow-circle → Load if present

### 4. Find

Module [Find](../arch.md). Story paths 28, 29.

- [x] 12.2.2 Residence only — do not add Server Find, Want, or Fetch to commit
- [x] 29.2 Commit stays Zoom — `searchPickSetRoot` does not Load

### 5. Browser proof

Prove the Browser computes and carries Wants without command or click input.

- [x] 13.2 and 27.2 Growth proof — Poll and post-Event recompute from the current ViewModel after each install
- [x] 31.2 Load proof — explicit Load receives and installs the current edges-plus-Nodes answer

## See also

[Browser residency architecture](../arch.md), [03 — Lock ongoing want priority and when wants are attached](03-lock-ongoing-want-priority.md)

## Comments

- 2026-09-26: Filed via `/to-tickets`. Migrate batch. Blocked by expand.
- 2026-09-26: Redefined as Browser transport and boot-consume owner; Shared apply stays on 08 and Server projection stays on 09.
- 2026-09-26: Republished via `/to-tickets`; completed expand 07 clears this ticket's live blockers.

## Time

- 2026-09-27 10m — migrated Browser Poll, post-Event, boot, and Load answer consumption.
