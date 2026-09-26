# 03 — Lock ongoing want priority and when wants are attached

**Type:** grilling
**Status:** done
**Blocked by:** None
**Actual:** 20m

## 1. Answer

Locked 2026-09-26.

1. **Attachment** — Every Poll and every post-Event carries the current Want list.
2. **Empty** — Already locked on [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md): always send `want`; empty compose is `want: []`. Restated only; not reopened.
3. **Repeat / cadence** — Want only Unloaded parents (compose lists parents that still miss Children). After Server answers and Browser installs, those parents are no longer Unloaded and drop out of the next compose naturally. Server does not hold back. Receiving Nodes twice is idempotent. No client suppress-until-context-changes list. No Want throttle.

## Comments

- 2026-09-26: [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md) is done. Empty-Want encoding is `want: []`. Attachment and cadence were still open on this ticket.
- 2026-09-26: Grill locked attachment on every Poll and post-Event, restated empty `want: []` from [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md), and cadence: Unloaded parents only, no suppress list, no Want throttle.

## Time

- 2026-09-26 20m — recorded 2026-09-26 grill locks: attach Want on every Poll and post-Event; Unloaded-parent cadence; no suppress list; no Want throttle (from chat)
