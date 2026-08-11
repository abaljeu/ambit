# Inventory edge ref sites

Type: research
Status: open

## Question

What are the production (and test-helper) sites that still construct, read, compare,
or persist `ChildNode.ref` / edge ownership — grouped so the migration spine can
draw compile-safe slice boundaries?

## Notes

Evidence anchors: [[tmp/childnode-refactor.md]], `Node.childOwnership` in
[[src/Shared/Model.fs]], `GraphBuild` owner-map fold still on `child.ref`,
`node_children.ownership` in [[src/Server/Database.fs]], projection write via
`Node.childOwnership` in [[src/Shared/GraphProjection.fs]].
