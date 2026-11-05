# 23 — Search results never vanish

**Status:** `coded`
**Type:** bug-fixing
**Blocked by:** None

## Context

A person types in Find or in file search. The results area keeps its height. The rows already on screen stay until the next results exist. An empty list shows one non-selectable row `Zero results`.

[15 — Dialog shows server hits](15-dialog-shows-server-hits.md) owns the server list in the dialog.

## What to build

### 1. Query keeps the previous rows

1. [x] Find — `NodeSearchQuery` leaves the cached Find rows in place. The next read of those rows builds the list for the new query.
2. [x] File search — `FileSearchQuery` leaves the cached file-search rows in place. The next read of those rows builds the list for the new query.

### 2. Empty list

1. [x] Zero row — An empty result list draws one `li` whose text is `Zero results`. Find and file search share that row.
2. [x] Pick — Enter, arrow keys, and click leave that row unselected.

## See also

[Online search spec](../spec.md) section 1 **Search spec**

## Check

[x] [23 search results never vanish check](../reports/23-search-results-never-vanish-check.md)
