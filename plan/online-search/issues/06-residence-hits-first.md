# 06 — Residence hits first

**Type:** coding
**Status:** done
**Blocked by:** None — can start immediately

## Context

A person types in Find. The dialog can show only Nodes the Browser already holds. This slice keeps that first picture on the client. Story path **Residence hits first** is [Online search architecture](plan/online-search/arch.md) §1.

This ticket matches the residence Find and Move the client already runs. Each keypress recomputes on the residence Graph only. There is no Search Actor Start. No product code change is required.

## What to build

While the globe is not selected, each keypress recomputes Find on the client and updates the dialog at once. That keypress sends no server message. When Move recomputes on each keypress the same way, and the globe is not selected, Move uses this same client path. The globe path is [21 — Globe requests the server](21-globe-requests-the-server.md). The existing [Search dialog](src/Client/SearchDialog.fs) path satisfies the local items below.

### 1. Search dialog

This path updates [Search dialog](src/Client/SearchDialog.fs). It does not start **Search Actor**. Start rules are [Online search architecture](plan/online-search/arch.md) §2 item 1 **Search Actor**, Interface.

1. [x] Keypress — While the globe is not selected, every keypress recomputes on the client only and updates the Find dialog immediately.
2. [x] No server message — While the globe is not selected, a keypress sends no server message and does not start the Search Actor. The globe path is [21 — Globe requests the server](21-globe-requests-the-server.md).
3. [x] Move keypress — When Move recomputes on each keypress the same way, it uses this same client path.

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 7 **Standing locks**

## Comments

- 2026-10-05: Alan. Status `done`. The residence Find and Move path already recomputes on each keypress. No product code change.
- 2026-10-07: The globe path is [21 — Globe requests the server](21-globe-requests-the-server.md). This ticket stays the local picture when the dialog opens and when the globe is not selected.
