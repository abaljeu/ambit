# 03 — Warm F5: fold snapshot plus log, then first paint

**Context:** Slice 3 of [[plan/client-start-time/reports/cache-first-boot-via-poll.md]]. On warm reload the Browser must fold cached Change events onto snapshot **F₀** before first paint, then run Session restore and pending merge. A miss, decode error, or fold error falls back to `/state`. Feature flag off keeps today's boot.

**What to build:** Shared `decideBootRead` / `foldLog` using `ResidentProjection.applyChange`. Program reads IndexedDB when the flag is on; hit dispatches `StateLoaded` with the folded Graph and Revision `max(R, max log id)`. Miss/error fetches `/state` and writes a fresh snapshot (slice 1). Flag off always fetches `/state`.

**Blocked by:** [[02-append-accepted-change-on-retire.md]]

**See also:** [[plan/client-start-time/reports/cache-first-boot-via-poll.md]], [[src/Client/Program.fs]], [[src/Client/App.fs]], [[src/Client/SessionState.fs]], [[plan/selective-client-loading/spec.md]] user story 11

**Status:** ready-for-agent

- [x] Feature flag off: boot still fetches `/state` (unchanged path).
- [x] Flag on and valid snapshot+log: decode, fold Δ onto F₀, `StateLoaded` with folded Graph and client Revision, then Session restore + pending merge, then first paint. No `/state` on this path.
- [x] Cache miss, `codecVersion` mismatch, `file` mismatch, `scopeKey` mismatch, decode error, or fold error: fetch `/state`.
- [x] Fold uses `ResidentProjection.applyChange`; first paint includes this tab's accepted edits from the log.
- [x] Load-only Workspace Nodes are not restored (snapshot stays bootstrap-scoped).
- [x] Shared tests cover flag off, miss, codec/file/scope mismatch, decode error, fold error, and a successful SetText fold that advances Revision.
