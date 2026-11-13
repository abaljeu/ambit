# 23 — Server search works

## 1. Current

[x] [Search](../../../../doc/current/search.md). The cap is `SearchPicture.Request.count` (200 when omitted). That cap applies to the server walk, the client reply room, and the quiet-gap gate.
[x] [Workspace graph](../../../../doc/current/graph.md). Reference search merges namespace-style queries and node text matching.
[x] [Query Actor](../../../../doc/current/query-actor.md). This Actor does not use the Find and Move Actor. The server stops at 200. A query function may stop lower.
[x] [Search Actor](../../../../doc/current/search-actor.md). The cap is that same `SearchPicture.Request.count`.
[x] [Online search architecture](../../arch.md). Seam 1 names ``reply walks from the request start``. `applyServerReply : int -> SearchPicture.Reply -> Node list -> VM -> VM * Effect list`.
[x] [Online search spec](../../spec.md)
[x] [Online search project](../../project.md)
[x] [Online search map](../../map.md)
[x] [06 — Residence hits first](../06-residence-hits-first.md)
[x] [07 — Server completes the picture](../07-server-completes-the-picture.md)
[x] [08 — Duplicates on Node id](../08-duplicates-on-node-id.md)
[x] [11 — Server evaluates](../11-server-evaluates.md)
[x] [14 — Cap of 200](../14-cap-of-200.md)
[x] [15 — Dialog shows server hits](../15-dialog-shows-server-hits.md)
[x] [18 — Move uses the same search](../18-move-uses-the-same-search.md)
[x] [19 — Shared segments](../19-shared-segments.md)
[x] [23 — Server search works](../23-server-search-works.md)

## 2. User

## 3. Found elsewhere

[x] The frontier entry for 23 — Server search works is done.
[x] Status is done.
[x] The code facts record the zoom-root walk, the Want `nodes` list, and the dialog append.
