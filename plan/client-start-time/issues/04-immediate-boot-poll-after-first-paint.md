# 04 — Immediate boot Poll after first paint

**Context:** After cache-first paint, Poll is catch-up for events this tab did not cache. Do not wait for the 5s interval. Skip Change already in the local log (`changeId` or `id`). Warm F5 with a valid cache must not fetch `/state`.

**What to build:** Immediate `GET /{file}/poll?rev={clientRev}` after first paint. Shared `novelChanges` filters the tail. Duplicate-only tails update `isReady` and do no Graph work. CodeOutdated keeps the existing refresh banner; cache stays advisory until page stamps match.

**Blocked by:** [[03-warm-f5-fold-then-first-paint.md]]

**See also:** [[.scratch/client-start-time/reports/cache-first-boot-via-poll.md]], [[src/Shared/SyncLogic.fs]], [[src/Client/App.fs]]

**Status:** ready-for-agent

- [x] After first paint, Browser issues one immediate Poll at `clientRev` (then the existing interval still starts).
- [x] Warm F5 with a valid cache does not fetch `/state`.
- [x] Poll Changes already in the local log (matching `changeId` or `id`) are skipped; no double-apply.
- [x] Empty tail or duplicate-only tail: update `isReady` from Poll; no Graph write.
- [x] CodeOutdated: existing stale banner; do not delete cache as the recovery for stamps.
- [x] Shared tests cover skip-by-id, skip-by-changeId, empty confirm, and duplicate-only confirm.
