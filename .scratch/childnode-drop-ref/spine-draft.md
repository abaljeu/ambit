# Proposed migration spine (grill draft)

After each slice the tree stays green under the stated invariant.

1. **Pre-collapse dual-Owner** at load (detect → lowest parent wins → extras Ref)  
   *Inv:* load never silently last-wins; ≤1 Owned claim collapses into `Node.owner`.

2. **JSON encode + decode `Node.owner`** (still write/read edge `ref`; **hard cutover** — after this slice, node JSON must include `owner`; no omit-compat)  
   *Inv:* matched deploy; new payloads always carry owner; old shape is simply pre-slice.

3. **`Op.SetOwner` + Change-complete Apply→Check→Undo** (Replace still carries `ref`)  
   *Inv:* every accepted ownership Change leaves Loaded-scope OK; rejected Changes leave graph+history unchanged.

4. **GraphBuild index from `Node.owner`** (stop owner-map fold on `child.ref`; proposed edges keep explicit ownership channel — still `ref` for now)  
   *Inv:* resident `ownerParentByChild` matches `Node.owner`; no fromNodes circularity regression.

5. **`childOwnership` drops edge fallback** (live = `Node.owner` only; planners keep proposed-edge channel)  
   *Inv:* live classifiers never read `child.ref`.

6. **Loaded-scope seam mandatory** on all shape/ownership Changes (if not already at 3)  
   *Inv:* no accepted Change can leave a provable membership ↔ owner disagreement.

7. **Stop encoding edge `ref` / DB writes ownership from `Node.owner` only**; **hard cutover** — after this slice, wire children are id-only (no child `"ref"`)  
   *Inv:* matched deploy; new wire/DB classification from `Node.owner`; pre-slice shape is simply old.

8. **Delete `ChildNode.ref`** (+ ctor/equality/test cleanup)  
   *Inv:* type is id-only; ownership only via `Node.owner`.

**DB:** keeps Ownership in `node_children.ownership` through step 8 (and beyond this effort). Read = Load bootstrap into `Node.owner`. Write = derived from in-memory `Node.owner` once classifiers are owner-only.
