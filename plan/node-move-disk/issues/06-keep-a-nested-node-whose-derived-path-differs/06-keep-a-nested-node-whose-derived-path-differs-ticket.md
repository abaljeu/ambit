# 06 — Keep a nested node whose derived path differs
**Status**: defined
One job: The Server keeps a nested File or Directory in the move list when that node's new derived path differs from the path the enclosing Directory move produces, so that node still reaches its own derived path. Story 7 Nested and overlapping paths in [Node move and disk](plan/node-move-disk/spec.md) is that keep.

Type: coding
Blockers: [05 — Relocate in the same workspace](../05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md)

## Scope

### Signatures

[x] `DocumentPathMove` in `src/Shared/DocumentPathMove.fs` is `{ nodeId: NodeId; oldPath: string; newPath: string }`.
[x] `DocumentPathMove.coalescePathMoves` in `src/Shared/DocumentPathMove.fs` has type `Graph -> DocumentPathMove list -> DocumentPathMove list`.
[x] `DocumentPathMove.planPathMovesBetweenGraphs` in `src/Shared/DocumentPathMove.fs` has type `Graph -> Graph -> DocumentPathMove list`.
[x] `DocumentPathMove.sameWorkspaceMoves` in `src/Shared/DocumentPathMove.fs` has type `Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list` after [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md) lands; this ticket does not add it.
[x] `NodeDesktopPath.expandedPathForNodeId` in `src/Shared/NodeDesktopPath.fs` has type `Graph -> NodeId -> string option` and supplies the desktop `oldPath` and `newPath` strings on each move.
[x] `DocumentPartition.artifactFileRelative` in `src/Shared/DocumentPartition.fs` has type `Graph -> NodeId -> string option`.
[x] `DocumentPartition.artifactDirectoryRelative` in `src/Shared/DocumentPartition.fs` has type `Graph -> NodeId -> string option`.

### Match sites

[x] `DocumentPathMove.moveSourceRelative` in `src/Shared/DocumentPathMove.fs` is private and maps a move to `Option<bool * string>` using `artifactFileRelative` for `Special File` and `artifactDirectoryRelative` for container kinds.
[x] `DocumentPathMove.isStrictPrefix` in `src/Shared/DocumentPathMove.fs` is private with body `path.Length > prefix.Length && path.StartsWith(prefix, System.StringComparison.Ordinal)`.
[x] `DocumentPathMove.coalescePathMoves` in `src/Shared/DocumentPathMove.fs` calls `moveSourceRelative` inside `List.choose` and `isStrictPrefix` inside `List.fold` when `keptIsDirectory` is true.
[x] `DocumentPathMove.coalescePathMoves` in `src/Shared/DocumentPathMove.fs` today drops a later move when a kept Directory move has `isStrictPrefix keptOldRelative oldRelative` and does not read the nested `newPath`.
[ ] The required drop rule keeps using `isStrictPrefix` on pre-graph relative paths and additionally compares the nested move `newPath` to the produced desktop path `directory.newPath` plus the suffix of `nested.oldPath` after `directory.oldPath`.
[x] `DocumentPathMove.joinParentName` in `src/Shared/DocumentPathMove.fs` is private and joins a parent desktop path with a name segment.
[x] `DocumentPersistChange.validatePathMoves` in `src/Server/DocumentPersistChange.fs` pipes `DocumentPathMove.planPathMovesBetweenGraphs preGraph postGraph` into `DocumentPathMove.coalescePathMoves preGraph`.
[x] `DocumentPersistChange.executePathMoves` in `src/Server/DocumentPersistChange.fs` pipes the input move list into `DocumentPathMove.coalescePathMoves preGraph`.
[x] Search finds no other production caller of `DocumentPathMove.coalescePathMoves`.
[x] This ticket keeps the `coalescePathMoves` signature, so `validatePathMoves` and `executePathMoves` need no edit for the signature.
[ ] Callers may see an extra kept move in the coalesced list when a nested derived path differs; that behavior change does not force a caller edit in this slice.
[x] This ticket adds no union case and does not add `crossWorkspaceMoves` or `moveAcrossWorkspaces`.

