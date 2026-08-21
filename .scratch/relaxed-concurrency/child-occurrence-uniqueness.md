# Child occurrence uniqueness within a parent

## Bottom line

**Yes — a `NodeId` can appear more than once under a single parent's `children` list today**, and the codebase treats that as intentional for reference links (Owner + Ref, or additional Ref siblings). There is **no validation** that forbids duplicate ids within one parent's list. **`Graph.replace` does not check id uniqueness**; it matches `oldChildren` by **exact `ChildNode` equality at `index`**. Global ownership validation (`History.validateOwnership*`) requires exactly one **Owner** edge per id graph-wide, but allows any number of **Ref** edges, including multiple under the same parent. **Forbidding duplicate ids within a parent would break the user-facing "Duplicate (link)" command** and AmbDocument's shared-node serialization (Owner then Ref under one parent). Matching Replace spans by id-run alone would already be ambiguous for the supported Owner+Ref case unless matching uses full `ChildNode` (id + `ref`) or index.

---

## 1. Existing validation for duplicate ids within one parent's `children`

**No.** Nothing rejects or prevents the same `NodeId` appearing twice in one parent's `children` list based on id alone.

### `Graph.replace` (`GraphMutate.fs`)

- Validates index bounds, that introduced nodes exist, placement rules, and name conflicts — not sibling id uniqueness (`171:353:src/Shared/GraphMutate.fs`).
- Confirms the op by **structural equality** of the span: `existing <> oldChildren` where `existing` is the slice at `index` (`241:247:src/Shared/GraphMutate.fs`). Same id with different `ref` is a different `ChildNode`.
- Under ROOT, system folders must appear **exactly once as Owner** per folder id — a per-id Owner count check, not a general sibling-id uniqueness rule (`284:299:src/Shared/GraphMutate.fs`).

### `History.validateOwnership*` (`History.fs`)

- **`validateOwnershipSemantics`** collects all `(parentId, child)` pairs, builds `ownerByChildId` from **Owner edges only**, and errors when a child id has **≠ 1 Owner parent** globally (`376:453:src/Shared/History.fs`). Ref siblings under the same parent are ignored here.
- Full-graph pass also checks **duplicate artifact names** (not duplicate ids) via `GraphQuery.tryFindArtifactNameDuplicate` (`540:548:src/Shared/History.fs`).
- **`validateOwnershipForChange`** scopes the same semantics to ids touched by shape ops; Replace placement checks artifact **name** conflict, not id uniqueness (`583:627:src/Shared/History.fs`).

### `GraphQuery.fs`

- `artifactNameConflict` / `siblingOwnedNameConflict` gate duplicate **owned names**, not duplicate child ids (`224:275:src/Shared/GraphQuery.fs`, `311:339:src/Shared/GraphQuery.fs`).
- `tryFindParentAndIndex` uses `parentByChild`, which stores **at most one** `(parent, index)` per child id graph-wide — it does not validate or repair duplicate list entries (`341:342:src/Shared/GraphQuery.fs`).

### `GraphBuild` / indexes (`GraphBuild.fs`)

- `addStructuralEdges` skips adding to `parentByChild` when the child id is **already a map key anywhere** — so a second list slot with the same id is **not indexed**, but the duplicate remains in `node.children` (`54:59:src/Shared/GraphBuild.fs`).
- Comment on `appendChildren`: duplicate handling in indexes is global/min-parent, not per-parent uniqueness (`306:307:src/Shared/GraphBuild.fs`).

### Client "Check graph" (`UpdateValidateGraph.fs`)

- Delegates to `History.validateOwnershipLocated` only (`7:13:src/Client/UpdateValidateGraph.fs`) — same global Owner rules, no per-parent id uniqueness.

### `ProjectionOwnershipRepair` (DB repair)

- Validates **global** unique Owner per survivor id, not uniqueness within a parent's child list (`288:307:src/Shared/ProjectionOwnershipRepair.fs`).

### Model comment vs per-parent uniqueness

- `Model.fs` documents global Owner uniqueness: "For each id:NodeId exactly one will have ref: Owner" (`35:38:src/Shared/Model.fs`). That permits multiple Ref entries (and Owner+Ref under one parent).

---

## 2. Code paths that can produce duplicate ids within one parent

**Yes.** Multiple producers can create duplicate ids under a single parent today.

### Explicit graph mutation tests

- `Graph.replace` accepts `[ ChildNode.owner shared; ChildNode.reference shared ]` under one parent (`135:154:tests/Shared.Tests/ModelTests.fs`).

### "Duplicate (link)" — primary user path

- `duplicateSelectionOp` maps selection to `{ child with ref = Ownership.Ref }` and inserts at `sel.range.endd` via `Op.Replace(..., [], duplicatedRefs)` (`421:433:src/Client/UpdateOps.fs`).
- Command registered as `DupNodes` / "Duplicate (link)" (`236:236:src/Shared/CommandEntry.fs`, `222:222:src/Client/Commands.fs`).
- `History.applyChange` tests accept same-parent Owner then Ref, and mid-list Ref insert (`862:900:tests/Shared.Tests/HistoryTests.fs`).

