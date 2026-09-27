# 32 — Defer or narrow path-sync ledger waterfall after push

**Type:** coding
**Status:** cancelled
**Blocked by:** None — can start immediately.

## Context

After a successful workspace-push, the Browser runs [[src/Client/App.fs]] `runWorkspacePathSyncSnapshot`: one mappings GET, then a sequential sync-ledger POST per mapped label. Each Desktop handler walks the whole tree via [[src/Shared/dotnet/WorkspaceSyncLedger.fs]] `liveStatusRows`. That waterfall is for path-sync UI, not for `/load` Fetch correctness, and it sits on the Load critical path.

## What to build

Path-sync ledger refresh after push does not block Load Fetch. Defer it, run it in the background, or refresh only the label just pushed.

- [ ] Successful push still reaches `/load` Fetch without waiting for every mapped label's ledger walk.
- [ ] Path-sync UI still updates, either later or for the pushed label only.
- [ ] No change to Fetch correctness for Unloaded vs Loaded.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md.
- 2026-09-26: Status `cancelled`. Path-sync ledger refresh after push is out of scope for [browser-residency](plan/browser-residency/map.md). Retarget to [[plan/transport-layer/project.md]]. See [04 — Retire selective hollow-click and resident-only Find assumptions](plan/browser-residency/issues/04-retire-selective-hollow-click-find.md).

## See also

[[tmp/load-performance-audit.md]], [[src/Client/App.fs]]