### Existing tests

[x] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` renames `docs` to `archive` via `graphWithNestedDocs`, coalesces, expects one Directory move, and expects the nested `readme.txt` File move absent because `newPath` `//home/archive/readme.txt` matches the produced path.
[x] ``coalescePathMoves keeps file-owned directory separate from file move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` renames a File that owns a Directory and expects both File and Directory moves in the coalesced list.
[x] ``planPathMovesBetweenGraphs includes nested roots after directory rename`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` supplies both Directory and nested File moves before coalesce for the `docs` to `archive` rename.
[ ] Seam 6 in [Node move and disk architecture](plan/node-move-disk/arch.md) names ``coalesce keeps a nested node whose derived path differs`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` at the **Path coalesce** / **Path class** seam.

### Copy-from

[x] `DocumentPathMove.joinParentName` in `src/Shared/DocumentPathMove.fs` already joins parent and child desktop path segments when a trailing `/` is present on the parent.
[x] `DocumentPathMove.isStrictPrefix` in `src/Shared/DocumentPathMove.fs` already expresses strict prefix on artifact-relative strings for the Directory drop gate.
[x] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` already builds the graph, plans moves, and calls `coalescePathMoves` for the keep-when-produced-path-matches case.

### write-ticket

[x] Blocker: [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md) adds `sameWorkspaceMoves` that the new seam test calls; this ticket does not re-scope that work.
[ ] Flow 3 **Nested derived path** in [Node move and disk architecture](plan/node-move-disk/arch.md) §4 states Path coalesce keeps a nested File or Directory whose new derived path differs and Path class routes it to Same-workspace relocate or Cross-workspace move; this ticket covers only the coalesce half and `sameWorkspaceMoves` on a same Workspace id, not `crossWorkspaceMoves`.
[ ] `coalescePathMoves` reads only the pre graph plus the move list and cannot read the post graph for the produced-path comparison.

#### Source files

[ ] `src/Shared/DocumentPathMove.fs` — `DocumentPathMove.coalescePathMoves` and any private helper the implementer adds for produced desktop path and equality against `nested.newPath`.

#### Tests

[ ] `tests/Shared.Tests/DocumentPathMoveTests.fs` — add ``coalesce keeps a nested node whose derived path differs``: build a post graph where a Directory rename and a nested File reparent (or other path change) make the nested `newPath` differ from `directory.newPath` plus the `nested.oldPath` suffix after `directory.oldPath`, assert `coalescePathMoves` keeps both moves, and assert `sameWorkspaceMoves preGraph postGraph coalesced` still contains that nested File when `GraphQuery.enclosingWorkspace` is the same on both graphs.
[ ] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` stays unchanged and must still pass.
[ ] ``coalescePathMoves keeps file-owned directory separate from file move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` stays unchanged and must still pass.
[ ] Distinct source files touched: 1, within the [Size limit](plan/context/size-limit.md) of 8.
[ ] Tests added or changed: 1, within the size limit of 4.

## Inputs

### Working

#### Path coalesce

[ ] `DocumentPathMove.coalescePathMoves` in `src/Shared/DocumentPathMove.fs` has type `Graph -> DocumentPathMove list -> DocumentPathMove list`.
[ ] The `Graph` argument is the pre graph.
[ ] Each `DocumentPathMove` carries `oldPath` and `newPath` from `NodeDesktopPath.expandedPathForNodeId`.
[ ] Private `moveSourceRelative` maps a move to the pre-graph artifact relative path: `DocumentPartition.artifactFileRelative` for a File, `DocumentPartition.artifactDirectoryRelative` for a Directory.
[ ] Private `isStrictPrefix` is `path.Length > prefix.Length` and an ordinal `StartsWith`.
[ ] A kept Directory move still drops a later move whose old relative path has that Directory's old relative path as a strict prefix, when the nested `newPath` equals the produced path.
[ ] The produced path is `directory.newPath` plus the suffix of `nested.oldPath` after `directory.oldPath`.
[ ] That suffix exists when `directory.oldPath` is a strict ordinal prefix of `nested.oldPath`.
[ ] When the nested `newPath` differs from the produced path, `coalescePathMoves` keeps the nested move.
[ ] When the relative path is nested and `directory.oldPath` is not a strict prefix of `nested.oldPath`, `coalescePathMoves` keeps the nested move.
[ ] `coalescePathMoves` reads no post graph.
[ ] The sort stays shortest old relative path first, then `nodeId`.