### Document parse / import

- **`AmbDocument.read`**: each `-> …` line calls `prependChild parentId (ChildNode.reference nodeId)` with no per-parent dedup (`538:547:src/Shared/documents/AmbDocument.fs`, prepend at `339:345:src/Shared/documents/AmbDocument.fs`).
- **`DocumentColdParse.planOpsFromGraphs`**: emits `Op.Replace(nodeId, 0, remainingOld, newChildren)` from parsed child lists; `outlineWithoutDupRefs` only strips Ref edges that duplicate **preserved owned File/Directory stubs**, not general ref duplicates (`101:112:src/Shared/documents/DocumentColdParse.fs`, `204:217:src/Shared/documents/DocumentColdParse.fs`).
- **`DocumentParseOps.planApplyArtifact`**: cold/warm paths delegate to `DocumentColdParse` / warm reconcile → same Replace planning (`10:34:src/Shared/dotnet/DocumentParseOps.fs`).

### `FileNodeOps`

- `planInsertFileRefAtFocus` skips insert only when a Ref to the same id is **already at the same index**; a second Ref at another index would still insert (`68:78:src/Shared/FileNodeOps.fs`, idempotent-at-index test `284:292:tests/Shared.Tests/FileNodeOpsTests.fs`).

### `AmbleRun`

- `planReplaceFromSpecs` appends `ChildNode.reference node.id` for each `RefSpec` with no dedup — repeated refs to the same node in one eval produce duplicate ids in the Replace child list (`58:71:src/Shared/AmbleRun.fs`).

### `Paste` / client paste

- `buildPasteOps` assigns fresh ids (no duplicate ids) (`29:49:src/Shared/Paste.fs`).
- `UpdatePaste.fs` can insert **links** to existing nodes via Replace (refs); multiple paste-link ops could repeat an id at different indices (client Replace sites: `85:109:src/Client/UpdatePaste.fs`).

### `LazyLoadReconciliation`

- Does not create duplicate refs under one parent by default; **`refReplacementOps`** iterates **all Ref occurrences** (including multiple under one parent if present) and replaces by `(parentId, index)` (`74:85:src/Shared/dotnet/LazyLoadReconciliation.fs`) — evidence the system expects multiple indexed occurrences per id.

### `Snapshot.read`

- Each `-> #…` line prepends a Ref; no sibling dedup (`278:282:src/Shared/Snapshot.fs`, `57:59:src/Shared/documents/DocumentOutlineOps.fs`).

### What is blocked

- A **second Owner** edge for an already-owned id is rejected by `History.applyChange` / `validateOwnershipSemantics` (`981:995:tests/Shared.Tests/HistoryTests.fs`, `436:453:src/Shared/History.fs`). So **Owner+Owner** same id under one parent is not producible through validated apply; **Owner+Ref** and **Ref+Ref** are.

---

## 3. Tests constructing or asserting duplicate ids under one parent

| Test | What it shows | Evidence |
|------|----------------|----------|
| `Replace can insert duplicate id with owner then ref` | `Graph.replace` + `assertValidOwnership` accept Owner+Ref same id same parent | `135:154:tests/Shared.Tests/ModelTests.fs` |
| `applyChange accepts same-parent Owner then Ref (Duplicate link)` | End-to-end Duplicate link on root | `862:880:tests/Shared.Tests/HistoryTests.fs` |
| `applyChange accepts mid-list same-parent Ref (Duplicate link)` | Ref duplicate mid-list | `884:900:tests/Shared.Tests/HistoryTests.fs` |
| `Duplicate Ref succeeds despite dual-Owned Replace parent` | Ref insert under parent even when graph has other ownership dirt | `926:941:tests/Shared.Tests/HistoryTests.fs` |
| `duplicate root child keeps both rows when canonicals insert` | DB repair plan keeps **Owner + Ref** same id under ROOT | `270:289:tests/Shared.Tests/ProjectionOwnershipRepairTests.fs` |
| `childOwnership follows edge.ref even when Node.owner matches parent` | Same id: edge `ref` distinguishes Owner vs Ref | `852:859:tests/Shared.Tests/HistoryTests.fs` |

**Not found:** a dedicated test for **two Ref siblings with the same id under one parent**, but nothing in parse/link/plan code prevents it.

**Related (different parents):** `createSharedNodeGraph`, SiteMap tests for same id under A and B (`78:92:src/Shared/ModelBuilder.fs`, `1470:1501:tests/Shared.Tests/ViewModelTests.fs`).

---

## 4. User-facing semantics requiring duplicate ids under one parent

**Yes — by design.**

### Duplicate (link)

