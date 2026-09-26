# 01 — Lock Sync want + edges/Nodes package shape

**Type:** grilling
**Status:** done
**Blocked by:** None
**Actual:** 25m

## 1. Answer

Locked 2026-09-26.

1. **Want request** — JSON field `want` as a `NodeId` list on both Poll and post-Event. One shared encode/decode shape (simplicity on transport: send events / request state / receive events / receive state). Do not use Poll query-string for Want. Poll may need a body or stop being pure GET-with-query so both doors share the same JSON field. Implementers follow that lock later.
2. **Empty Want** — Always send `want`. When `Want.compose` is empty, send `want: []`. Do not omit the field. No door-specific empty rules.
3. **Answer package** — On `ChangeSuccessResponse` (Poll / post-Event success), additive fields `nodes` (Node list) and `childMap` (`Map<NodeId, ChildNode list>`). Edges and Nodes are separate collections. No dangling edges (every edge target must be in `nodes` or already Resident). Absent `childMap` key stays Unloaded; present key including `[]` is Loaded. Load keeps `packages` / `packageChildMap` until [06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md). Do not rename Load fields in this lock.
4. **ApiVersion** — When the expand that ships Want + `nodes`/`childMap` lands, set `ApiVersion.current = 13` (wire integer for 1.3: major*10+minor). Bump with expand, same commit as the package. Do not implement the bump in this recording. Current `ApiVersion.current` is 12 (1.2).

## Comments

- 2026-09-26: Grill locked Want request `want`, empty `want: []`, answer `nodes` + `childMap`, and ApiVersion 13 with expand.

## Time

- 2026-09-26 25m — recorded 2026-09-26 grill locks: `want` JSON field, empty `want: []`, `nodes` + `childMap`, ApiVersion 13 with expand (from chat)
