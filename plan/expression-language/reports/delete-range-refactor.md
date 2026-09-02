# Delete range refactor

Tree left dirty. No commit. No branch switch.

## Goal

Split Client Delete so Exec/Run can clear a query node's Children without swapping `selectedNodes` or restoring focus.

## What changed

[[src/Client/UpdateOps.fs]]

- `deleteRange` takes a `SiteNodeRange` and delegates to `deleteChildSpan`. `deleteChildSpan` classifies and plans from the graph Node (no SiteEntry required), `applyAndPost` as Delete, then `withSiteMap` on successful apply. Does not clamp or swap `selectedNodes`.
- `deleteSelectionOp` still owns command Delete: `match model.selectedNodes`, `deleteRange` on that range, then the same post-delete selection clamp and `withSiteMap`. No-op and apply error (empty effects) skip the clamp.

[[src/Client/Commands.fs]]

- Removed `withQueryChildrenSelected` and `restoreQueryFocus`.
- `execRunOp` still commits, `shouldExec`, then if the focus Node has Children, calls `deleteChildSpan` on that Node (`start = 0`, `endd = n`). No SiteEntry lookup. Original `selectedNodes` stays. Abort if Children remain, else `runAmbleOp`.
- `cmd Delete` still `keyAlways deleteSelectionOp`. `cmd Exec` still `execRunOp`.

[[src/Client/UpdateAmbleRun.fs]] `runAmbleOp` is unchanged (search + materialise only).

Command Delete still uses `classifyDeleteForSelection` + `planDeleteOps` via `deleteRange`. Exec uses `deleteChildSpan` (graph Node span; no SiteEntry). Shared tests that call `classifyDeleteForSelection` + `planDeleteOps` on a child span are unchanged.

## Tests

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter FullyQualifiedName~AmbleRunTests
```

Result: **Passed — 25/25**.

```
./scripts/client.sh build
```

Result: Fable + esbuild succeeded.

No browser pass. Client `deleteRange` is a compile gate only.

## Notes

Resolved in [[fix-exec-delete-sitemap.md]]: `deleteChildSpan` remaps SiteMap on successful apply; Exec deletes by graph Children without a SiteEntry; empty effects on `deleteSelectionOp` are a true no-op (not a risk).

- Abort when Children remain: blocked classify returns empty ops, so the graph is unchanged. If apply left Children (e.g. skipped Unloaded `Replace`), Exec still does not Run. SiteMap is remapped when Delete applied.
