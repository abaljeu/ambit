# Replace index field — wire contract (2026-08-22, corrected)

**Question:** What is the Replace wire contract — full-list only, or span-at-index still valid?

**Answer:** **Full-list only.** Wire contract is `Replace(parentId, fullOldList, fullNewList)`. Partial span Replace — including `index > 0`, zero-width insert at non-zero index, or `index = 0` with lists shorter than the parent's full children — is **not** valid on the wire. Producers that still emit span ops are **migration debt** (issue 13), not an alternate supported mode.

## Wire vs internal type

| Layer | Status |
| --- | --- |
| Wire contract | Full-list only: complete parent `oldChildren` / `newChildren` at `index = 0` until §10 field rename |
| `Op.Replace` type | `parentId * index * oldChildren * newChildren` — span fields retained for apply/replay during migration — [[../../../src/Shared/History.fs]] L8–12 |
| Wire JSON (today) | `"index"`, `"oldChildren"`, `"newChildren"` — [[../../../src/Shared/Serialization.fs]] L230–236 |
| Apply | Span CAS in [[../../../src/Shared/GraphMutate.fs]] `replace` (index bounds + old-span match) |
| Amendment | `tryAmendReplace` accepts only index-0 full-list posts — [[../../../src/Shared/ChangeAmendment.fs]] L112–123 |
| Spec | [[../details/replace-amendment.md]] §1 wire contract, §6 producer rule, §10 migration |

## `index` field

Deprecated on wire for semantics: always `0` for valid full-list posts. Target shape after §10: `oldList` / `newList` with no `index` (open decision). Decoding span from legacy logs at replay is separate from permitting span on new posts.

## Migration debt — span / partial producers (invalid wire usage today)

| Module | Pattern |
| --- | --- |
| [[../../../src/Client/UpdatePaste.fs]] | paste/remove at `range.start`; insert at `focusIdx + 1` |
| [[../../../src/Client/UpdateMove.fs]] | cross-parent move (two span Replaces) |
| [[../../../src/Client/UpdateOps.fs]] | duplicate at `sel.range.endd` |
| [[../../../src/Client/UpdateHelpers.fs]] | split insert at `insertIndex` |
| [[../../../src/Shared/ImportText.fs]] | append at `existingChildren.Length` |
| [[../../../src/Shared/FileNodeOps.fs]] | insert at computed index |
| [[../../../src/Shared/ViewModelDeleteOps.fs]] | span remove, promote, TRASH append |
| [[../../../src/Shared/ViewModelJoinOps.fs]] | remove at `indexInParent`; reparent append |
| [[../../../src/Shared/dotnet/LazyLoadReconciliation.fs]] | ref replace, trash move, reparent |
| [[../../../src/Shared/Paste.fs]] | `index = 0` but `oldChildren = []` on non-empty parents |
| [[../../../src/Shared/ChangeAmendment.fs]] | amb-conflict child insert (`oldChildren = []`) |

**Already full-list at index 0 (wire-valid shape):** [[../../../src/Client/UpdateMove.fs]] same-parent reorder; [[../../../src/Shared/ImportText.fs]] focus replace; [[../../../src/Shared/AmbleRun.fs]]; [[../../../src/Shared/documents/DocumentColdParse.fs]]; hard-delete paths in [[../../../src/Shared/ViewModelDeleteOps.fs]]; amended output in [[../../../src/Shared/ChangeAmendment.fs]].

## Implication for issue 05

Issue 05 delivered merge/amend for **wire-valid** full-list posts. Remaining gap is **producer migration** to emit only full-list Replace (issue 13), not extending amendment to `index > 0`.
