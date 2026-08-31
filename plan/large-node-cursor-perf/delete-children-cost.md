# Delete-children operational cost

Date: 2026-08-17  
Parent: [[investigation.md]], [[implement-fix.md]], [[further-speedups.md]]  
Scope: read-only cost analysis (no fixes). Stress case = one expanded parent with **N** visible children.

**Branch note:** `status.sh` reported `selective-client-sync` (ahead 7). Selection-only `planPatchDOM` / `childIndex` code is present in tree; analysis assumes that baseline.

---

## Executive summary

| Scenario | Graph / ops | SiteMap | View / DOM |
| --- | --- | --- | --- |
| Delete **one** among **N** siblings (typical MoveToTrash / Ref-only) | Classify **O(E)**; mid-list `Replace` → **full `fromNodes` O(E)**; TRASH append cheap | Reconcile expanded parent: often **O(N²)** sibling rematch + **O(S log S)** parent index | Full `planPatchDOM` + structural `patchDOM` walk **O(V)**; **1** `RemoveRow` |
| Delete **parent** of **N** (subtree stays under moved node) | Same pattern: one span remove + trash append; **children list of deleted node untouched** | Drop parent (+ visible descendants) from site map; **N** disappears from **view**, not from graph | Structural plan/apply **O(V)**; many `RemoveRow`s if parent was expanded |
| Contiguous multi-delete **K** of **N** | One span `Replace`; classify **O(K·E)** | Same shift rematch on remaining **N−K** | **K** removals + full visible walk |
| Hard-delete in TRASH (subtree size **S**) | `getAllOccurrences` × **S** → **O(S·E)**; each non-append `Replace` → **fromNodes** | Reconcile only expanded paths | Same structural DOM path |
| Collapse (fold) vs delete | **No** graph ops | `toggleFold` **O(1)** map update | Still **new** `siteMap` → **no** selection fast path; **O(V)** plan + structural apply |

**Selection-only fast path does not help deletes.** It requires `obj.ReferenceEquals` on both `siteMap` and `graph` ([[ViewModelDomPlan.fs]] `canUseSelectionFastPath`). Delete always replaces both.

---

## Command path (user Delete)

1. Key → `Commands.fs` → `deleteSelectionOp` ([[UpdateOps.fs]] ~455–497).
2. `ViewModelDeleteOps.classifyDeleteForSelection` → `planDeleteOps`.
3. `applyAndPost` → `SyncLogic.applyLocalChange` → `Change.apply` (each `Op`).
4. `withSiteMap` → `reconcileSiteMapFrom` ([[UpdateHelpers.fs]] ~359–372).
5. Dispatch → `patchDOM` → `planPatchDOM` ([[View.fs]] ~58–164).

There is **no** separate “delete all children of X” command. User delete is always a **contiguous** `SiteNodeRange` under one parent. Non-contiguous bulk is only in warm-parse `planDeleteDroppedOwnedMany` (not the Delete key).

---

## 1. Classify + plan ops

### Contiguous selection delete

```23:84:src/Shared/ViewModelDeleteOps.fs
    let classifyDeleteForSelection
        (graph: Graph)
        (range: SiteNodeRange)
        : ClassifiedDelete list
        =
        ...
            let ownerOcc = getOwnerOccurrence graph nodeId
            ...
            let others = occurrencesOutsideSelection graph range nodeId
```

Per selected child:

- `getAllOccurrences` scans **every** parent’s children list ([[ViewModelOccurrence.fs]] 12–22) → **O(E)** edges.
- Called again via `getOwnerOccurrence` / `occurrencesOutsideSelection` / `isOwnerUnderTrash`.

**Cost for K selected:** **O(K·E)**. Single-child delete: **O(E)** (independent of sibling count **N**, but scales with whole graph).

### Planned ops (`planDeleteOps`)

```226:241:src/Shared/ViewModelDeleteOps.fs
    let planDeleteOps ... =
        ...
        let spanRemove = Op.Replace(parentId, range.start, selectedChildren, [])
        ...
        promoteOps @ [ spanRemove ] @ trashRenames @ trashOps @ hardDeleteOps
```

| Action | Typical ops |
| --- | --- |
| `LocalDeleteRefOnly` | Span remove only |
| `MoveToTrash` | Span remove + optional `SetName` + TRASH **append** `Replace` |
| `LocalDeleteWithPromotion` | Promote `Replace` elsewhere + span remove |
| `HardDeleteSubtreeInTrash` | Span remove + per-parent full-list `Replace`s from `hardDeleteSubtreePlan` |

**Important:** Moving a parent of **N** children to TRASH does **not** emit **N** child deletes. The owned subtree stays under that node; only the **edge under the selection parent** is removed (plus TRASH append).

### Hard-delete plan hotspot

