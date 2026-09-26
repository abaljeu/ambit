# 10 — Migrate Browser Poll, post-Event, and Boot

**Type:** coding
**Status:** blocked
**Blocked by:** [07 — Expand Want and edges/Nodes package](07-expand-want-and-edges-nodes-package.md)

## Context

The Browser already Polls and posts Events, and boot already hits `/state` or boot Poll. Those doors do not send a Want or install edges plus Nodes. After expand, Shared compose and codecs exist. This batch attaches the same Want on App and Browser Poll and post-Event, and lets boot consume visible-closure. Auto wants need no click and no Load command.

## What to build

Each Poll and each post-Event carries the current Want. The Browser installs the Want answer after Changes. First paint uses the visible-closure Graph. Load Fetch still uses `/load` `packages` as the sole Load path until [12 — Contract old Load Fetch packages](12-contract-old-load-fetch-packages.md). Do not add a production dual-run ([06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md) done). Find does not Fetch.

### 1. Browser HTTP

Module [Browser HTTP](../arch.md). Story paths 13, 16, 17, 19, 20, 27.

1. [ ] Poll carries Want — 20.2: `runPollServer` sends the current Want
2. [ ] post-Event carries Want — 19.2: POST `/{file}/changes` sends the current Want
3. [ ] Recompute while I work — 27.2: later Poll and post-Event recompute Want from current Included
4. [ ] No click — 16.2: Want attaches from Included, not from a Bullet click
5. [ ] No command — 17.2: auto Want does not call `loadOp` or `tryStartLoad`
6. [ ] Same wants — App and Browser use Want.compose

### 2. Boot Poll

Module [Boot Poll](../arch.md). Story paths 1, 2, 3.

1. [ ] First paint scoped Graph — 1.4: boot installs visible-closure only
2. [ ] Included and framing path — 2.1, 3.1: Included and Zoom ancestors are Resident at first paint
3. [ ] Boot Poll may Want — 8.2.2: after that Graph exists, boot Poll may carry Want

### 3. SyncPlanner and Load command

Modules [SyncPlanner](../arch.md), [Load command](../arch.md). Story paths 18, 30, 31.

1. [ ] Want is payload — 10.2.2: Want rides Poll and submit; no new flight state
2. [ ] Old Load path stays until contract — 31.2: `runLoadServer` still POSTs `/load` `packages` as the sole Load path; not a production dual-run
3. [ ] Hollow-click Load may remain — 18.2: do not un-wire hollow-circle → Load if present

### 4. Find

Module [Find](../arch.md). Story paths 28, 29.

1. [ ] Residence only — 28.3: do not add a Want or Fetch from Find
2. [ ] Commit stays Zoom — 29.2: `searchPickSetRoot` does not Load

## See also

[Browser residency architecture](../arch.md), [spec.md](../spec.md) Solution **Auto wants**

## Comments

- 2026-09-26: Filed via `/to-tickets`. Migrate batch. Blocked by expand.
- 2026-09-26: [06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md) locked no dual-run in product. This batch does not add a second production Load path.
