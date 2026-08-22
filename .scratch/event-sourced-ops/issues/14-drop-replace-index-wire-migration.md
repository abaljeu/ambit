# 14 — Drop Replace index (§10 wire migration)

**Context:** [[../details/replace-amendment.md]] §10 tracks two wire migrations: producer full-list emission (issue 13) and dropping the `index` field from `Op.Replace` and JSON. Issue 13 migrated planners to full-list Replace via [[../../../src/Shared/ChildListWire.fs]] but left `Op.Replace(parentId, index, oldChildren, newChildren)` and optional `"index"` on the wire. This ticket completes §10: remove `index` from the type and encode/decode, drop the legacy shim, and close the `oldList`/`newList` JSON rename as not planned.

**What to build:** `Op.Replace(parentId, oldChildren, newChildren)` only on the type and wire. Encode omits `"index"`. Decode requires `parentId`, `oldChildren`, `newChildren` — no backward-compat `"index"` read. Apply, undo, and invert call `Graph.replace parentId 0 oldChildren newChildren`. Retain `oldChildren` / `newChildren` JSON field names (rename to `oldList` / `newList` not planned). `GraphMutate.replace` keeps an internal splice `index` for non-wire apply paths.

**Blocked by:** 13 — Migrate producers to full-list Replace wire shape

**See also:** [[../details/replace-amendment.md]] §1, §10, [[../reports/wire-full-list-replace-contract.md]], [[../reports/14-drop-replace-index-wire-migration-build.md]]

**Status:** done

- [x] `Op.Replace` has no `index` field — `Replace(parentId, oldChildren, newChildren)`.
- [x] JSON encode omits `"index"`; decode requires current shape only (legacy `"index"` shim removed).
- [x] Apply, undo, invert, and amendment paths use `Graph.replace parentId 0 oldChildren newChildren`.
- [x] JSON field rename `oldChildren` / `newChildren` → `oldList` / `newList` closed as not planned.
- [x] Focused tests updated; `Shared.Tests` and `StateEndpointTests` green.
