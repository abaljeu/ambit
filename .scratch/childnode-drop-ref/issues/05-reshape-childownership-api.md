# Reshape childOwnership API

Type: grilling
Status: open
Blocked by: 01

## Question

Should `Node.childOwnership` take `(parentId, child: Node)` (or id + Node) instead of
`(graph, parentId, ChildNode)`, when does that land relative to the spine, and which
call sites must keep a graph only to look up the Node?
