# Ledger reuse on already-synced Load

**Type:** grilling
**Status:** cancelled
Blocked by:

## Question

On an already-synced Load (Mask path), how should the workspace-push ledger be reused so Depth-infinity PROPFIND seed does not run again? Diagnose empty-ledger resets. Secondary to the Load path; do not implement in this ticket.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md. Audit: [[tmp/load-performance-audit.md]]. Code: [[src/Shared/dotnet/WorkspaceSyncLedger.fs]] `needsSeed`, [[src/Shared/dotnet/WorkspaceFileSync.fs]] `ensureLedgerSeeded`.
- 2026-09-26: Status `cancelled`. File-transit / workspace-push ledger work is out of scope for [browser-residency](plan/browser-residency/map.md). Retarget to [[plan/transport-layer/project.md]]. See [04 — Retire selective hollow-click and resident-only Find assumptions](plan/browser-residency/issues/04-retire-selective-hollow-click-find.md).
