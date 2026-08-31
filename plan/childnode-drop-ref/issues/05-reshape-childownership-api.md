# Reshape childOwnership API

Type: grilling
Status: resolved
Blocked by: 01

## Question

Should `Node.childOwnership` take `(parentId, child: Node)` (or id + Node) instead of `(graph, parentId, ChildNode)`, when does that land relative to the spine, and which call sites must keep a graph only to look up the Node?

## Answer

At spine step 5 (drop edge fallback): reshape to **`childOwnership(parentId, child: Node)`** — Owned iff `child.owner = parentId`, else Ref. Call sites that only have a child id must look up the Node (need a Graph or the Node in hand). No `ChildNode` / edge `ref` argument.
