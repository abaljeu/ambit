# 01 — Lock Sync want + edges/Nodes package shape

**Type:** grilling
**Status:** needs-info
**Blocked by:** None

## 1. Question

What exact fields do post-Event and Poll carry for a Want, and how does the Server answer with child edges plus Nodes?

Today `ChangeSuccessResponse` carries `events`, `apiVersion`, and related Poll stamps. The destination adds auto wants on those same doors. A Want names Nodes whose Children are desired. The answer must return `Graph.childMap` edges and the Nodes those edges point at — no dangling edges. Absent `childMap` key stays Unloaded.

Lock:

1. **Want request fields** — Which field names, types, and emptiness rules ride post-Event and Poll? Is the Want a list of Node ids only?
2. **Edge vs Node separation** — Are edges and Nodes two collections, one Graph fragment, or another shape? How does the Browser install them without inventing Children?
3. **ApiVersion implications** — Does this package require a new `ApiVersion.current`, a compatible additive field, or a different door? Do not pick the integer yet.