- Inserts additional **Ref** edges beside existing children for the same `NodeId` (`421:433:src/Client/UpdateOps.fs`). Documented in model: "same-parent Refs (Duplicate link)" (`196:198:src/Shared/Model.fs`).

### AmbDocument write (shared / multi-occurrence nodes)

- When a node is shared (`occurrenceCount > 1`) and an Owner line was already emitted under a parent, a **second appearance under that same parent** is written as a **`->` ref line**, not a second Owner (`211:242:src/Shared/documents/AmbDocument.fs`). Round-tripping can yield **Owner + Ref** under one parent.

### AmbDocument / Snapshot read

- Each `->` line adds another `ChildNode.reference` (`538:547:src/Shared/documents/AmbDocument.fs`). Two lines pointing at the same stable id → two Ref siblings (no dedup in `prependChild`).

### UI / site map

- Site map reconciliation explicitly handles **duplicate refs** under one parent via positional fallback and distinct `instanceId`s (`90:93:src/Shared/ViewModelSiteMap.fs`).
- Selection uses **instanceId**, not NodeId alone, because duplicate NodeIds are distinct positions (`237:237:src/Shared/ViewModelSelection.fs`).
- Row highlight prevents **sibling occurrences of the same NodeId (DIGRAPH links)** from all selecting together (`13:13:src/Shared/ViewModelRowState.fs`).

### Wikilinks / `[[…]]`

- Lazy-load ref replacement creates a **new** node with text `[[path]]` as **Owner**, not a second edge to an existing id (`79:85:src/Shared/dotnet/LazyLoadReconciliation.fs`) — different mechanism from Duplicate link.

### Document corruption check (not sibling dup)

- `readAllDocuments duplicate id corruption` errors on **conflicting stable-id membership across documents**, not duplicate sibling refs (`837:850:tests/Server.Tests/DocumentPersistenceTests.fs`).

---

## 5. Can `ChildNode` distinguish two same-id siblings under one parent?

**Only via `ref` (Owner vs Ref), and via list index / site-map `instanceId` — not via any other field.**

### Fields

```36:44:src/Shared/Model.fs
type ChildNode =
    { ref: Ownership
      id: NodeId }
    ...
    static member reference (id: NodeId) : ChildNode =
        { ref = Ownership.Ref; id = id }
```

No occurrence id, ordinal key, or third discriminator.

### Owner + Ref, same id, same parent — permitted

- Tested directly (`135:154:tests/Shared.Tests/ModelTests.fs`, `862:880:tests/Shared.Tests/HistoryTests.fs`).
- `Node.childOwnership` returns **`child.ref`**; comment: same-parent Refs stay Ref even when `Node.owner` matches parent (`196:204:src/Shared/Model.fs`).

### Ref + Ref, same id, same parent — structurally identical `ChildNode` records

- Both are `{ ref = Ref; id = same }`. The only distinction is **index in `parent.children`** and **site-map `instanceId`** (`90:119:src/Shared/ViewModelSiteMap.fs`).

### Owner + Owner, same id, same parent — not allowed (globally)

- Second Owner edge fails validation (`981:995:tests/Shared.Tests/HistoryTests.fs`).

### Index maps

- `parentByChild` retains **one** entry per id (first structural edge wins globally), so it **cannot** distinguish two same-id siblings under one parent (`54:59:src/Shared/GraphBuild.fs`). Occurrence-aware code uses `getAllOccurrences` / site map instead (`12:22:src/Shared/ViewModelOccurrence.fs`).

---

## Riskiest code paths if per-parent id uniqueness were enforced in `Graph.replace`

1. **`duplicateSelectionOp` / DupNodes** — core "Duplicate (link)" inserts Ref copies with the same ids beside the selection (`421:433:src/Client/UpdateOps.fs`). Would fail immediately for any selection.

2. **`AmbDocument` read/write round-trip** — write emits Owner then ref line for shared nodes under the same parent (`233:242:src/Shared/documents/AmbDocument.fs`); read prepends one Ref per `->` line (`538:547:src/Shared/documents/AmbDocument.fs`). Cold/warm parse applies full child-list Replace (`204:217:src/Shared/documents/DocumentColdParse.fs`).

3. **Id-run Replace matching (proposed)** — already ambiguous today for **Owner+Ref** pairs sharing an id; id-only matching cannot locate spans uniquely without also using `ref` or index.

4. **`AmbleRun` / shell ref lists** — multiple `RefSpec` to one node in a single Replace (`58:71:src/Shared/AmbleRun.fs`).

5. **Site map / delete / lazy-load** — code assumes multiple occurrences per id indexed by `(parentId, index)` (`74:85:src/Shared/dotnet/LazyLoadReconciliation.fs`, `12:22:src/Shared/ViewModelOccurrence.fs`); enforcement would need coordinated UI and op-planning changes, not `Graph.replace` alone.