```88:120:src/Shared/ViewModelDeleteOps.fs
    let hardDeleteSubtreePlan (graph: Graph) (rootNodeId: NodeId) : Map<NodeId, int list> =
        ...
        let subtreeNodes = collectSubtree [ rootNodeId ] Set.empty
        ...
                getAllOccurrences graph nid
```

For subtree size **S**: **O(S·E)** occurrence scans, then one `Replace(pid, 0, children, remaining)` per affected parent (each mid/full rewrite → `fromNodes`, below).

### Warm-parse bulk (not Delete key)

`planDeleteDroppedOwnedMany`: one `Replace` **per dropped index** (descending), shared TRASH append. Cost multiplies mid-list `fromNodes` by drop count.

---

## 2. Graph mutation (`Op.Replace`)

```163:174:src/Shared/History.fs
        | Op.Replace(parentId, index, oldChildren, newChildren) ->
            match Graph.replace parentId index oldChildren newChildren state.graph with
```

```225:251:src/Shared/GraphMutate.fs
                let isAppend = oldCount = 0 && index = childCount
                let commit (updatedChildren: ChildNode list) =
                    ...
                    if isAppend then
                        GraphBuild.appendChildren ...
                    else
                        GraphBuild.fromNodes
                            graph.root
                            (graph.nodes |> Map.add parentId updatedParent)
                ...
                        let prefix = children |> List.take index
                        let suffix = children |> List.skip (index + oldCount)
                        let updatedChildren = prefix @ newChildren @ suffix
```

| Case | List splice | Index rebuild |
| --- | --- | --- |
| Span remove among **N** siblings | `take`/`skip`/`@` → **O(N)** | **`fromNodes` → O(E)** (all nodes’ children) |
| TRASH append | — | **`appendChildren`** incremental (cheap) |
| Hard-delete `Replace(..., 0, all, remaining)` | **O(N_parent)** | **`fromNodes` again** |

`fromNodes` rebuilds `parentByChild` / `ownerParentByChild` and remaps `owner` on **all** nodes ([[GraphBuild.fs]] 74–79, 279–290).

**Delete one among N (MoveToTrash):** ≥1 full graph rebuild from span remove; TRASH append is the cheap path. Sibling count **N** affects splice **O(N)**; graph size **E** dominates rebuild.

`Change.apply` folds ops sequentially ([[History.fs]] 312–326): **P** non-append replaces ⇒ **P** full rebuilds.

---

## 3. SiteMap reconcile (`withSiteMap`)

```359:362:src/Client/UpdateHelpers.fs
let withSiteMap (model: VM) : VM =
    ...
        ViewModel.reconcileSiteMapFrom model.graph zoomRoot model.siteMap model.nextSiteId
```

`reconcileSiteMapFrom` walks **expanded** entries only; collapsed children stay leaves ([[ViewModelSiteMap.fs]] 173–190, 62–120).

### Sibling rematch after mid-list delete

For an expanded parent with **N** children, each child is matched positionally, else `List.tryPick` over old children ([[ViewModelSiteMap.fs]] 76–100).

Deleting index **i** shifts all later siblings:

- Indices **&lt; i**: positional hit → **O(1)** each.
- Indices **≥ i**: positional miss → **O(N)** `tryPick` each.

**Worst case (delete near front):** **O(N²)** rematch for that parent alone.  
**Best case (delete last):** **O(N)** positional hits.

Then `buildParentInstanceIndex` over remaining site entries → **O(S log S)** every reconcile ([[ViewModelSiteMap.fs]] 9–14, 188–190).

Orphaned instance ids (removed child + its expanded descendants) are simply omitted from the new `acc` map.

### Collapse vs structural delete

```200:204:src/Shared/ViewModelSiteMap.fs
    let toggleFold (instanceId: SiteId) (siteMap: SiteMap) : SiteMap =
        ...
            { siteMap with entries = Map.add instanceId { entry with expanded = false; childrenStale = true } siteMap.entries }
```

Fold: **O(1)** site entry update, **no** graph change, children lists **kept** (stale). Delete: full reconcile + graph rewrite. Both still force a **new** `siteMap` reference → DOM falls off the selection fast path.

---

## 4. View update / DOM patch

### Fast path gate (does not apply)

```96:108:src/Shared/ViewModelDomPlan.fs
    let private canUseSelectionFastPath (oldModel: VM) (newModel: VM) : bool =
        obj.ReferenceEquals(oldModel.siteMap, newModel.siteMap)
        && obj.ReferenceEquals(oldModel.graph, newModel.graph)
        && ...
```

Delete updates `graph` and `siteMap` → always the **full** plan branch.

### Full `planPatchDOM`

```126:204:src/Shared/ViewModelDomPlan.fs
    let planPatchDOM ... =
        match tryPlanSelectionOnly ... with
        | Some mutations -> mutations
        | None ->
            let newVisible = getVisibleInstanceIds newModel.siteMap
            ...
            removals @ upserts
```

