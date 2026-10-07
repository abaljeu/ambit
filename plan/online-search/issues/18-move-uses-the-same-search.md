# 18 — Move uses the same search

**Status:** `defined`
**Type:** coding
**Blocked by:** [21 — Globe requests the server](21-globe-requests-the-server.md), [10 — Start at the trash node](10-start-at-the-trash-node.md), [14 — Cap of 200](14-cap-of-200.md), [15 — Dialog shows server hits](15-dialog-shows-server-hits.md)

## Context

Move uses the same search as Find. Find already has the local open, the globe, the replace rule, the trash rule, the cap of 200, and the dialog list. Story path **Move uses the same search** is [Online search architecture](plan/online-search/arch.md) §1. [08 — Duplicates on Node id](08-duplicates-on-node-id.md) is not the live story.

## What to build

Move shows that same hit list. Move uses the shared Find and Move Actor. Move does not start a second Actor. Both use the globe, the same server request, the same cap of 200, the same Node id list, and the same trash rule.

### 1. Search Actor

The shared backend stays on [Online search architecture](plan/online-search/arch.md) §2 item 1 **Search Actor**.

1. [ ] Same backend — Move uses this same search. Find shows the hit list in the dialog. Move shows that same list. Both use the globe, the same server request, the same cap of 200, the same Node id list, and the same trash rule. Move does not start a second Actor.

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 10 **Globe**
