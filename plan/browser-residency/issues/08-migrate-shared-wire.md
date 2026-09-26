# 08 — Migrate Shared wire

**Type:** coding
**Status:** coded
**Estimate:** 2h
**Actual:** 3h
**Blocked by:** None — can start immediately

## Context

Shared types contain the new Want and answer fields, but codecs and Sync apply still allow or use old shapes. This batch makes Poll, post-Event, Load Fetch, and their answer one strict current-version wire. There is no old/new wire interoperation.

## What to build

Poll and post-Event codecs require Want; their answer requires edges plus Nodes. Load Fetch uses the same answer fields. Missing current-version fields fail decode. After the Event tail, SyncLogic installs the answer through one path.

### 1. Sync wire

Module [Sync wire](../arch.md).

- [x] 5.2.1 Request and answer codecs — `PollRequest` and `ChangeRequest` require `want`; `ChangeSuccessResponse` requires `nodes` plus `childMap`; empty Want is `[]`
- [x] 5.2.3 Current version only — missing version 13 fields fail decode and no compatibility branch remains

### 2. SyncLogic

Module [SyncLogic](../arch.md). Story path 9.

- [x] 9.2.1 Apply order — apply the Event tail before the edges-plus-Nodes answer
- [x] 9.2.2 One residency install — Poll, post-Event, bootstrap, and Load Fetch install through `installWantAnswer`
- [x] 9.3.3 One Load response — `loadResponseToSync` maps Load Fetch onto `nodes` plus `childMap`; it does not install `packages`

### 3. Shared proof

Use the narrowest Shared seam named in [Browser residency architecture](../arch.md).

- [x] 9.3 Shared apply proof — Events apply before residency; `[]` marks Loaded; a missing key stays Unloaded; dangling edges fail

## See also

[Browser residency architecture](../arch.md), [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md)

## Comments

- 2026-09-26: Filed via `/to-tickets`. Migrate batch. Blocked by expand.
- 2026-09-26: Redefined for strict current-version codecs and Shared apply after decisions 01–03.
- 2026-09-26: Republished via `/to-tickets`; completed expand 07 clears this ticket's live blockers.
- 2026-09-26: Built Shared current-version codecs and one `installWantAnswer` apply path. `getPollOutcome` keys on event id; it does not branch on `apiVersion`. `LoadResponse.packages` still exist for later contract. Production Server and Browser doors stay on [09 — Migrate Server Sync doors](09-migrate-server-sync-doors.md) through [11 — Migrate Bullet and Included readers](11-migrate-bullet-included-and-bootstrap-wants.md).

## Time

- 2026-09-26 2h — SyncLogic installWantAnswer after Events, changeSuccessToSync, Shared.Tests (from chat)
- 2026-09-26 0.5h — strip apiVersion handler branch in getPollOutcome (from chat)
- 2026-09-26 0.5h — rebase onto staging cleanup; required decode; Load maps onto nodes plus childMap (from chat)
