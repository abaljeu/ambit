# Independent review: blank edit box and File Backspace

Verdict: **Needs work**.

This review covers two lands. The scroll land between them is out of scope.

1. Fix blank `#edit-input` on one Node after split / Server okay. `ViewModelRowState.retargetEditingSelection`, `RowView` seed text, and the Shared tests.
2. Fix File insert-before Backspace dual owner. `ViewModelJoinOps.previousJoinPlan` and the File blank path.

The mechanical scan from the Poll land through the tip printed no over-long line, no function over 40 lines, and no file over 800 lines on these lands. `previousJoinPlan` is 39 lines. That count is inside the limit in [fsharp-source](.agents/rules/fsharp-source.md). The focused Shared run for `ViewModelJoinOpsTests`, `ViewModelSplitOpsTests`, and `planPatchDOM remounts edit` passed 18 tests.

## Standards

Hard violations of [fsharp-source](.agents/rules/fsharp-source.md): "Group related function parameters into a named, reused type (record or DU). When adding a parameter that belongs with existing ones, extend that type instead of lengthening the argument list. Reuse a type that already exists."

`joinWithPreviousPlan` takes `(currentText, model)`. The new helpers unpack `model`, `Selection`, the visible-neighbor triple, and the parent index into loose arguments. `sel`, `currentId`, `currentNode`, and the Graph are already on `model`.

`previousJoinPlan` takes ten arguments:

```fsharp
(currentText: string) (model: VM) (sel: Selection) (currentId: NodeId)
(currentNode: Node) (prevInstId: SiteId) (prevId: NodeId) (prevNode: Node)
(parentId: NodeId) (indexInParent: int)
```

`tryDiscardBlankPrevious` takes eight arguments:

```fsharp
(graph: Graph) (sel: Selection) (currentText: string) (prevInstId: SiteId)
(prevId: NodeId) (prevNode: Node) (parentId: NodeId) (indexInParent: int)
```

`joinIntoPreviousOps` takes seven arguments:

```fsharp
(graph: Graph) (prevId: NodeId) (prevNode: Node) (currentId: NodeId)
(currentText: string) (parentId: NodeId) (indexInParent: int)
```

`continueEditAfterSplit` lengthens the `splitContinueEditText` trio (`cursorPos`, `currentText`, `newNodeText`) with target identity:

```fsharp
(cursorPos: int) (currentText: string) (newNodeText: string)
(newNodeId: NodeId) (focusInstId: SiteId option) (model: VM)
```

Smell baseline: Parameter Explosion and Data Clumps describe the same bags. The house rule already makes them hard violations. No further smell stands on its own.

## Spec

No local plan spec for these two lands. Spec axis skipped.

## Design

### Blank `#edit-input`

The sibling-slide repair is real. [planPatchDOM](src/Shared/ViewModelDomPlan.fs) used to call `isEditingEntry` with the new `SiteEntry`. A sibling that reused the focus index looked edited. The new code uses the old entry for that instance id. `planPatchDOM remounts edit when cached sibling slides into old edit index` in [ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) locks the remount.

`commitIfEditing` in [UpdateHelpers.fs](src/Client/UpdateHelpers.fs) treats a missing `#edit-input` as "leave Editing". It does not post empty text. A mounted empty box is `Some ""` and still posts.

The snapshot repair is wider than the empty-box bug, and it sits on every site-map refresh.

[retargetEditingSelection](src/Shared/ViewModelRowState.fs) runs from [withSiteMap](src/Client/UpdateHelpers.fs) whenever `mode` is `Editing`. Two arms write the box snapshot:

- `rehydrateEditingFromNode` replaces the snapshot whenever `node.text <> draft`, and it sets the caret to index 0.
- `editingSnapshotFor` runs when no visible edit row matches. It always stores `node.text` and caret index 0.

The comment on `retargetEditingSelection` says an empty or stale draft. The code has no empty check. `retargetEditingSelection replaces a leftover suffix draft on hello` in [ViewModelSplitOpsTests.fs](tests/Shared.Tests/ViewModelSplitOpsTests.fs) locks the wide replace (`"lo"` becomes the Graph text `"hello"`). The recovery test builds a stale parent and does not assert the caret. Recovery of the same Node drops a mid-line caret from `Mode`.

