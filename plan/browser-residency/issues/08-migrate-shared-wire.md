# 08 — Migrate Shared wire

**Type:** coding
**Status:** defined
**Blocked by:** [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md), [07 — Expand Want and edges/Nodes package](07-expand-want-and-edges-nodes-package.md)

## Context

Shared types contain the new Want and answer fields, but codecs and Sync apply still allow or use old shapes. This batch makes Poll, post-Event, and their answer one strict current-version wire. There is no old/new wire interoperation. Explicit Load `packages` still work until the later contract.

## What to build

Poll and post-Event codecs require Want; their answer requires edges plus Nodes. Missing current-version fields fail decode. After the Event tail, SyncLogic installs the Want answer. Load Fetch `packages` still apply.

### 1. Sync wire

Module [Sync wire](../arch.md).

1. [ ] Poll and post-Event request codecs — `PollRequest` and `ChangeRequest` require `want`; empty is `[]`
2. [ ] Sync answer codec — `ChangeSuccessResponse` requires `nodes` plus `childMap`
3. [ ] Current version only — missing version 13 fields fail decode; no compatibility branch remains

### 2. SyncLogic

Module [SyncLogic](../arch.md). Story path 9.

1. [ ] Install after Events — Module **SyncLogic** Interface **Install Want answer**: apply the Event tail, then `installWantAnswer`
2. [ ] Dual-run packages — Module **SyncLogic** Uses **Load dual-run**: `loadResponseToSync` still installs `packages`
3. [ ] Outcome stamps unchanged — `getPollOutcome` still keys on `apiVersion` and event id

## See also

[Browser residency architecture](../arch.md), [07 — Expand Want and edges/Nodes package](07-expand-want-and-edges-nodes-package.md)

## Comments

- 2026-09-26: Filed via `/to-tickets`. Migrate batch. Blocked by expand.
- 2026-09-26: Redefined for strict current-version codecs and Shared apply after decisions 01–03.
