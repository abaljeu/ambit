# Define load bootstrap without edge ref

Type: grilling
Status: open
Blocked by: 01, 08

## Question

Once `ChildNode` has no `ref`, how does load turn DB `node_children.ownership` (and
any legacy JSON `ref`) into correct `Node.owner` / id-only children — during the
compat window and after encode no longer writes edge ownership into `ChildNode` —
assuming dual-Owner pre-collapse policy from
[[.scratch/childnode-drop-ref/issues/08-detect-dual-owner-before-load-collapse.md]]?
