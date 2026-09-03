# 01 — Keep Current on rediscovered Added

**Context:** Browser Load Workspace runs directory reconcile, which maps every on-disk path to `Added` and then demotes resolved Files from `Current` to `Unparsed`. Parse persists `Current` on the Server; Load should not wipe it for unchanged rediscovered paths.

**What to build:** In `LazyLoadReconciliationApply.addedStubIds`, gate `resolvedStubIds` so only `NoServerFile` nodes are demoted. Created stubs still become `Unparsed`. Already-`Current` (and already-`Unparsed`) rediscovered files keep their state. Modified-path demotion stays unchanged.

**Blocked by:** None — can start immediately.

**See also:** Plan `fix_load_demotes_parse_8d40752b`; [[src/Shared/dotnet/LazyLoadReconciliationApply.fs]], [[tests/Shared.Tests/LazyLoadReconciliationTests.fs]].

**Status:** in-progress

- [x] Shared regression: plan-add `note.txt`, promote to `Current`, re-plan same path as Added; id and `documentState` stay `Current` (children preserved).
- [x] `resolvedStubIds` in `addedStubIds` includes only `NoServerFile` resolved stubs.
- [x] Existing coverage still: new Added → Unparsed; Modified Current → Unparsed.
- [x] Filtered `FullyQualifiedName~LazyLoadReconciliationTests` green.
- [ ] HITL (user): Parse → Current → Load Workspace → stays Current.

## Comments

- 2026-09-02: Parked from WORK.md Active. Outcome already on this issue: keep Current when Load Workspace rediscovers Added path; demote only new stubs / NoServerFile.
