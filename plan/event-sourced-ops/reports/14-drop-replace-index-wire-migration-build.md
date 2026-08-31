# §10 build — drop `index` from `Op.Replace`

**Date:** 2026-08-22  
**Branch:** `w/event-sourced-ops`  
**Issue:** [[../issues/14-drop-replace-index-wire-migration.md]]  
**Spec:** [[../details/replace-amendment.md]] §10

## Summary

Migrated `Op.Replace` from `Replace(parentId, index, oldChildren, newChildren)` to **`Replace(parentId, oldChildren, newChildren)`** (full-list wire shape). JSON encode no longer emits `"index"`. Apply/undo/invert use `Graph.replace parentId 0 oldChildren newChildren`.

## Apply semantics

| Case | Behavior |
| --- | --- |
| Full-list CAS | `oldChildren` equals live parent children → set to `newChildren` |
| Prepend (legacy test pattern) | `oldChildren = []`, `newChildren` non-empty → span prepend at 0 via `Graph.replace` |
| Amendment | `ChangeAmendment.tryAmendReplace` — no `index <> 0` guard; full-list only |
| Ownership validation | `History.validateOwnershipForChange` and `DocumentOpImpact` diff **introduced/removed** occurrences only |

`GraphMutate.replace` retains internal `index` parameter; only `Op` and wire dropped the field.

## Legacy shim

**Added (minimal, later removed):** `Serialization.decodeOp` initially read optional `"index"` and discarded it.  
**Removed (2026-08-22):** no backward-compat `"index"` on Replace decode — current wire shape only. See [[14-drop-replace-index-wire-migration-no-legacy-shim.md]].  
**Not added:** upgrade of span/partial logs to full-list at decode — no graph context; would need one-time log migration.

## Tests

| Suite | Result |
| --- | --- |
| `Shared.Tests` | **1350 passed**, 1 skipped |
| `StateEndpointTests` | **66 passed** |
| Build | `Gambol.sln` clean |

## Key source files

- [[../../../src/Shared/History.fs]] — type, apply/undo/invert, introduced-only validation
- [[../../../src/Shared/Serialization.fs]] — encode/decode
- [[../../../src/Shared/ChildListWire.fs]] — builders (no `index = 0`)
- [[../../../src/Shared/ChangeAmendment.fs]] — `tryAmendReplace`
- [[../../../src/Shared/DocumentOpImpact.fs]] — delta-based touch ids
- [[../../../src/Shared/AmbleRun.fs]], [[../../../src/Shared/documents/DocumentColdParse.fs]]
- [[../../../src/Server/DatabaseProjection.fs]] — pattern arity
- Tests: span fixtures → `ChildListWire` or full-list `Op.Replace`

## Files changed (this slice)

~**50** tracked paths (13 `src/` including `ChildListWire.fs`, 37 `tests/`, 2 `plan/` spec/report updates). Count includes issue-13 producer work on the same branch.

## Not in scope

- JSON field rename `oldChildren` → `oldList` — **closed, not planned** ([[14-drop-replace-index-wire-migration-no-legacy-shim.md]])
- `GraphMutate.replace` span removal (internal only)
- Issue 10 interleaving polish
