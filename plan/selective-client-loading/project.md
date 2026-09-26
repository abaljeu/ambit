# Selective client loading

Stage: done
Summary: Give the Browser a Graph that starts with only the Workspace Nodes needed for ROOT and restored navigation, grow residency only through explicit Load, and keep the Server Graph fully Resident and authoritative.
Updated: 2026-09-26
Finished: 2026-09-26

## Notes

- Prior whole-Workspace slice is complete.
- Successor is [[plan/browser-residency/project.md]] (want-driven visible-closure).
- Leftover tickets [24 — Keep navigation and Find resident-only](issues/24-keep-navigation-and-find-resident-only.md), [27 — Document delivered selective-loading baseline](issues/27-document-delivered-selective-loading-baseline.md), [28 — Make hollow-circle clicks invoke Load](issues/28-make-hollow-circle-clicks-invoke-load.md), [29 — Validate two-phase state loading exploration](issues/29-validate-two-phase-state-loading.md), [30 — Ledger reuse on already-synced Load](issues/30-ledger-reuse-on-already-synced-load.md), [31 — Skip workspace-inventory when Unloaded](issues/31-skip-workspace-inventory-when-unloaded.md), and [32 — Defer or narrow path-sync ledger waterfall after push](issues/32-defer-path-sync-ledger-waterfall.md) are cancelled. They are not active work under this done Project. Record: [04 — Retire selective hollow-click and resident-only Find assumptions](plan/browser-residency/issues/04-retire-selective-hollow-click-find.md).
