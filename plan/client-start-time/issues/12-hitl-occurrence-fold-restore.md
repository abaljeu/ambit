# 12 — HITL occurrence-based fold restore and Zoom fallback

**What to build:** HITL only. Warm F5 with occurrence-based `f` restore. Legacy `e` collapses safely. Zoom fallback when the preferred Node is absent after replay.

**Blocked by:** None.

**Status:** ready-for-human

- [ ] Warm F5 restores folds from occurrence snapshots (`f`).
- [ ] Legacy `e` payloads restore collapsed.
- [ ] Zoom falls back when the preferred Node is absent after replay.

## Context

Report: [[../reports/page-not-responding-loading.md]]. Artifacts: [[../reports/restore-fold-occurrences.md]], [[src/Shared/ViewModelSiteMap.fs]], [[src/Shared/ViewModelOccurrence.fs]], [[src/Client/SessionState.fs]], [[src/Client/UpdateHelpers.fs]]. Distinct from [[11-hitl-cold-load-loading-hang.md]].

## Comments

- 2026-09-02: Parked from WORK.md.
