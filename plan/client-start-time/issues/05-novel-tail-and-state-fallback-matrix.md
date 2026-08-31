# 05 — Novel tail plus `/state` fallback matrix

**Context:** Slice 5 of [[plan/client-start-time/reports/cache-first-boot-via-poll.md]]. Other-actor events must apply and append. Poll cannot repair a wrong snapshot: revision regression, apply error, scope/codec mismatch, or an oversized tail must delete the cache and fetch `/state`.

**What to build:** Shared `decideBootPoll`. Novel tail → `applyServerTail` then append. Fallback reasons delete snapshot+log and fetch `/state`. Oversized: novel count or `poll.revision - clientRev` over a documented bound.

**Blocked by:** [[04-immediate-boot-poll-after-first-paint.md]]

**See also:** [[plan/client-start-time/reports/cache-first-boot-via-poll.md]], [[src/Shared/SyncLogic.fs]], [[plan/event-sourced-ops/overview.md]]

**Status:** ready-for-agent

- [x] Novel Poll Changes apply through `applyServerTail`, patch the Graph, and append to the log.
- [x] `poll.revision < clientRev`: delete cache and fetch `/state`.
- [x] Apply error on the novel tail: delete cache and fetch `/state`.
- [x] Scope or codec mismatch remains a `/state` miss (slice 3) and still deletes unusable cache on that path.
- [x] Oversized novel count or Revision gap: delete cache and fetch `/state`.
- [x] Shared tests cover apply-novel, revision regression, oversized count, oversized gap, and the fallback reason strings.
