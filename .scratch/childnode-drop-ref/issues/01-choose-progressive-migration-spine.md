# Choose progressive migration spine

Type: grilling
Status: open
Blocked by: 06

## Question

What ordered slices take us from today’s dual source (`Node.owner` + edge `ref`) to
id-only `ChildNode` with `Node.owner` / `Op.SetOwner` only — and what invariant must
hold after each slice so the tree stays green?

Include at least: when `SetOwner` lands; when index build stops reading `child.ref`;
when `childOwnership` drops edge fallback; when the Loaded-scope membership seam
becomes mandatory; when JSON starts writing node `owner`, when it stops writing edge
`ref`, and when decode drops compat for each; when the `ref` field is deleted from
the type; how DB `node_children.ownership` is read/written in each window; when
pre-collapse dual-Owner detection at load is required.
