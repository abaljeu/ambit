# 29 — Validate two-phase state loading exploration

**What to build:** Validate the spec-break (folds widen bootstrap), Phase 1 thin-id-list feasibility, and production \|V⁺\| vs Workspace. Reconcile with cache-first boot. Decisions are captured; promotion waits on validation. Do not implement product code in this ticket.

**Blocked by:** None.

**Type:** research
**Status:** cancelled

- [ ] Spec-break: saved expansion widens Phase 2 with a sound semantic, or the proposal narrows bootstrap instead.
- [ ] Phase 1 thin-id-list can drive a correct Phase 2 **V⁺** query.
- [ ] Production measurement: when \|V⁺\| ≪ \|Workspace\|.
- [ ] Cache-first boot vs two-phase: complementary, competing, or one subsumes the other.

## Context

Report: [[../reports/two-phase-state-loading-exploration.md]]. Parent: [[plan/selective-client-loading/spec.md]].

## Comments

- 2026-09-02: Parked from WORK.md.
- 2026-09-26: Status `cancelled`. Superseded by [browser-residency](plan/browser-residency/map.md). Bootstrap is Zoom-scoped visible-closure, not a two-phase complete-Workspace load. See [02 — Lock bootstrap visible-closure set](plan/browser-residency/issues/02-lock-bootstrap-visible-closure.md) and [04 — Retire selective hollow-click and resident-only Find assumptions](plan/browser-residency/issues/04-retire-selective-hollow-click-find.md).
