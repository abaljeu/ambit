# Split-node origin TRACE

Question: does the Graph.childMap node data-structure change already break Enter node-split (a line becomes two Nodes), or does a later change introduce a path that deletes line text?

Answer: Shared first-apply split ops are not broken at childMap. Two later mechanisms can delete line text after a local split. The one that leaves a truncated line (`hel` without `lo`) is introduced at `8309e9db`. The one that drops the new sibling and can restore the old full text is Want overwrite, live from `52f79c49`..`b41c5dea`.

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
3. `Op.SetText` on the focused Node only when the text changes. Offset 0 uses live `currentText`. An empty field still posts `SetText` to empty. See §10.

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

## 6 — Browser gap

Client `splitNode` reads `#edit-input` in the Browser. This run did not drive Fable or the DOM. Shared tests apply the same Op list `splitNode` builds. Residency overwrite is now characterized in Shared. See §8.

## 7 — First-pass conclusion (childMap)

1. The Graph.childMap changelist does not break node-split Shared apply.
2. `a3cd1fd1` is not the split-break.
3. Enter-at-front line wipe is a Client `Editing` snapshot defect. It predates childMap. PR 154 corrects the symptom. It is not a graph data-structure break of split.
4. The split Op list is unchanged after childMap except the PR 154 snapshot.

## 8 — Hypothesis 1: residency Want overwrite

[installWantAnswer](src/Shared/ResidentProjection.fs) does `Map.add key kids` on `childMap`. A stale package whose parent list omits the new split Child replaces the local list. [graphAfterWant](src/Shared/SyncLogic.fs) runs that install after every non-empty Poll/Post answer.

Characterization in [SplitOriginTraceTests.fs](tests/Shared.Tests/SplitOriginTraceTests.fs) (5 passed):

| Fact | Result | Meaning |
|---|---|---|
| `stale installWantAnswer after split drops suffix and restores hello text` | PASS | Primitive is real. Suffix edge gone. `hello` text restored. Node still in `nodes`. |
| `applySyncResponse stale Want after split drops suffix` | PASS | Poll/Post consume path does the same overwrite. |
| `compose after Loaded split does not want the parent` | PASS | Default [Want.compose](src/Shared/Want.fs) lists only Unloaded ids. A Loaded split parent is not requested. |
| `applyLoadResponse answer-only with pending is raced Load answer` | PASS | Load Fetch is guarded when pending is non-empty. |

Kill of the common Poll: after a Loaded parent split, `currentWant` is compose, so the parent is not in `request.want`. Server `wantAnswer` then omits that parent. `graphAfterWant` is a no-op for that key.

Live race: an in-flight Poll that composed the parent while it was still Unloaded. Server returns the pre-split list. Client [PollDone](src/Client/Update.fs) `stateOpt = None` still calls `applySyncResponse` (no `isAutoSyncBlocked` check). [tryStartPoll](src/Shared/SyncPlanner.fs) will not start a new Poll while pending, but a Poll already in flight (state `Polling`) can finish after Enter.

Introducing range (Want pipeline after childMap):

| Commit | Role |
|---|---|
| `2e3dcc1b` | `installWantAnswer` overwrite |
| `52f79c49` | `applySyncResponse` uses `graphAfterWant` → `installWantAnswer` |
| `d3d80ed2` | Poll/Post responses carry `childMap` |
| `b41c5dea` | Client PollDone applies `applySyncResponse` |

Narrowest live range: `52f79c49`..`b41c5dea` (2026-09-27). Symptom: new sibling vanishes; original text can return to the pre-split string (split looks undone).

## 9 — Hypothesis 2: later split Op / replay / edit-sync commits

`git log -p 61c24d51..69930a91 -- src/Client/UpdateHelpers.fs` shows one `splitNode` hunk after childMap: `68723ec2` continue-edit snapshot. No later commit changes `NewNode` + `insertAt` + optional `SetText`.

Later sync apply does change the fate of that pending Replace:

- `84d19416` soft-skips recoverable CAS on Poll/sync apply.
- `8309e9db` on that skip, inverts other pending ops on the same field.

[applyOpsForSync Replace CAS undoes pending split insert and keeps prefix text](tests/Shared.Tests/SplitOriginTraceTests.fs) PASSES. Incoming mismatched `Replace` on the parent (`old span does not match`) does not apply. It inverts the local pending `Replace` (suffix removed from the parent list). Same-Change `SetText` is a different op kind, so it is not inverted. Original text stays `hel`.

That is the remaining line-deletion shape: prefix kept, suffix gone from the outline.

`84d19416` alone would skip and leave the local split in place. `8309e9db` is the introducing commit of the undo.

## 10 — Hypothesis 3: Client-only paths

Shared first-apply stays clean. Client paths that can still erase text:

- Offset 0 plus empty `#edit-input` (`readEditInputValue` is `""` when the element is missing). `updatedText = currentText` then `SetText` wipes. This read-empty behavior is in `c543a13b` (2026-04-02). Not introduced after childMap.
- PR 154 snapshot wipe at offset 0. Present on the childMap parent. Fixed at `68723ec2`.

No Fable/DOM run in this TRACE. The two Shared mechanisms above do not need the Browser.

## 11 — Origin

Name these two. They are different symptoms.

1. **Truncated line on split (`hel`, suffix gone):** introducing commit `8309e9db` (Undo optimistic pending field when Poll CAS is soft-skipped). Needs a Poll/sync `Replace` on the same parent whose old span does not match, while the split Change is still pending. Mechanism: soft-skip + invert pending `Replace` only.
2. **New sibling dropped, old full text restored:** introducing range `52f79c49`..`b41c5dea`. Mechanism: stale Want `childMap` overwrite through `applySyncResponse`. Default compose after a Loaded split does not request that parent. An in-flight Unloaded-parent Poll can still carry it.

childMap `61c24d51` is not the first-apply break. PR 154 is not this origin. No product fix in this TRACE.
