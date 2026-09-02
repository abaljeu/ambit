# Restore fold occurrences

Date: 2026-08-29
Branch: `w/sitemap-parent-index`
Parent: [[page-not-responding-loading.md]], [[../project.md]]

## Problem

Session fold restore used `e: NodeId[]` — a set of expanded node ids. `applyFoldSession` BFS-expanded every runtime appearance whose `nodeId` was in that set. On a Ref cycle, expanding an appearance materializes a new child appearance of the same `NodeId`; the queue never empties. `SiteId` values are allocated fresh on each boot, so persisted ids could not identify which appearance to reopen.

## Solution

### Shared representation

`FoldOccurrenceSnapshot` in [[src/Shared/ViewModel.fs]]:

- `parentIndex: int option` — index into the same snapshot list (`None` = site-map root)
- `childIndex: int` — 0-based index among parent's `children`
- `nodeId: NodeId` — expected child node

O(expanded appearances) storage. Runtime `SiteId` values are not persisted.

### Shared behavior

[[src/Shared/ViewModelSiteMap.fs]]:

- `captureFoldOccurrences` — preorder walk of expanded SiteMap; emit parent-first records
- `restoreFoldOccurrences` — resolve parent snapshot to runtime `SiteId`, validate child `NodeId`, expand that exact appearance; skip invalid parents and mismatches

Replaced `applyFoldSession`.

### Browser persistence

[[src/Client/SessionState.fs]]:

- Save `f: [{p, i, n}, …]` instead of `e: NodeId[]`
- Retain `z` and `b` as Node IDs
- Legacy payloads with only `e` decode with `f = []` — all appearances stay collapsed until next save

## Tests

[[tests/Shared.Tests/ViewModelTests.fs]]:

1. Ref cycle restores finitely
2. Two appearances of one Node restore independently
3. Restore succeeds when runtime SiteIds differ
4. Stale child index / NodeId mismatch skipped safely
5. Parent index rebuild after 40-deep chain restore

All passed.

## Verification

```text
dotnet test tests/Shared.Tests --filter "FullyQualifiedName~restoreFoldOccurrences|FullyQualifiedName~fold restore"
# Passed: 5

./scripts/client.sh build
# OK
```

## HITL

Warm F5 on Edge/WebView2 profile that previously hung: confirm boot completes with legacy `e` (collapsed folds) and with new `f` after one save/refresh cycle.
