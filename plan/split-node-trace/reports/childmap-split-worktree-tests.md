# ChildMap worktree tests for node-split

Question: was Enter node-split already broken at the `Graph.childMap` refactor?

Answer: no. Shared split ops pass at the parent commit and at the childMap commit. The existing sibling undo/redo test passes. A throwaway hello/middle/offset-0 fixture also passes. `/workspace` HEAD stayed on `69930a91`.

## 1 — Worktrees

`/workspace` was not checked out. Two detached worktrees:

| Path | Commit | Subject |
|---|---|---|
| `/tmp/wt-pre-childmap` | `0170be5bf32b1a488b37eb318fbb4751b30e01e5` | parent of childMap (merge ready into staging) |
| `/tmp/wt-childmap` | `61c24d51a482f232b74608404cf68cfb2a37e3a4` | Move Node children onto Graph.childMap |

Commands:

```
git worktree add /tmp/wt-pre-childmap 0170be5bf32b1a488b37eb318fbb4751b30e01e5
git worktree add /tmp/wt-childmap 61c24d51a482f232b74608404cf68cfb2a37e3a4
```

API difference used by tests: pre-childMap reads `graph.nodes.[parentId].children`. childMap reads `Graph.children graph parentId`.

## 2 — Existing Shared test: split-shaped Change Undo and Redo

The named test exists at both commits in [HistoryTests.fs](tests/Shared.Tests/HistoryTests.fs). It applies `Op.NewNode` + `ChildListWire.insertAt` + `Op.SetText` on [ModelBuilder.createState12](src/Shared/ModelBuilder.fs), then undo/redo.

Restore (needed once per worktree):

```
dotnet restore /tmp/wt-pre-childmap/tests/Shared.Tests/Gambol.Shared.Tests.fsproj
dotnet restore /tmp/wt-childmap/tests/Shared.Tests/Gambol.Shared.Tests.fsproj
```

Both restores exited 0.

Focused run:

```
dotnet test /tmp/wt-pre-childmap/tests/Shared.Tests/Gambol.Shared.Tests.fsproj --filter "FullyQualifiedName~split-shaped" --no-restore --logger "console;verbosity=detailed"
dotnet test /tmp/wt-childmap/tests/Shared.Tests/Gambol.Shared.Tests.fsproj --filter "FullyQualifiedName~split-shaped" --no-restore --logger "console;verbosity=detailed"
```

| Commit | Result | Assertion messages |
|---|---|---|
| `0170be5b` | PASS — `HistoryTests.split-shaped Change Undo and Redo preserve sibling semantics` [78 ms]. Total tests: 1. Passed: 1. | none |
| `61c24d51` | PASS — same test [72 ms]. Total tests: 1. Passed: 1. | none |

## 3 — Other Shared split / Enter tests

`rg` in each worktree for Shared facts whose names contain Split or Enter:

- `splitContinueEditText` / `ViewModelSplitOpsTests` — absent at both commits.
- `CommandEntryTests.run owns Ctrl Enter and Alt Enter` — key binding for Run, not node-split.
- `ViewModelTests` facts that enter Editing — edit-mode entry, not Enter-split.
- `GraphOnlyChangeChunksTests` `split` — Change chunking.
- `CStyleDocumentTests` brace split, `RefExprTests` slash split, `SearchTests` search-part split — other domains.

Focused probe (expect no matches):

```
dotnet test …/Gambol.Shared.Tests.fsproj --filter "FullyQualifiedName~ViewModelSplitOps|FullyQualifiedName~splitContinue" --no-restore
```

Both worktrees: `No test matches the given testcase filter`. Exit 0. Those unrelated Enter/split facts were not run.

`splitNode` lives in Client [UpdateHelpers.fs](src/Client/UpdateHelpers.fs) at both commits. Shared tests apply the same `Op` list. Client Enter was not executed here.

## 4 — Throwaway hello / sibling fixture

Uncommitted files only in the worktrees: `/tmp/wt-pre-childmap/tests/Shared.Tests/SplitNodeOpsWorktreeTests.fs` and `/tmp/wt-childmap/tests/Shared.Tests/SplitNodeOpsWorktreeTests.fs` (each registered at the end of that worktree's fsproj). `/workspace` was not edited for these tests.

Fixture: parent (`createState12` first root child) with children `hello` and `sib`. Ops match `splitNode` (NewNode, `ChildListWire.insertAt`, SetText only when text changes).

- Middle, cursor 3: NewNode `"lo"`, insertAt after original, SetText original to `"hel"`. Assert texts, `sib` still present, child list length + 1.
- Offset 0: NewNode `""`, insertAt before original, no SetText. Assert original still `"hello"`, blank sibling present, `sib` still present.

```
dotnet test /tmp/wt-pre-childmap/tests/Shared.Tests/Gambol.Shared.Tests.fsproj --filter "FullyQualifiedName~SplitNodeOpsWorktree" --no-restore --logger "console;verbosity=detailed"
dotnet test /tmp/wt-childmap/tests/Shared.Tests/Gambol.Shared.Tests.fsproj --filter "FullyQualifiedName~SplitNodeOpsWorktree" --no-restore --logger "console;verbosity=detailed"
```

| Commit | Result | Assertion messages |
|---|---|---|
| `0170be5b` | PASS — `middle split at cursor 3 keeps sibling and both texts` [70 ms]; `offset-0 split inserts blank and does not wipe siblings` [1 ms]. Total tests: 2. Passed: 2. | none |
| `61c24d51` | PASS — same two facts [64 ms] and [1 ms]. Total tests: 2. Passed: 2. | none |

## 5 — GraphMutate.replace and GraphBuild.addDetachedNode at 61c24d51

Commands:

```
git show 61c24d51:src/Shared/GraphBuild.fs
git show 61c24d51:src/Shared/GraphMutate.fs
```

`GraphBuild.addDetachedNode` (NewNode apply path):

- Fresh id: add the node with `owner = graph.root`, and `childMap |> Map.add node.id []`.
- Existing id: `fromNodes` with the same `childMap`.
- It does not rewrite the parent's child list. NewNode does not empty the parent. A following Replace does not lose siblings for that reason.

`GraphMutate.replace`:

- `Op.Replace` applies as `Graph.replace parentId 0 oldChildren newChildren` ([History.fs](src/Shared/History.fs) `applyAllowed`).
- `ChildListWire.insertAt` is a full-list Replace: old = current children, new = `insertInto` that list.
- Full-list wire (`index = 0` and `oldCount = childCount`) sets `updatedChildren` to the new list (`prefix @ newChildren @ suffix` with empty prefix/suffix).
- Non-append commit writes `Map.add parentId updatedChildren graph.childMap` then `fromNodes`.
- Siblings stay when they are in the new list. `insertAt` keeps them.
- Owner of the new node starts as root. `fromNodes` / `applyOwnerField` then sets owner from the Owner edge under the parent. Existing Owner edges in the new list keep their owner.

Replace after NewNode drops siblings or owner only if the new list omits them or the old span does not match (Invalid, not a silent wipe). The passing throwaway tests show the split-shaped lists match and keep `sib`.

## 6 — Conclusion

Node-split Shared ops were not broken at `61c24d51`. Both the stock sibling undo/redo test and the hello/sibling throwaway pass before and at the childMap commit. If Enter-split is broken later, look after this refactor.
