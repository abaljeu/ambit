# 06 — Keep a nested node whose derived path differs
**Status**: defined
One job: The Server keeps a nested File or Directory in the move list when that node's new derived path differs from the path the enclosing Directory move produces, so that node still reaches its own derived path. Story 7 Nested and overlapping paths in [Node move and disk](plan/node-move-disk/spec.md) is that keep.

Type: coding
Blockers: [05 — Relocate in the same workspace](05-relocate-in-the-same-workspace.md)

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

[ ] This ticket adds no union case.
[ ] This ticket changes no public signature.
[ ] `DocumentPersistChange.validatePathMoves` in `src/Server/DocumentPersistChange.fs` calls `coalescePathMoves`.
[ ] `DocumentPersistChange.executePathMoves` in `src/Server/DocumentPersistChange.fs` calls `coalescePathMoves`.
[ ] Those two callers stay as they are.

#### Tests the ticket leaves as they are

[ ] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` renames `docs` to `archive` and expects the nested `readme.txt` move to be absent.
[ ] ``coalescePathMoves keeps file-owned directory separate from file move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` expects the File move and the owned Directory move.
[ ] ``planPathMovesBetweenGraphs includes nested roots after directory rename`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` expects both the Directory move and the nested File move before coalesce.

### Reference

[ ] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Path coalesce** and **Path class**, §4 flow 3 **Nested derived path**, and §6 seam 6.
[ ] [06 — Keep a nested node whose derived path differs scope](plan/node-move-disk/reports/06-keep-a-nested-node-whose-derived-path-differs-scope.md).
[ ] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[ ] `src/Server/DocumentPersistChange.fs` stays as it is.
[ ] `src/Server/DocumentPersistPath.fs` and `src/Server/WorkspaceGit.fs` stay as they are.
[ ] `src/Shared/GraphQuery.fs` stays as it is.
[ ] `crossWorkspaceMoves` and `moveAcrossWorkspaces` stay absent.
[ ] `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` stay as they are.

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

[ ] Record the stop at `plan/node-move-disk/reports/06-keep-a-nested-node-whose-derived-path-differs-stop.md`, then ask.
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
[ ] Write the verdict to `plan/node-move-disk/reports/06-keep-a-nested-node-whose-derived-path-differs-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
