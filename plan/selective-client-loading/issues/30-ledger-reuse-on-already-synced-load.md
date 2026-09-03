# Ledger reuse on already-synced Load

Type: grilling
Status: open
Blocked by:

## Question

On an already-synced Load (Mask path), how should the workspace-push ledger be reused so Depth-infinity PROPFIND seed does not run again? Diagnose empty-ledger resets. Secondary to the Load path; do not implement in this ticket.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md. Audit: [[tmp/load-performance-audit.md]]. Code: [[src/Shared/dotnet/WorkspaceSyncLedger.fs]] `needsSeed`, [[src/Shared/dotnet/WorkspaceFileSync.fs]] `ensureLedgerSeeded`.
