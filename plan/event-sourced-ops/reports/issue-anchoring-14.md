# Issue anchoring — §10 drop `index` wire migration

**Date:** 2026-08-22

## Decision

**Issue 14** — [[../issues/14-drop-replace-index-wire-migration.md]] — anchors the §10 wire/type migration. Work is **done**; no `WORK.md` Pending entry.

## Rationale (not issue 13)

| Slice | Scope | Issue |
| --- | --- | --- |
| Producer migration | Planners emit full-list Replace (`index = 0`, complete parent lists) via `ChildListWire` | **13** — done |
| Wire/type migration | Drop `index` from `Op.Replace` and JSON; remove legacy decode shim; close `oldList`/`newList` rename | **14** — done |

Issue 13 acceptance criteria covered producer emission only. §10 in [[../details/replace-amendment.md]] explicitly listed dropping `index` as a separate migration row. Different acceptance criteria, different reports, and a clear dependency (14 blocked by 13) justify a distinct ticket rather than retroactively extending issue 13.

## Files

| Action | Path |
| --- | --- |
| Created | [[../issues/14-drop-replace-index-wire-migration.md]] |
| Renamed | `14-drop-replace-index-build.md` → [[14-drop-replace-index-wire-migration-build.md]] |
| Renamed | `14-follow-up.md` → [[14-drop-replace-index-wire-migration-follow-up.md]] |
| Renamed | `14-no-legacy-shim-decision.md` → [[14-drop-replace-index-wire-migration-no-legacy-shim.md]] |
| Updated | [[wire-full-list-replace-contract.md]], [[../details/replace-amendment.md]] §10, [[../project.md]] |

## Commits

None (per instruction).
