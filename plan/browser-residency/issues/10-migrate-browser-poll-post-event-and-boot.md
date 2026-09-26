# 10 — Migrate Browser Poll, post-Event, and Boot

**Type:** coding
**Status:** defined
**Blocked by:** [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md), [02 — Lock bootstrap visible-closure set](02-lock-bootstrap-visible-closure.md), [03 — Lock ongoing want priority and when wants are attached](03-lock-ongoing-want-priority.md), [07 — Expand Want and edges/Nodes package](07-expand-want-and-edges-nodes-package.md), [08 — Migrate Shared wire](08-migrate-shared-wire.md), [09 — Migrate Server Sync doors](09-migrate-server-sync-doors.md)

## Context

The Browser already Polls and posts Events, and boot already hits `/state` or boot Poll. Those Sync calls do not send the strict current-version Want requests. Shared owns codecs and apply; the Server owns projection and answers. This batch owns Browser transport: restore the ViewModel, compute Want at each send, POST Poll, and consume the resulting answers. Auto wants need no click and no Load command.

## What to build

Each Poll and each post-Event computes and carries the current Want, including `[]`. Boot consumes the small Zoom-scoped Graph, restores Fold locally, then computes the next Want from the ViewModel. Load Fetch uses the same edges-plus-Nodes answer. Find remains residence-only and postponed.

### 1. Browser HTTP

Module [Browser HTTP](../arch.md). Story paths 13, 16, 17, 19, 20, 27.

1. [ ] Poll carries Want — story **Wants on Poll**: `runPollServer` POSTs current-version `PollRequest`
2. [ ] post-Event carries Want — 19.2: POST `/{file}/changes` sends the current Want
3. [ ] Compute at send time — [03 — Lock ongoing want priority and when wants are attached](03-lock-ongoing-want-priority.md): each Poll and post-Event runs `Want.compose` from current Graph, SiteMap, and Zoom
4. [ ] Empty and repeats — always send `want`, including `[]`; repeated Wants need no client acknowledgement state
5. [ ] No click — 16.2: Want attaches from Included, not from a Bullet click
6. [ ] No command — 17.2: auto Want does not call `loadOp` or `tryStartLoad`
7. [ ] Same wants — App and Browser use `Want.compose`

### 2. Boot Poll

Module [Boot Poll](../arch.md). Story paths 1, 2, 3.

1. [x] Zoom bootstrap request — send best-effort saved Zoom on `/state`
2. [ ] First paint scoped Graph — consume the Server-produced visible-closure only
3. [ ] Zoom and framing path — assert Zoom, Zoom Children, and the Zoom ancestor path are Resident before first paint
4. [ ] Local Fold restore — restore Fold in the Browser after State loads
5. [ ] Boot Poll may Want — after restore, boot Poll computes Fold-aware Included and carries the next Want

### 3. SyncPlanner and Load command

Modules [SyncPlanner](../arch.md), [Load command](../arch.md). Story paths 18, 30, 31.

1. [ ] Want is payload — 10.2.2: Want rides Poll and submit; no new flight state
2. [ ] Load uses current answer — story **Load uses the same package**: `runLoadServer` receives `nodes` plus `childMap`
3. [ ] Hollow-click Load may remain — 18.2: do not un-wire hollow-circle → Load if present

### 4. Find

Module [Find](../arch.md). Story paths 28, 29.

1. [ ] Residence only — 28.3: do not add a Want or Fetch from Find
2. [ ] Commit stays Zoom — 29.2: `searchPickSetRoot` does not Load
3. [ ] Postponed Server mode — [05 — Chart server-mode Find](05-chart-server-mode-find.md) does not gate this ticket

## See also

[Browser residency architecture](../arch.md), [spec.md](../spec.md) Solution **Auto wants**

## Comments

- 2026-09-26: Filed via `/to-tickets`. Migrate batch. Blocked by expand.
- 2026-09-26: Redefined as Browser transport and boot-consume owner; Shared apply stays on 08 and Server projection stays on 09.
