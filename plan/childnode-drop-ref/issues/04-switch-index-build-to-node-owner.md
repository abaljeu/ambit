# Switch index build to Node.owner

Type: grilling
Status: resolved
Blocked by: 01

## Question

When and how do `GraphBuild` owner maps / `appendChildren` stop using `child.ref` and use `Node.owner` instead, without reintroducing the fromNodes circularity that forced edge.ref as the write-side source today?

## Comments

- 2026-08-11: lands at spine step 4; proposed edges keep an explicit ownership channel (still `ref` until later).
- 2026-08-11: clarified — Children are the links; `ownerParentByChild` is a derived lookup map (also historically the path that filled `Node.owner`).

## Answer

At spine step 4: build `ownerParentByChild` only from each resident Node’s `owner` field. Do not read the child-link Owned/Ref mark for owner maps. Set `Node.owner` first (Load bootstrap, NewNode owner arg, or `Op.SetOwner` / field writes in apply); then rebuild indexes. Proposed edges keep a separate ownership channel until `ref` is gone. No two-pass “edge fills missing owner” on the index path.
