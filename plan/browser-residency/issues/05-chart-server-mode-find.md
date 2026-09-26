# 05 — Chart server-mode Find

**Type:** grilling
**Status:** needs-info
**Blocked by:** [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md)

## 1. Question

How should Find gain a Server mode that asks the Server and lets the Browser receive found Nodes, without changing today's residence-only default?

Default Find searches residence only. This ticket is design only. It does not implement Fetch-before-navigate. Later work may Fetch those Nodes before navigate when a hit is not Resident.

This ticket is postponed. It does not gate [08 — Migrate Shared wire](08-migrate-shared-wire.md) through [12 — Contract old Load Fetch packages](12-contract-old-load-fetch-packages.md).

Lock:

1. **Two modes** — What names and triggers distinguish residence Find from Server-mode Find? Does Server mode stay opt-in?
2. **Received Nodes** — When the Server answers found hits, does the Browser install those Nodes through the same edges-plus-Nodes package as auto wants, or through a Find-specific shape?
3. **Navigate** — What must be Resident before navigate: the hit only, the hit plus its framing path, or more? Is hydrate-before-navigate a later implementation ticket after this design?
