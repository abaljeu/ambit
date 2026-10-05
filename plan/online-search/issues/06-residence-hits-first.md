# 06 — Residence hits first

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately

## Context

A person types in Find. The dialog can show only Nodes the Browser already holds. This slice keeps that first picture on the client. Story path **Residence hits first** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

Each keypress recomputes Find on the client and updates the dialog at once. A keypress sends no server message. When Move recomputes on each keypress the same way, Move uses this same client path.

### 1. Search dialog

This path updates [Search dialog](src/Client/SearchDialog.fs). It does not start **Search Actor**. Start rules are [Online search architecture](plan/online-search/arch.md) §2 item 1 **Search Actor**, Interface.

1. [ ] Keypress — Every keypress recomputes on the client only and updates the Find dialog immediately.
2. [ ] No server message — A keypress sends no server message and does not start the Search Actor.
3. [ ] Move keypress — When Move recomputes on each keypress the same way, it uses this same client path.

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 7 **Standing locks**
