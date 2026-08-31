# To-design report — Relaxed concurrency

Date: 2026-08-19

## Deliverable

Created [[design.md]] — design pass between spec and tickets using codebase-design vocabulary (module, interface, seam, depth, adapter, leverage, locality).

## Inputs consumed

- [[spec.md]], [[map.md]], [[replace-span-cas-feasibility.md]]
- [[docs/agents/issue-tracker.md]] — conventions only; no issue files written
- Source: `FileAgent.applyBatch` (revision gate ~150), `DbAgent.applyBatch` (~108), `History.applyChange`, `GraphMutate` CAS, `StateEndpointTests.fs` (`POST with wrong base revision returns 400`)

## Design conclusions

- **Primary seam confirmed:** POST `/ambit/changes` integration tests (parameterized file/db) — interface is the test surface; deletion test passes (gate was pass-through).
- **Change is subtractive:** one branch removed per server agent; Shared stays deep and untouched.
- **Three ticket slices:** (1) file agent + initial tests, (2) db parity, (3) structural concurrency scenarios + obsolete test rewrite.

## Files changed

| File | Action |
|------|--------|
| `.scratch/relaxed-concurrency/design.md` | Created |
| `.scratch/relaxed-concurrency/to-design-report.md` | Created (this file) |
| `.scratch/relaxed-concurrency/spec.md` | Added Further Notes wikilink to design.md |
| `.scratch/relaxed-concurrency/spec.md.md` | Deleted (empty junk) |
| `WORK.md` | Linked design.md from pending entry |

## Not changed (per instructions)

- `spec.md` Status — remains `ready-for-agent`
- `project.md` Stage — remains `spec`
- No `/to-tickets`; no issue files under `issues/`