- `getVisibleInstanceIds`: preorder **O(V)** ([[ViewModelSiteMap.fs]] 422–427).
- Removals: cached ids not in new visible → `RemoveRow`.
- Upserts: **every** remaining visible row → `PatchRow` / `RecreateRow` / `CreateRow`.
- With O(1) `childIndex`, selection checks are **O(1)** per row ([[ViewModelRowState.fs]] 14–25) → planning **O(V)** (not O(V²)).

### `patchDOM` apply

```103:151:src/Client/View.fs
    let hasStructuralMutation =
        mutations |> List.exists (function CreateRow _ | RemoveRow _ | RecreateRow _ -> true | ... )
    if not hasStructuralMutation then
        // named PatchRows only
    else
        for instId in ViewModel.getVisibleInstanceIds newModel.siteMap do
            ... resolveRow ... atCorrectPos insertBefore ...
```

Any delete that removes a visible row sets `RemoveRow` → **structural** path: second full visible walk + DOM order checks **O(V)**. Remaining rows are mostly identity patches but still visited.

**Delete one among N (parent expanded):** V ≈ N (plus ancestors); remove 1; walk ~N−1.  
**Delete expanded parent of N:** remove 1+N (or more descendants); walk remaining outside that subtree.

No virtualization; row DOM cost scales with visible count (see [[investigation.md]]).

---

## Ranked hotspots (large N under one parent, graph edges E)

1. **Non-append `Graph.replace` → `fromNodes`** — **O(E)** per mid-list / rewrite Replace; usually ≥1 per Delete; hard-delete multiplies. Dominates when E ≫ N.
2. **`getAllOccurrences` in classify / hard-delete** — **O(E)** per node touched; hard-delete subtree **O(S·E)**.
3. **SiteMap reconcile sibling rematch** — up to **O(N²)** when deleting early among N expanded siblings.
4. **Full structural `planPatchDOM` + `patchDOM`** — **O(V)** plan + **O(V)** DOM order walk; unavoidable today for any structural change. Selection fast path irrelevant.
5. **F# list splice on parent.children** — **O(N)** copy; usually smaller than (1)/(3).
6. **`buildParentInstanceIndex`** — **O(S log S)** each reconcile.

---

## Scenario cheatsheet

### A. Delete one child among N siblings (owner → MoveToTrash)

- Ops: span remove + TRASH append (+ rare rename).
- Graph: **O(N)** splice + **O(E)** `fromNodes`; append incremental.
- SiteMap: rematch up to **O(N²)**; drop one leaf entry.
- DOM: structural **O(V)**; one `RemoveRow`.

### B. Delete parent that has N children (parent selected among its siblings)

- Graph: same as A for the **parent** edge; **N** children remain on the moved node.
- If parent was expanded: reconcile drops parent + descendant site entries; view loses **N** rows without **N** graph deletes.
- DOM: many `RemoveRow`s + structural walk of leftover visible rows.

### C. Multi-select K contiguous children

- One span remove of length K; classify **O(K·E)**.
- Same rebuild / rematch / DOM pattern as A with K removals.

### D. Hard-delete from TRASH (unique owner under trash)

- Plan **O(S·E)** + multiple `fromNodes`; DOM only if those rows are in current zoom/visible map.

### E. Collapse parent (FoldUnfold / ArrowLeft fold)

- No ops / no `fromNodes`.
- `toggleFold` O(1); DOM still full structural path (siteMap identity changes) — cheaper than delete on graph side, similar DOM class of work (mass `RemoveRow`).

---

## Relation to selection-only fast path

Implemented for **Selecting ↔ Selecting** with **unchanged** `graph`/`siteMap` refs ([[implement-fix.md]], [[further-speedups.md]]).

| Gesture | Fast path? |
| --- | --- |
| CursorUp/Down | Yes (intended) |
| Fold / unfold | No (`siteMap` replaced) |
| Delete / Move / Paste structural | No (`graph` + `siteMap` replaced) |

Further “edit-toggle” fast path ([[further-speedups.md]]) also would **not** cover delete.

---

## Possible follow-ups (not implemented)

1. Incremental mid-list `Replace` (avoid `fromNodes` when only one parent’s child list changes) — largest structural win across Delete/Move/etc.
2. Occurrence index / use `parentByChild` instead of full-graph `getAllOccurrences`.
3. Reconcile: shift-aware rematch after span remove (avoid O(N²) `tryPick`).
4. Structural DOM plan that only removes deleted ids + patches parents / shifted siblings (still need correct ordering for creates).
5. HITL: Delete among 300–500 siblings vs collapse same parent — separate graph vs DOM cost.

Board mutation for parent (if desired): **add** Pending → link this file — “profile/optimize delete among large sibling lists (fromNodes + reconcile rematch)”.
