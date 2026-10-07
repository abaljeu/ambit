# 21 — Globe requests the server

**Status:** `defined`
**Type:** coding
**Blocked by:** [07 — Server completes the picture](07-server-completes-the-picture.md)

## Context

The Find dialog is open. A local search already shows N hits. That local picture is [06 — Residence hits first](06-residence-hits-first.md). The Search Actor is [07 — Server completes the picture](07-server-completes-the-picture.md). The cap of 200 is [14 — Cap of 200](14-cap-of-200.md) section 1 **Find and Move**. Story path **Globe requests the server** is [Online search architecture](plan/online-search/arch.md) §1. [08 — Duplicates on Node id](08-duplicates-on-node-id.md) is not the live story. Move shares this search on [18 — Move uses the same search](18-move-uses-the-same-search.md).

## What to build

The globe on the search bar requests the server when N is under 200. When the search text is unchanged, the server reply replaces the client list. When the person edits the text, the search follows the globe. The server list shows each Node id once. The start does not carry shown Node ids.

### 1. Globe

The Find start stays on [Online search architecture](plan/online-search/arch.md) §1 story path **Globe requests the server** and §2 item 1 **Search Actor**.

1. [ ] Open stays local — The Find dialog opens on a local search and shows N hits. While the globe is not selected, each keypress recomputes on the client and sends no server message.
2. [ ] Globe — When N is under 200, the globe on the search bar can request the server. When N is 200, that request is not sent. That request starts the shared Find and Move Search Actor. There is no quiet-gap Start.
3. [ ] Replace — When the search text is unchanged, the server reply replaces the client list. A reply for an older search string does not replace the list.
4. [ ] Edit follows the globe — When the person edits the text, the search is local when the globe is not selected, and a server search when the globe is selected.
5. [ ] Node id — The server list lists each Node id once. The start does not carry the Node ids the client already showed. The reply does not add server hits onto the client list.
6. [ ] Cap of 200 — The reply holds at most 200 Node ids. The local list stops at 200. There is no continuation cursor.

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 10 **Globe**
