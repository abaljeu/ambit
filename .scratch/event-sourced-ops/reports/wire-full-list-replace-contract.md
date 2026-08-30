# Wire contract — full-list Replace only

**Date:** 2026-08-22
**Branch:** `w/event-sourced-ops`
**Artifacts:** [[../details/replace-amendment.md]] §1, §6, §10; [[../details/conflict-resolution.md]] Kind 3; [[../issues/13-migrate-producers-full-list-replace-wire.md]]; [[../issues/14-drop-replace-index-wire-migration.md]]; [[14-drop-replace-index-wire-migration-build.md]]

## Contract (documented)

| Aspect | Rule |
| --- | --- |
| Wire shape | `Replace(parentId, fullOldList, fullNewList)` — complete parent child lists at the Actor's common prior |
| Partial span | **Not** on the wire — no non-zero splice index, no zero-width insert at non-zero index, no partial lists |
| `index` field | **Dropped** (§10, 2026-08-22) — encode omits `"index"`; decode requires current shape only (no legacy shim) |
| JSON fields | `"oldChildren"`, `"newChildren"` — retained; rename to `oldList`/`newList` **not planned** — [[../../../src/Shared/Serialization.fs]] |
| Internal `Op` type | `Replace(parentId, oldChildren, newChildren)` — apply/undo call `Graph.replace parentId 0 ...` |
| Amendment (issue 05) | `tryAmendReplace` — full-list only — [[../../../src/Shared/ChangeAmendment.fs]] |

## Producer migration (issue 13 — done)

All Client/Shared Change planners emit full-list Replace via [[../../../src/Shared/ChildListWire.fs]]. Build: [[13-migrate-producers-full-list-replace-build.md]].

## Issue tracker

- Issue **13** — producer migration — **done** — [[../issues/13-migrate-producers-full-list-replace-wire.md]]
- Issue **14** — §10 drop `index` from type and wire — **done** — [[../issues/14-drop-replace-index-wire-migration.md]]; build [[14-drop-replace-index-wire-migration-build.md]]
- §10 rename `oldChildren`/`newChildren` → `oldList`/`newList` — **closed, not planned** — [[14-drop-replace-index-wire-migration-no-legacy-shim.md]]
- Legacy `"index"` decode shim — **removed** — [[14-drop-replace-index-wire-migration-no-legacy-shim.md]]

## Not in scope (this report)

- One-time upgrade of span/partial legacy logs at decode
- Issue 10 order polish
