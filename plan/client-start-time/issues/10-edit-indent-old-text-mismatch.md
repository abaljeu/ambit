# 10 — Editing `returnTo` vs Poll/Load under edit+indent Tab CAS

**What to build:** Verify or fix Editing `returnTo` plus `adjustModeAfterServerApply` vs Poll/Load when the user edits, then indents with Tab. CAS must not emit `SetText` with a stale `originalText`.

**Blocked by:** None.

**Status:** ready-for-agent

- [ ] Overlay `returnTo` Editing does not keep a stale `originalText` after Poll/Load apply.
- [ ] `adjustModeAfterServerApply` tracks the node whose text changed, not a relocated focus.
- [ ] HITL: multi-select, edit focus, type one char, open command palette, wait one Poll, close, Tab; no `old text does not match`.

## Context

Report: [[../reports/edit-indent-old-text-mismatch.md]]. Seams: [[src/Client/UpdateHelpers.fs]] `tryTextCommitOps` / `adjustModeAfterServerApply`; [[src/Client/CommandPalette.fs]] `returnTo`; [[src/Client/UpdateMove.fs]] indent.

## Comments

- 2026-09-02: Parked from WORK.md Active.
