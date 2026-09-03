# 14 — Defer fold restore until after first paint

**What to build:** Defer `restoreFoldOccurrences` to after first paint. First render is collapsed SiteMap only. Lower priority after HITL restore of 8 ms / 18 rows.

**Blocked by:** None.

**Status:** ready-for-agent

- [ ] First outline paint uses collapsed SiteMap from `buildSiteMapFrom` only.
- [ ] Fold restore runs after that paint.

## Context

Report: [[../reports/bucket-3-post-state-work.md]]. Artifacts: [[src/Client/App.fs]], [[src/Client/SessionState.fs]]. HITL rank: [[../reports/production-hitl-after-deploy.md]]. Client-only defer is not a substitute for two-phase fetch ([[plan/selective-client-loading/issues/29-validate-two-phase-state-loading.md]]).

## Comments

- 2026-09-02: Parked from WORK.md.
