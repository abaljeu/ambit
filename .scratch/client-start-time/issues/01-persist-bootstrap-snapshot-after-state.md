# 01 — Persist bootstrap snapshot after `/state`

**Context:** Warm F5 still waits on `GET /state`. Slice 1 of [[.scratch/client-start-time/reports/cache-first-boot-via-poll.md]] writes a bootstrap-scoped snapshot to IndexedDB after a successful `/state` so later slices can paint from cache. This slice does not read the cache at boot.

**What to build:** Shared snapshot envelope (database `gambol-boot-cache-v1`, store `snapshots`, `codecVersion`, `file`, `scopeKey`, `revision`, `stateJson`). Browser writes that envelope after a successful `/state` decode and clears the Change log. Do not write on `pagehide`. Do not persist Load-only Workspace Nodes. No boot read yet.

**Blocked by:** None

**See also:** [[.scratch/client-start-time/reports/cache-first-boot-via-poll.md]], [[.scratch/client-start-time/reports/reload-state-reuse-investigation.md]], [[.scratch/selective-client-loading/spec.md]] user story 11, [[.scratch/event-sourced-ops/overview.md]]

**Status:** ready-for-agent

- [x] `scopeKey` is `root` when there is no saved Zoom widen, and `root|zoom:{guid}` when `tryReadSavedZoomId` is Some.
- [x] Snapshot record carries `codecVersion`, `file`, `scopeKey`, `revision`, `isReady`, `stateJson` (the `/state` body), and `writtenAt`.
- [x] After successful `/state` decode, Browser writes the snapshot to IndexedDB database `gambol-boot-cache-v1` and clears store `changes` for that `file`.
- [x] `pagehide` / `visibilitychange` still write only Session and pending; they do not write the snapshot.
- [x] Boot still fetches `/state` (no cache read in this slice).
- [x] Focused Shared tests cover `scopeKey`, envelope encode/decode, and file/scope/codec metadata checks.
