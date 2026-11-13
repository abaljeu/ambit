# 21 — Globe requests the server

**Status:** `defined`
**Type:** coding
**Blocked by:** [07 — Server completes the picture](07-server-completes-the-picture.md)

## Context

The Find dialog is open. The residency search has already returned its client hits. That residency search is [06 — Residence hits first](06-residence-hits-first.md). Both searches start at the zoom root. The Search Actor is [07 — Server completes the picture](07-server-completes-the-picture.md). The cap of 200 is [14 — Cap of 200](14-cap-of-200.md) section 1 **Find and Move**. Story path **Globe requests the server** is [Online search architecture](plan/online-search/arch.md) §1. [08 — Duplicates on Node id](08-duplicates-on-node-id.md) is not the live story. Move shares this search on [18 — Move uses the same search](18-move-uses-the-same-search.md).

## What to build

The globe on the search bar requests the server when the residency search has fewer than 200 client hits. When the search text is unchanged, the server results exclude Node ids the client search found, and the dialog appends those server hits. The server result list carries the found matching nodes. The client adds those nodes to the residency Graph and then lists them. When the person edits the text, the search follows the globe. The server list lists each Node id once. That reply includes the found Nodes as Want-fulfillment. [15 — Dialog shows server hits](15-dialog-shows-server-hits.md) installs that list. Alan, 2026-11-13.

### 1. Globe

The Find start stays on [Online search architecture](plan/online-search/arch.md) §1 story path **Globe requests the server** and §2 item 1 **Search Actor**.

1. [ ] Open stays local — The Find dialog opens on the residency search. That search starts at the zoom root. While the globe is not selected, each keypress recomputes on the client and sends no server message.
2. [ ] Globe — When the residency search has fewer than 200 client hits, the globe on the search bar can request the server. When that search has 200 client hits, that request is not sent. That request starts the shared Find and Move Search Actor. There is no quiet-gap Start.
3. [ ] Append — When the search text is unchanged, the server results exclude Node ids the client search found, and the dialog appends those server hits. The client adds the carried nodes to the residency Graph and then lists them. A reply for an older search string leaves the list.
4. [ ] Edit follows the globe — When the person edits the text, the search is the residency search when the globe is not selected, and the full-database search when the globe is selected. The server search starts at the zoom root.
5. [ ] Node id — The server list lists each Node id once. The dialog excludes Node ids the client search found when it appends the server hits. Displayed rows play no part, except that both searches start at the zoom root.
6. [ ] Cap of 200 — The reply holds at most 200 Node ids. The residency search stops at 200 client hits. There is no continuation cursor.
7. [ ] Want nodes — The server reply includes the found Nodes on the existing Want answer `nodes` list. There is no new package. [15 — Dialog shows server hits](15-dialog-shows-server-hits.md) installs that list. Claim home: [Want nodes for hits](../../doc/current/want-nodes.md).

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 10 **Globe** and item 11 **Want nodes**
