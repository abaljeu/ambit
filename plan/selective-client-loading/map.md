# Selective client loading

## Destination

Resolve every product, domain, and architectural decision needed for `/to-spec` to produce a complete, coherent, implementation-ready selective client loading specification. Implementation itself is outside this map.

## Notes

- This is an independently shippable client-only phase. The server remains fully resident. Later want-driven visible-closure work is [[plan/roadmap/epics/chapters/incremental-operations.md]] and may replace this phase's Workspace granularity and protocol.
- [[doc/roadmap/selective client loading.amb]] is a preliminary historical concept, not a current requirement or decision store.
- Tickets 01–13 are resolved historical deliberation. [[plan/selective-client-loading/issues/14-simplify-selective-loading.md]] is the sole current decision and supersedes them wherever they differ.
- Ticket 14 is the simplified model.  Future developments may revisit 01-13 to add sophistication.
- Leftover tickets [24 — Keep navigation and Find resident-only](issues/24-keep-navigation-and-find-resident-only.md), [27 — Document delivered selective-loading baseline](issues/27-document-delivered-selective-loading-baseline.md), [28 — Make hollow-circle clicks invoke Load](issues/28-make-hollow-circle-clicks-invoke-load.md), [29 — Validate two-phase state loading exploration](issues/29-validate-two-phase-state-loading.md), [30 — Ledger reuse on already-synced Load](issues/30-ledger-reuse-on-already-synced-load.md), [31 — Skip workspace-inventory when Unloaded](issues/31-skip-workspace-inventory-when-unloaded.md), and [32 — Defer or narrow path-sync ledger waterfall after push](issues/32-defer-path-sync-ledger-waterfall.md) are cancelled. They are not remaining work on this map. Record: [04 — Retire selective hollow-click and resident-only Find assumptions](plan/browser-residency/issues/04-retire-selective-hollow-click-find.md).
- Residency grows monotonically by complete Workspace within one webpage session. Refresh starts a new session; eviction and re-unloading are outside this effort.
- `Loaded` means the client received an authoritative complete direct-child list, including an empty one. Unloaded nodes remain distinct from loaded leaves.

## Decisions so far

- [[plan/selective-client-loading/issues/14-simplify-selective-loading.md]]: use monotonic complete-Workspace residency, explicit full-selection Load, serialized projected synchronization, and one shared structural guard for every local Change plan including MoveSelected; the Move dialog does not offer Unloaded destinations.

## Not yet specified
- [[plan\event-sourced-ops\overview.md]] is a standard of behavior, established after this project began, but to be met by this project.  The local spec does not yet take it into account.

## Out of scope

- Implementing selective client loading or producing implementation slices.
- Partial server residency, lazy server cache admission, server startup de-residency, or server eviction. A server endpoint needed by the client mechanism remains eligible while the server graph stays fully resident.
- Client eviction, re-unloading, or passive reclamation during a webpage session.
- A configurable loading-policy framework, alternative policies, or speculative future loading scopes.