#### Path class

[ ] `DocumentPathMove.sameWorkspaceMoves` from the blocker ticket has type `Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list`.
[ ] The seam test calls `sameWorkspaceMoves` on the coalesced list.
[ ] A kept move whose enclosing Workspace id changes is the input to `crossWorkspaceMoves` in the later cross-workspace ticket.

#### Match sites

[x] This ticket adds no union case.
[x] This ticket changes no public signature.
[x] `DocumentPersistChange.validatePathMoves` in `src/Server/DocumentPersistChange.fs` calls `coalescePathMoves`.
[x] `DocumentPersistChange.executePathMoves` in `src/Server/DocumentPersistChange.fs` calls `coalescePathMoves`.
[x] Those two callers stay as they are for this ticket.

#### Tests the ticket leaves as they are

[x] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` renames `docs` to `archive` and expects the nested `readme.txt` move to be absent.
[x] ``coalescePathMoves keeps file-owned directory separate from file move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` expects the File move and the owned Directory move.
[x] ``planPathMovesBetweenGraphs includes nested roots after directory rename`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` expects both the Directory move and the nested File move before coalesce.

### Reference

[x] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Path coalesce** and **Path class**, §4 flow 3 **Nested derived path**, and §6 seam 6.
[x] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[x] `src/Server/DocumentPersistChange.fs` stays as it is.
[x] `src/Server/DocumentPersistPath.fs` and `src/Server/WorkspaceGit.fs` stay as they are.
[x] `src/Shared/GraphQuery.fs` stays as it is.
[x] `crossWorkspaceMoves` and `moveAcrossWorkspaces` stay absent.
[x] `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` stay as they are.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `coalescePathMoves` in `src/Shared/DocumentPathMove.fs` so a prefix-covered nested move stays when its `newPath` differs from the produced path, as Working specifies.
3. [ ] Expand `tests/Shared.Tests/DocumentPathMoveTests.fs` with the test in Outputs. Reuse `graphWithNestedDocs`.

## Outputs

[ ] `src/Shared/DocumentPathMove.fs` — `coalescePathMoves` keeps a nested move whose `newPath` differs from the produced path.
[ ] Distinct source files: 1. `src/Shared/DocumentPathMove.fs`.
[ ] `tests/Shared.Tests/DocumentPathMoveTests.fs` — the test below.
[ ] ``coalesce keeps a nested node whose derived path differs`` — `Graph.setName` renames Directory `docs` to `archive` and File `readme.txt` to `notes.txt`. `coalescePathMoves` keeps the Directory move and the File move. The File `newPath` is `//home/archive/notes.txt`. The produced path is `//home/archive/readme.txt`. `sameWorkspaceMoves` on that coalesced list returns the File move.

## Stop when

[ ] Record the stop at `plan/node-move-disk/issues/06-keep-a-nested-node-whose-derived-path-differs/06-build.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit changes `validatePathMoves`, `executePathMoves`, `sameWorkspaceMoves`, `gitMove`, or `GraphQuery`.
[ ] Stop when the edit contradicts story 7 Nested and overlapping paths in the spec.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``coalesce keeps a nested node whose derived path differs``
  - `tests/Shared.Tests`: ``coalescePathMoves drops nested roots covered by directory move``
  - `tests/Shared.Tests`: ``coalescePathMoves keeps file-owned directory separate from file move``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/node-move-disk/issues/06-keep-a-nested-node-whose-derived-path-differs/06-build.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