`Mode` here is the edit snapshot. Live keystrokes stay in the DOM until a row is built again or a `SetText` patch runs. `planPatchDOM` emits `SetText` when the outline label changes. It does not emit `SetText` when only the snapshot changes. [makeRow](src/Client/RowView.fs) writes `#edit-input` when it builds the row. A later refresh that keeps the same row in Editing, with the same Graph text, leaves the mounted characters in place. A mounted blank box stays blank. The next commit reads that blank and can wipe the Node. The tested repair is the remount path.

[editInputSeedText](src/Shared/ViewModelRowState.fs) fills a draft only when the draft is `""`. The call site passes [outlineDisplayText](src/Shared/ViewModelRowState.fs). `rehydrateEditingFromNode` reads `node.text`. For an artifact, an empty `text` still displays `name`. A new box then shows the file name while the snapshot stays `""`. A non-empty wrong draft is kept (`editInputSeedText "lo" "hello"` is `"lo"`). Only the retarget arm repairs that draft, and only in `Mode`.

[readEditInputValue](src/Client/UpdateHelpers.fs) still maps a missing box to `""`. `commitIfEditing` is the one caller that stopped. `isAutoSyncBlocked` still treats that `""` as a dirty edit.

### File Backspace

The reported 400 is fixed on the tested path. Enter at the start of a File inserts a blank sibling. Backspace on the File now removes that blank and leaves the File and its child. `joinWithPreviousPlan drops blank above File and keeps the child` applies the ops and checks ownership.

Other join-into-previous moves that copy children now also emit a source-clear `Replace`. [applyOps](src/Shared/History.fs) checks ownership after the whole batch, so append-then-clear can pass. `joinWithPreviousPlan moves current children to previous leaf` covers that final Graph.

The shape that produces the clear is the problem. `previousJoinPlan` is a bag of locals that `joinWithPreviousPlan` already held. The 39-line body stays under the function limit by pushing those locals into `tryDiscardBlankPrevious` and `joinIntoPreviousOps`. Each of those lists repeats the same clump. Further join edits on this signature will keep growing the bag.

Backspace on a File that is not an immediate blank leaf returns `RestoreCaret`:

```fsharp
| None when NodeKind.artifact currentNode.kind -> RestoreCaret
```

A File with children and a text sibling above used to build a join that 400'd (children copied, source list kept). `RestoreCaret` leaves that File in place. A childless File stays in place on the same arm. No test covers `RestoreCaret` for a File.

`isBlankLeaf` treats any whitespace `Normal` leaf as blank. A sibling whose text is spaces is deleted. The File text stays. The caret is index 0.

## Poll glance

[01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md) says: "On this conflict, do not rewrite `#edit-input` unless focus or the Node is gone."

Poll apply in [Update.fs](src/Client/Update.fs) calls `withSiteMap` and then `adjustModeAfterServerApply`. `withSiteMap` now runs `retargetEditingSelection` while `mode` is still `Editing`. Retarget can replace `selectedNodes` before `adjustModeAfterServerApply` reads the focused Node. `adjustModeAfterServerApply` then keeps Editing when that Node's text is unchanged. A repaired focus can be a different row from the one the person was typing.

[finishAppliedSubmit](src/Client/Update.fs) calls `withSiteMap` on the submit ack and does not call `adjustModeAfterServerApply`. The same retarget runs there, after split has left the client in Editing.

A Graph-text change on the edited Node still leaves Editing through `adjustModeAfterServerApply` on the Poll path. That drop is older than these two lands. This glance stops there.

## Summary

Standards: 4 hard violations, 0 extra smells. Worst on this axis: `previousJoinPlan` takes ten arguments.

Spec: 0 findings. No local spec for these lands.

Worst design defect: `retargetEditingSelection` rewrites any unequal edit snapshot, and the recovery arm clears the caret, from inside `withSiteMap`.
