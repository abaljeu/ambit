# 06 — Idle snapshot truncation

**Context:** Do not rewrite the snapshot after every edit. When the log is long, idle truncation projects the live Graph to bootstrap scope, writes a new **F₀**, and drops the log prefix. Load-only Workspace Nodes must stay out of the snapshot (user story 11).

**What to build:** Shared `shouldTruncate` (log length or Revision gap over bound). At idle, `ResidentProjection.bootstrapGraph RootClosure savedZoom` of the live Graph becomes the new snapshot; delete Changes with `id <=` new Revision. Pending and Session stores stay in localStorage.

**Blocked by:** [[05-novel-tail-and-state-fallback-matrix.md]]

**See also:** [[.scratch/client-start-time/reports/cache-first-boot-via-poll.md]], [[src/Shared/ResidentProjection.fs]], [[.scratch/selective-client-loading/spec.md]] user story 11

**Status:** ready-for-agent

- [x] `shouldTruncate` is true when log length or `clientRev - snapshotR` exceeds the bound; false otherwise.
- [x] Idle truncation writes a bootstrap-scoped snapshot (ROOT closure plus optional Zoom Workspace) and drops the log prefix.
- [x] A Workspace Node present only via Load is not in the truncated snapshot.
- [x] Truncation does not run on `pagehide`.
- [x] Shared tests cover the bound, bootstrap projection of a Graph that also has a Load-only Workspace, and that the truncated snapshot omits that Workspace's children.
