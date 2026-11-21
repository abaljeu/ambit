# 54 — Parse and import stop the Parsed Set: SetDocumentState in ImportText

Branch: dev.

Question only. Source stays as read.

## Working copy

The open file is the working copy of [[src/Shared/ImportText.fs]].

- `markDocumentCurrentBeforeParse` returns `[]` when the focus node is an Unparsed document root.
- `markDocumentCurrentBeforeParse` returns `[]` for every other node.
- `buildImportChange` still prepends that list, then `package.ops`, then the replace-all-children op.
- `buildDirectoryMergeChange` still prepends that list in both the empty-merge event and the filtered-merge event.
- The text `SetDocumentState` is absent from this file. The op was replaced in place by the empty list. It was not moved to another function in this file.

## HEAD

`git diff HEAD -- src/Shared/ImportText.fs`.

- On HEAD, the Unparsed document-root arm of `markDocumentCurrentBeforeParse` is `[ Op.SetDocumentState(focusId, Unparsed, Current) ]`.
- The working copy replaces that one-element list with `[]`.
- The other arm was already `[]` on HEAD.
- The same diff adds `importCommand` and `tryImportFocus`.
- The same diff sets `commandName` to `importCommand focusId` on the three events in `buildImportChange` and `buildDirectoryMergeChange`. HEAD used `commandName = ""`.

The working copy is the empty-list slice for this helper. The helper still runs.

## Search of the open file

- A search of the open file for `SetDocumentState` matches the working copy.
- The cursor near line 55 sits on `markDocumentCurrentBeforeParse`, whose Unparsed arm is now `[]`.

## Functions that still build the op

- `ImportDocument.planParseFile` prepends `Op.SetDocumentState(fileId, Unparsed, Current)` when the File is Unparsed.
- `DirectoryReconcile.markCurrent` builds `Op.SetDocumentState(directoryId, Unparsed, Current)` when the directory node is Unparsed.
- `LazyLoadReconciliationApply.markUnparsed` builds Current to Unparsed, and PullingFile to Unparsed.
- `LazyLoadReconciliationApply.markParentCurrent` builds Unparsed to Current for a Directory or a Workspace.
- `LazyLoadReconciliationApply.parseDirInfoIfPresent` builds Unparsed to Current, and PullingFile to Current.
- `WorkspaceUploadStructure.markUnparsed` builds Current to Unparsed, and PullingFile to Unparsed.
- `WorkspaceUploadStructure.markPullingFile` builds Current to PullingFile, and Unparsed to PullingFile.
- `WorkspaceUploadStructure.markParentCurrent` builds Unparsed to Current.
- `WorkspaceUploadStructure.planServerFilePresentOps` builds PullingFile to Unparsed.
- `LazyLoadReconciliation.unparsedStateOp` builds a SetDocumentState to Unparsed. `containingDirectoryMarkOps` uses it.

## Rebuild of an existing op

- `Op.invert` in `src/Shared/History.fs` swaps the two states and builds `Op.SetDocumentState` again.
- Decode in `src/Shared/Serialization.fs` builds `Op.SetDocumentState` from the wire name `SetDocumentState`.

## Match only

These read an op that already exists.

- `Op.apply` in `src/Shared/History.fs`
- `Api.inferredNodeEvents`
- `DatabaseProjection`
- `ResidentProjection`
- `DocumentOpImpact`
- `LazyLoadReconciliationServer`
- `DocumentColdParse`

## Import test still on HEAD

- The fact `build import marks unparsed file current before tree operations` in `tests/Shared.Tests/ImportTextTests.fs` still expects `Op.SetDocumentState(focusId, Unparsed, Current)` as the first op of `buildImportChange`.
- That test file is unchanged from HEAD.

Issue: [54 — Parse and import stop the Parsed Set](../issues/54-parse-and-import-stop-the-parsed-set.md).
