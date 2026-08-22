# Wire contract — full-list Replace only

**Date:** 2026-08-22
**Branch:** `w/event-sourced-ops`
**Artifacts:** [[../details/replace-amendment.md]] §1, §6, §10; [[../details/conflict-resolution.md]] Kind 3; [[../issues/13-migrate-producers-full-list-replace-wire.md]]

## Contract (documented)

| Aspect | Rule |
| --- | --- |
| Wire shape | `Replace(parentId, fullOldList, fullNewList)` — complete parent child lists at the Actor's common prior |
| Partial span | **Not** on the wire — no `index > 0`, no zero-width insert at non-zero index, no `index = 0` with partial lists |
| `index` field | Deprecated for semantics; always `0` on valid posts until §10 drops it |
| JSON today | `"index"`, `"oldChildren"`, `"newChildren"` — [[../../../src/Shared/Serialization.fs]] L230–236 |
| Target JSON (§10, open) | `"oldList"`, `"newList"` — no `index` |
| Internal `Op` type | `Replace(parentId, index, oldChildren, newChildren)` retained for apply, undo, legacy log replay |
| Amendment (issue 05) | `tryAmendReplace` — index-0 full-list only — [[../../../src/Shared/ChangeAmendment.fs]] L102–123 |

## Migration debt — producers still emitting invalid wire shape

### Client

| File | Span / partial pattern |
| --- | --- |
| [[../../../src/Client/UpdatePaste.fs]] | Remove at `range.start`; insert at `focusIdx + 1`; select-mode range replace |
| [[../../../src/Client/UpdateMove.fs]] | Cross-parent: remove at `from.start`, insert at `to.endd` |
| [[../../../src/Client/UpdateOps.fs]] | Duplicate insert at `sel.range.endd` |
| [[../../../src/Client/UpdateHelpers.fs]] | Split: insert at `insertIndex` |

### Shared

| File | Span / partial pattern |
| --- | --- |
| [[../../../src/Shared/ImportText.fs]] | Directory merge append at `existingChildren.Length` |
| [[../../../src/Shared/FileNodeOps.fs]] | Create/insert at computed index |
| [[../../../src/Shared/ViewModelDeleteOps.fs]] | Span remove, promote, TRASH append, batch remove |
| [[../../../src/Shared/ViewModelJoinOps.fs]] | Remove at `indexInParent`; reparent append |
| [[../../../src/Shared/dotnet/LazyLoadReconciliation.fs]] | Ref replace, trash move, disk reparent |
| [[../../../src/Shared/Paste.fs]] | `index = 0`, `oldChildren = []` on parents that may be non-empty |
| [[../../../src/Shared/ChangeAmendment.fs]] | Amb-conflict child: `oldChildren = []` |

### Already wire-valid (full-list at index 0)

| File | Notes |
| --- | --- |
| [[../../../src/Client/UpdateMove.fs]] L112 | Same-parent reorder |
| [[../../../src/Shared/ImportText.fs]] L79 | Focus replace with full `existingChildren` |
| [[../../../src/Shared/AmbleRun.fs]] | `replaceAllChildrenOp` |
| [[../../../src/Shared/documents/DocumentColdParse.fs]] | Parse reconcile |
| [[../../../src/Shared/ViewModelDeleteOps.fs]] | Hard-delete full-list paths |
| [[../../../src/Shared/ChangeAmendment.fs]] L123 | Amended `current → target` |

## Issue tracker

New issue **13** — [[../issues/13-migrate-producers-full-list-replace-wire.md]]. Issues 06–12 do not cover producer migration; §10 covers wire field rename only.

## Board mutation (for root)

- **add** — [[../issues/13-migrate-producers-full-list-replace-wire.md]] to Pending in [[../../../WORK.md]]

## Not in scope (this report)

- Client producer implementation (issue 13)
- §10 JSON field rename / legacy log upgrade shim
- Issue 10 order polish
