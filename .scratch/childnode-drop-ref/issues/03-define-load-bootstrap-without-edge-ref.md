# Define load bootstrap without edge ref

Type: grilling
Status: resolved
Blocked by: 01, 08

## Question

Once `ChildNode` has no `ref`, how does load turn DB `node_children.ownership` (and any legacy JSON `ref`) into correct `Node.owner` / id-only children — during the compat window and after encode no longer writes edge ownership into `ChildNode` — assuming dual-Owner pre-collapse policy from [[.scratch/childnode-drop-ref/issues/08-detect-dual-owner-before-load-collapse.md]]?

## Comments

- 2026-08-11: dual-Owner policy locked (08); spine steps 1/7/8 frame the windows.
- 2026-08-11: **DB keeps Ownership in `node_children`** (no `nodes.owner` column in this effort). Wire JSON hard-cutovers are separate from DB bootstrap.

## Answer

PostgreSQL remains the store of edge Ownership via `node_children.ownership` (Owned vs Ref). There is no `nodes.owner` column in this effort’s near destination.

**Load:** read child rows → detect dual Owned (ticket 08: lowest parent wins; extras become Ref appearances) → set each Node’s in-memory `owner` field from the winner → build id-only Children (order preserved) → rebuild `ownerParentByChild` from `Node.owner` only.

**Write:** projection continues to persist `node_children.ownership`, derived from in-memory `Node.owner` (vs parent) once classifiers are owner-only — not from a deleted `ChildNode.ref`.

Legacy JSON edge `ref` is not a DB Load concern; wire slices are hard before/after per deploy.
