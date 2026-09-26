# 06 — Dual-run vs migrate explicit Load Fetch

**Type:** grilling
**Status:** done
**Blocked by:** None
**Actual:** 30m

## 1. Answer

Locked 2026-09-26.

1. **Dual-run window** — No dual-run in product. Old Load Fetch `packages` / `packageChildMap` exists only for development if needed.
2. **Death of the old path** — Migrate/contract swaps the old form for the new edges-plus-Nodes (`nodes` / `childMap`). Do not maintain both production paths.
3. **App vs Browser** — No dual-run in product covers App and Browser. The old form is not a production leftover after the cut.
4. **Explicit Load after the cut** — Explicit Load uses the new package after the cut.

## Comments

- 2026-09-26: [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md) is done. Load keeps `packages` / `packageChildMap` until this ticket locks the cut. This ticket still decides dual-run and death of the old Fetch path.
- 2026-09-26: Grill locked no dual-run in product. Old `packages` / `packageChildMap` development-only if needed. Migrate/contract swaps to `nodes` / `childMap`. Explicit Load uses the new package after the cut.

## Time

- 2026-09-26 30m — recorded 2026-09-26 grill lock: no dual-run in product; old Load Fetch for development if needed; migrate/contract swap to `nodes` / `childMap` (from chat)
