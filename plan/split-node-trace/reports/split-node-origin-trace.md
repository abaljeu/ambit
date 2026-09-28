# Split-node origin TRACE

Question: does the Graph.childMap node data-structure change already break Enter node-split (a line becomes two Nodes), or does a later change introduce a path that deletes line text?

Answer: Shared split ops are not broken at that change. The Enter-at-front field wipe is a Client continue-edit snapshot defect. It is already present on the parent of the childMap change. It is not a Graph child-list defect. No later change rewrites the split Op list. The snapshot correction does not change middle-split Graph semantics.

Worktree commands and pass/fail tables: [childmap-split-worktree-tests](childmap-split-worktree-tests.md).

## 1 — Named changelist

| Commit | Subject | Role |
|---|---|---|
| `0170be5b` | parent of childMap | last tip with `Node.children` |
| `61c24d51` | Move Node children onto Graph.childMap | Graph.childMap refactor (PR 120 squash) |
| `a3cd1fd1` | Correct 61c24d51a … graph datastructure refactoring issue | `addDetachedNode` already-present rebuild |
| `68723ec2` | Fix Enter at line start wiping the current node's text (PR 154) | Client continue-edit snapshot |

`61c24d51` removes `Node.children` and `Node.childrenStatus`. `Graph.childMap` is the Loaded child list. An absent parent key is Unloaded. A present key, including `[]`, is Loaded.

The only `splitNode` hunk in `61c24d51` is mechanical:

```
- let ownerChildren = model.graph.nodes.[newNodeOwner].children
+ let ownerChildren = Graph.children model.graph newNodeOwner
```

`ChildListWire.insertAt` is still a full-list `Op.Replace`. `History` still applies `NewNode` then `Replace` then optional `SetText`.

## 2 — Split Op path

[splitNode](src/Client/UpdateHelpers.fs) builds:

1. `Op.NewNode` for the new Child (blank at offset 0; suffix at offset > 0).
2. `ChildListWire.insertAt` on `Graph.children` of the owner (parent sibling, or first Child of an expanded Node).
3. `Op.SetText` on the focused Node only when the text changes. Offset 0 keeps the current text, so there is no `SetText`.

`Graph.replace` refuses an Unloaded parent (`parent children not loaded`). A silent sibling wipe needs a Loaded parent whose `childMap` list is already wrong (Loaded `[]` or a short list), not an Unloaded parent.

## 3 — Verdict at the refactor

Shared apply of those ops is not broken at `61c24d51`.

| Check | `0170be5b` | `61c24d51` |
|---|---|---|
| [HistoryTests](tests/Shared.Tests/HistoryTests.fs) `split-shaped Change Undo and Redo preserve sibling semantics` | PASS | PASS |
| Throwaway middle split `hello` at 3 → `hel` + `lo`, sibling `sib` kept | PASS | PASS |
| Throwaway offset-0 blank insert, `hello` and `sib` kept, no `SetText` | PASS | PASS |
| `NewNode` replay of the suffix id; owner edge and sibling list kept | — | PASS |
| `addDetachedNode` of an attached id; parent children kept | — | PASS |

Worktrees: `/tmp/wt-pre-childmap` at `0170be5b`, `/tmp/wt-childmap` at `61c24d51`. `/workspace` HEAD was not moved for those runs.

`a3cd1fd1` changes only [GraphBuild.addDetachedNode](src/Shared/GraphBuild.fs): an id that is already present no longer goes through `fromNodes`. First-apply split uses a fresh id, so it takes the new-node branch (`owner = root`, `childMap` key `[]`). Replay of that same `NewNode` at `61c24d51` still keeps the parent Owner edge. That correction is a different childMap rebuild issue. It is not the split-break.

## 4 — Forward walk

Commits after `61c24d51` that touch [UpdateHelpers.fs](src/Client/UpdateHelpers.fs), [GraphBuild.fs](src/Shared/GraphBuild.fs), [GraphMutate.fs](src/Shared/GraphMutate.fs), or residency install: `2e3dcc1b`, `d3d80ed2`, `b41c5dea`, `9ccf378b`, `3cde2784`, `a3cd1fd1`, `b982c66c`, `84d19416`, `8309e9db`, `68723ec2`.

None of those rewrite the split Op list (`NewNode` + `insertAt` + optional `SetText`).

`68723ec2` changes only the continue-edit snapshot. Middle and end Enter still edit the new Node. Offset 0 keeps the current Node text in `Editing`. [ViewModelSplitOps.splitContinueEditText](src/Shared/ViewModelSplitOps.fs) holds that rule.

On current tip `69930a91`, focused Shared tests still pass: `split-shaped` plus three `splitContinueEditText` facts (4 passed).

## 5 — Enter-at-front wipe is not the childMap change

At `0170be5b` and at `61c24d51`, offset 0 already keeps focus on the current Node (`focusedInstanceId sel`) and seeds `Editing` with `newNodeText`. At offset 0, `newNodeText` is `textBefore`, which is empty.

[RowView](src/Client/RowView.fs) writes `Editing` text into `#edit-input` on create. An empty snapshot makes the current line look erased. A later commit of that field can `SetText` the Node to empty. The Graph split ops at offset 0 do not change the current Node text.

PR 154 / `68723ec2` corrects that snapshot. It does not change Graph child-list apply. The same focus-stay plus empty snapshot is already on the childMap parent. childMap is not the introducing change.

## 6 — What this TRACE did not make red

Client `splitNode` reads `#edit-input` in the Browser. This run did not drive Fable or the DOM. Shared tests apply the same Op list `splitNode` builds.

A later residency install can overwrite `childMap[parent]` with a short Loaded list ([installWantAnswer](src/Shared/ResidentProjection.fs) `Map.add key kids`). `insertAt` would then `Replace` that short list. That can drop siblings that were not in the installed list. That is not present as a first-apply defect at `61c24d51`, where Loaded `[]` is a true leaf. This TRACE has no red Browser residency split repro.

## 7 — Conclusion

1. The Graph.childMap changelist does not break node-split Shared apply.
2. `a3cd1fd1` is not the split-break.
3. Enter-at-front line wipe is a Client `Editing` snapshot defect. It predates childMap. PR 154 corrects the symptom. It is not a graph data-structure break of split.
4. No later commit introduces a new split Op-list break on the walk to `69930a91`.
