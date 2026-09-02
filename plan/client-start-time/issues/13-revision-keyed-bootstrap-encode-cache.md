# 13 — Server revision-keyed bootstrap encode cache

**What to build:** Server revision-keyed bootstrap encode cache for warm F5 at unchanged revision. Do not skip `/state` without that cache. Reconcile with product spec user story 11 (refresh is a new residency session).

**Blocked by:** None.

**Status:** ready-for-agent

- [ ] Unchanged revision reuses a revision-keyed encoded `/state` body.
- [ ] A new revision encodes again.

## Context

Report: [[../reports/reload-state-reuse-investigation.md]]. Artifacts: [[src/Server/Api.fs]], [[src/Shared/ResidentProjection.fs]]. Parent: [[../reports/state-further-optimization.md]].

## Comments

- 2026-09-02: Parked from WORK.md.
