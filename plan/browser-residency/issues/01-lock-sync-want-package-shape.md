# 01 — Lock Sync want + edges/Nodes package shape

**Type:** grilling
**Status:** done
**Blocked by:** None
**Actual:** 10m

## 1. Question

What exact fields do post-Event and Poll carry for a Want, and how does the Server answer with child edges plus Nodes?

Today `ChangeSuccessResponse` carries `events`, `apiVersion`, and related Poll stamps. The destination adds auto wants on those same doors. A Want names Nodes whose Children are desired. The answer must return `Graph.childMap` edges and the Nodes those edges point at — no dangling edges. Absent `childMap` key stays Unloaded.

Lock:

1. **Want request fields** — Which field names, types, and emptiness rules ride post-Event and Poll? Is the Want a list of Node ids only?
2. **Edge vs Node separation** — Are edges and Nodes two collections, one Graph fragment, or another shape? How does the Browser install them without inventing Children?
3. **ApiVersion implications** — Does this package require a new `ApiVersion.current`, a compatible additive field, or a different door? Do not pick the integer yet.

## Answer

1. **Required Want** — `PollRequest` and `ChangeRequest` carry required `want: NodeId list`. Every Poll and post-Event sends the field; empty is `[]`. Poll uses the current-version POST body rather than the old GET query form.
2. **Separate answer collections** — `ChangeSuccessResponse` carries required `nodes: Node list` and `childMap: Map<NodeId, ChildNode list>`. The Server includes every target Node for every emitted edge. The Browser installs Events first, then this answer through `installWantAnswer`.
3. **Current version only** — `ApiVersion.current` is 13. There is no interoperation between versions and no compatibility decode for missing current-version request or answer fields. Poll and post-Event migrate directly to the version 13 shape.
4. **Load converges** — [06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md) affirms that explicit Load Fetch uses the same `nodes` plus `childMap` answer. The legacy `LoadResponse.packages` and `packageChildMap` fields are removed.

## Time

- 2026-09-26 10m — accepted current-version Want and edges/Nodes wire shape (from chat)
