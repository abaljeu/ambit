# Switch index build to Node.owner

Type: grilling
Status: open
Blocked by: 01

## Question

When and how do `GraphBuild` owner maps / `appendChildren` stop using `child.ref` and
use `Node.owner` instead, without reintroducing the fromNodes circularity that forced
edge.ref as the write-side source today?
