## Signatures

[ ] `DocumentPathMove` in `src/Shared/DocumentPathMove.fs` is `{ nodeId: NodeId; oldPath: string; newPath: string }`.
[ ] `DocumentPathMove.coalescePathMoves` in `src/Shared/DocumentPathMove.fs` has type `Graph -> DocumentPathMove list -> DocumentPathMove list`.
[ ] `DocumentPathMove.planPathMovesBetweenGraphs` in `src/Shared/DocumentPathMove.fs` has type `Graph -> Graph -> DocumentPathMove list`.
[ ] `DocumentPathMove.sameWorkspaceMoves` in `src/Shared/DocumentPathMove.fs` has type `Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list` after [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md) lands; this ticket does not add it.
[ ] `NodeDesktopPath.expandedPathForNodeId` in `src/Shared/NodeDesktopPath.fs` has type `Graph -> NodeId -> string option` and supplies the desktop `oldPath` and `newPath` strings on each move.
[ ] `DocumentPartition.artifactFileRelative` in `src/Shared/DocumentPartition.fs` has type `Graph -> NodeId -> string option`.
[ ] `DocumentPartition.artifactDirectoryRelative` in `src/Shared/DocumentPartition.fs` has type `Graph -> NodeId -> string option`.

## Match sites

[ ] `DocumentPathMove.moveSourceRelative` in `src/Shared/DocumentPathMove.fs` is private and maps a move to `Option<bool * string>` using `artifactFileRelative` for `Special File` and `artifactDirectoryRelative` for container kinds.
[ ] `DocumentPathMove.isStrictPrefix` in `src/Shared/DocumentPathMove.fs` is private with body `path.Length > prefix.Length && path.StartsWith(prefix, System.StringComparison.Ordinal)`.
[ ] `DocumentPathMove.coalescePathMoves` in `src/Shared/DocumentPathMove.fs` calls `moveSourceRelative` inside `List.choose` and `isStrictPrefix` inside `List.fold` when `keptIsDirectory` is true.
[ ] `DocumentPathMove.coalescePathMoves` in `src/Shared/DocumentPathMove.fs` today drops a later move when a kept Directory move has `isStrictPrefix keptOldRelative oldRelative` and does not read the nested `newPath`.
[ ] The required drop rule keeps using `isStrictPrefix` on pre-graph relative paths and additionally compares the nested move `newPath` to the produced desktop path `directory.newPath` plus the suffix of `nested.oldPath` after `directory.oldPath`.
[ ] `DocumentPathMove.joinParentName` in `src/Shared/DocumentPathMove.fs` is private and joins a parent desktop path with a name segment.
[ ] `DocumentPersistChange.validatePathMoves` in `src/Server/DocumentPersistChange.fs` pipes `DocumentPathMove.planPathMovesBetweenGraphs preGraph postGraph` into `DocumentPathMove.coalescePathMoves preGraph`.
[ ] `DocumentPersistChange.executePathMoves` in `src/Server/DocumentPersistChange.fs` pipes the input move list into `DocumentPathMove.coalescePathMoves preGraph`.
[ ] Search finds no other production caller of `DocumentPathMove.coalescePathMoves`.
[ ] This ticket keeps the `coalescePathMoves` signature, so `validatePathMoves` and `executePathMoves` need no edit for the signature.
[ ] Callers may see an extra kept move in the coalesced list when a nested derived path differs; that behavior change does not force a caller edit in this slice.
[ ] This ticket adds no union case and does not add `crossWorkspaceMoves` or `moveAcrossWorkspaces`.

## Existing tests

[ ] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` renames `docs` to `archive` via `graphWithNestedDocs`, coalesces, expects one Directory move, and expects the nested `readme.txt` File move absent because `newPath` `//home/archive/readme.txt` matches the produced path.
[ ] ``coalescePathMoves keeps file-owned directory separate from file move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` renames a File that owns a Directory and expects both File and Directory moves in the coalesced list.
[ ] ``planPathMovesBetweenGraphs includes nested roots after directory rename`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` supplies both Directory and nested File moves before coalesce for the `docs` to `archive` rename.
[ ] Seam 6 in [Node move and disk architecture](plan/node-move-disk/arch.md) names ``coalesce keeps a nested node whose derived path differs`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` at the **Path coalesce** / **Path class** seam.

## Copy-from

[ ] `DocumentPathMove.joinParentName` in `src/Shared/DocumentPathMove.fs` already joins parent and child desktop path segments when a trailing `/` is present on the parent.
[ ] `DocumentPathMove.isStrictPrefix` in `src/Shared/DocumentPathMove.fs` already expresses strict prefix on artifact-relative strings for the Directory drop gate.
[ ] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` already builds the graph, plans moves, and calls `coalescePathMoves` for the keep-when-produced-path-matches case.

## write-ticket

[ ] Blocker: [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md) adds `sameWorkspaceMoves` that the new seam test calls; this ticket does not re-scope that work.
[ ] Flow 3 **Nested derived path** in [Node move and disk architecture](plan/node-move-disk/arch.md) §4 states Path coalesce keeps a nested File or Directory whose new derived path differs and Path class routes it to Same-workspace relocate or Cross-workspace move; this ticket covers only the coalesce half and `sameWorkspaceMoves` on a same Workspace id, not `crossWorkspaceMoves`.
[ ] `coalescePathMoves` reads only the pre graph plus the move list and cannot read the post graph for the produced-path comparison.

### Source files

[ ] `src/Shared/DocumentPathMove.fs` — `DocumentPathMove.coalescePathMoves` and any private helper the implementer adds for produced desktop path and equality against `nested.newPath`.

### Tests

[ ] `tests/Shared.Tests/DocumentPathMoveTests.fs` — add ``coalesce keeps a nested node whose derived path differs``: build a post graph where a Directory rename and a nested File reparent (or other path change) make the nested `newPath` differ from `directory.newPath` plus the `nested.oldPath` suffix after `directory.oldPath`, assert `coalescePathMoves` keeps both moves, and assert `sameWorkspaceMoves preGraph postGraph coalesced` still contains that nested File when `GraphQuery.enclosingWorkspace` is the same on both graphs.
[ ] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` stays unchanged and must still pass.
[ ] ``coalescePathMoves keeps file-owned directory separate from file move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` stays unchanged and must still pass.
[ ] Distinct source files touched: 1, within the [Size limit](plan/context/size-limit.md) of 8.
[ ] Tests added or changed: 1, within the size limit of 4.
